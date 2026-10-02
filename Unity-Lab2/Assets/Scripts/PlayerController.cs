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
    private bool canApplyJumpForce;
    private Vector3 jumpDirection;

    // Новые флаги для аниматора
    private bool isFalling;   // true, когда высота начинает уменьшаться
    private bool hasLanded;   // true, когда игрок коснулся земли после прыжка

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
            animator.SetBool("HasLanded", true); // Сообщаем аниматору, что коснулись земли
        }

        // --- ЛОГИКА ПАДЕНИЯ ---
        if (isJumping && velocity.y < 0 && !isFalling)
        {
            isFalling = true;
            animator.SetBool("IsFalling", true); // Сообщаем аниматору, что полетели вниз
        }

        // Получаем направления камеры
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = (camForward * moveInput.y + camRight * moveInput.x);

        // --- БЛОКИРОВКА ДВИЖЕНИЯ ВО ВРЕМЯ ПРЫЖКА ---
        // Если идет прыжок, мы ИГНОРИРУЕМ ввод движения и поворота (стоит на месте)
        if (isJumping)
        {
            velocity.x = 0f;
            velocity.z = 0f;
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            // Обычное движение на земле
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
                velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * 5f);
                velocity.z = Mathf.Lerp(velocity.z, 0, Time.deltaTime * 5f);
            }
        }

        // Применяем силу прыжка, когда анимация Start Jump закончилась
        if (isJumping && canApplyJumpForce)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

            // (Опционально) Если хочешь, чтобы он чуть-чуть летел вперед, раскомментируй строки ниже, 
            // но по ТЗ он должен стоять на месте, поэтому пока оставим 0.
            // velocity.x = jumpDirection.x * moveSpeed * 0.3f;
            // velocity.z = jumpDirection.z * moveSpeed * 0.3f;

            canApplyJumpForce = false;
            animator.SetTrigger("JumpMid"); // Запускаем анимацию полета вверх
        }

        // Гравитация и применение скорости
        velocity.y += gravity * Time.deltaTime;
        rb.linearVelocity = velocity; // Для Unity 6
    }

    void Jump()
    {
        if (isGrounded && !isJumping)
        {
            isJumping = true;
            isFalling = false;
            hasLanded = false;

            // Сбрасываем флаги в аниматоре
            animator.SetBool("IsFalling", false);
            animator.SetBool("HasLanded", false);

            // Сохраняем направление (на случай, если захочешь добавить инерцию)
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
        canApplyJumpForce = true;
    }

    // Animation Event: вешается на ПОСЛЕДНИЙ кадр анимации Jump to Stand
    public void OnJumpToStandEnd()
    {
        isJumping = false; // Прыжок полностью завершен, можно снова ходить
    }
}