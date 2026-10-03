using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Настройки прыжка")]
    public float jumpForce = 55f;

        [Header("Настройки проверки земли")]
    public float groundCheckNormalThreshold = 0.7f;

    private Rigidbody rb;
    private Animator animator;
    private PlayerControls inputActions;
    private Camera mainCamera;

    private Vector2 moveInput;
    private Vector3 targetVelocity;

    private bool isGrounded;
    private bool isJumping;
    private bool isMidAir;
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
        if (isJumping && isGrounded && !hasLanded)
        {
            hasLanded = true;
            animator.SetBool("HasLanded", true);
        }

        if (isJumping && rb.linearVelocity.y < 0 && !isFalling)
        {
            isFalling = true;
            animator.SetBool("IsFalling", true);
        }
    }

    void FixedUpdate()
    {
        if (isGrounded && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = (camForward * moveInput.y + camRight * moveInput.x);

        if (isJumping && !isMidAir)
        {
            targetVelocity = Vector3.zero;
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            if (move.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);

                animator.SetFloat("Speed", move.magnitude);
                targetVelocity.x = move.x * moveSpeed;
                targetVelocity.z = move.z * moveSpeed;
            }
            else
            {
                animator.SetFloat("Speed", 0f);

                if (!isMidAir)
                {
                    targetVelocity.x = Mathf.Lerp(targetVelocity.x, 0, Time.fixedDeltaTime * 5f);
                    targetVelocity.z = Mathf.Lerp(targetVelocity.z, 0, Time.fixedDeltaTime * 5f);
                }
            }
        }

                rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

        private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > groundCheckNormalThreshold)
                {
                    isGrounded = true;
                    isMidAir = false;
                    break;
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
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
            animator.SetTrigger("JumpStart");
        }
    }

    public void OnStartJumpEnd()
    {
        isMidAir = true;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void OnJumpToStandEnd()
    {
        isJumping = false;
        isMidAir = false;
    }
}