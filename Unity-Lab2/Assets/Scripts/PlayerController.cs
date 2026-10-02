using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Настройки прыжка")]
    public float jumpForce = 7f;
    public float gravity = -9.81f;

    private Rigidbody rb;
    private Animator animator;
    private PlayerControls inputActions;
    private Camera mainCamera;

    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;

    // Переменные для логики прыжка
    private bool isJumping;
    private bool isMidAir;
    private bool canApplyJumpForce;
    private Vector3 jumpDirection;

    // Флаги для аниматора
    private bool isFalling;
    private bool hasLanded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        inputActions = new PlayerControls();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.performed += ctx => Jump();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void Update()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);

        // Защита от проваливания
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        // --- ЛОГИКА ПРИЗЕМЛЕНИЯ ---
        if (isJumping && isGrounded && !hasLanded)
        {
            hasLanded = true;
            isMidAir = false; // <--- ВОТ ОНО: как только коснулись земли, режим "полета" выключается, движение блокируется
            animator.SetBool("HasLanded", true);
        }

        // --- ЛОГИКА ПАДЕНИЯ ---
        if (isJumping && velocity.y < 0 && !isFalling)
        {
            isFalling = true;
            animator.SetBool("IsFalling", true);
        }

        // Получаем направления камеры
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = (camForward * moveInput.y + camRight * moveInput.x);

        // --- БЛОКИРОВКА ДВИЖЕНИЯ ТОЛЬКО ВО ВРЕМЯ АНИМАЦИЙ (Старт и Приземление) ---
        if (isJumping && !isMidAir)
        {
            // Мы в анимации Start Jump или Jump to Stand. Стоим на месте.
            velocity.x = 0f;
            velocity.z = 0f;
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            // Движение работает и на земле, и В ПОЛЕТЕ (isMidAir == true)
            if (move.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

                animator.SetFloat("Speed", move.magnitude);
                velocity.x = move.x * moveSpeed;
                velocity.z = move.z * moveSpeed;
            }
            else
            {
                animator.SetFloat("Speed", 0f);

                // Гасим инерцию ТОЛЬКО если мы на земле. В воздухе игрок должен сохранять инерцию!
                if (!isMidAir)
                {
                    velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * 5f);
                    velocity.z = Mathf.Lerp(velocity.z, 0, Time.deltaTime * 5f);
                }
            }
        }

        // Применяем силу прыжка, когда анимация Start Jump закончилась
        if (isJumping && canApplyJumpForce)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            canApplyJumpForce = false;
        }

        // Гравитация и применение скорости
        velocity.y += gravity * Time.deltaTime;
        rb.linearVelocity = velocity;
    }

    void Jump()
    {
        if (isGrounded && !isJumping)
        {
            isJumping = true;
            isMidAir = false;
            isFalling = false;
            hasLanded = false;

            animator.SetBool("IsFalling", false);
            animator.SetBool("HasLanded", false);

            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0; camRight.y = 0;
            jumpDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
            if (jumpDirection.magnitude < 0.1f) jumpDirection = transform.forward;

            animator.SetTrigger("JumpStart");
        }
    }

    // Animation Event: вешается на ПОСЛЕДНИЙ кадр анимации Start Jump
    public void OnStartJumpEnd()
    {
        isMidAir = true;
        canApplyJumpForce = true;
    }

    // Animation Event: вешается на ПОСЛЕДНИЙ кадр анимации Jump to Stand
    public void OnJumpToStandEnd()
    {
        isJumping = false;
        isMidAir = false;
    }
}