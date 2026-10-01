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

    [Header("Настройки камеры")]
    public float lookSensitivity = 2f;

    private Rigidbody rb;
    private Animator animator;
    private PlayerControls inputActions;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Замораживаем вращение по X и Z, чтобы персонаж не падал
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        inputActions = new PlayerControls();

        // Подписываемся на инпуты
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => Jump();
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void Update()
    {
        // Проверка земли через луч вниз
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);

        // Если на земле и падаем — обнуляем вертикальную скорость
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        // Движение
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);

        if (move.magnitude >= 0.1f)
        {
            // Поворот в направлении движения
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            animator.SetFloat("Speed", move.magnitude);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }

        // Применяем горизонтальное движение
        velocity.x = move.x * moveSpeed;
        velocity.z = move.z * moveSpeed;

        // Вращение мышью (горизонтальное)
        if (lookInput.x != 0)
        {
            transform.Rotate(Vector3.up, lookInput.x * lookSensitivity);
        }

        // Гравитация
        velocity.y += gravity * Time.deltaTime;

        // Применяем скорость к Rigidbody
        rb.linearVelocity = velocity;
    }

    void Jump()
    {
        if (isGrounded)
        {
            // Формула прыжка
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            animator.SetTrigger("Jump");
        }
    }
}