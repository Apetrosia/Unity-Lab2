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
    private Camera mainCamera; // Ссылка на камеру

    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main; // Находим главную камеру

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

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        // 1. Получаем направления камеры (игнорируем наклон по Y, чтобы персонаж не летал)
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // 2. Считаем итоговое направление движения относительно камеры
        Vector3 move = (camForward * moveInput.y + camRight * moveInput.x);

        if (move.magnitude >= 0.1f)
        {
            // Поворот персонажа в сторону ДВИЖЕНИЯ (а не мышки)
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            animator.SetFloat("Speed", move.magnitude);

            // Движение
            velocity.x = move.x * moveSpeed;
            velocity.z = move.z * moveSpeed;
        }
        else
        {
            animator.SetFloat("Speed", 0f);
            // Плавная остановка
            velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * 5f);
            velocity.z = Mathf.Lerp(velocity.z, 0, Time.deltaTime * 5f);
        }

        velocity.y += gravity * Time.deltaTime;
        rb.linearVelocity = velocity;
    }

    void Jump()
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            animator.SetTrigger("Jump");
        }
    }
}