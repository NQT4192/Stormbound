using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.PlayerScripts
{
    public class PlayerMoveControl : MonoBehaviour
    {
        // TODO: sử dụng ScriptableObject để dễ xử lý việc thay đổi giá trị và input thay vì gán trực tiếp (26/5/2025)(trung)
        [Header("Debug Info")]
        public TMP_Text SpeedText;
        public TMP_Text StateText;
        [Header("Movement")] private float moveSpeed;
        public float walkSpeed;
        public float sprintSpeed;
        public float groundDrag;

        [Header("Jumping")] public float jumpForce;
        public float jumpCooldown;
        public float airMultiplier;
        bool readyToJump;

        [Header("Keybindings")] public KeyCode jumpKey = KeyCode.Space;
        public KeyCode sprintKey = KeyCode.LeftShift;
        public KeyCode crouchKey = KeyCode.C;

        [Header("Crouching")] public float crouchSpeed;
        public float crouchYScale;
        private float starYScale;

        [Header("Slope Moving")] public float maxSlopeAngle;
        private RaycastHit slopeHit;
        private bool isOnSlope;
        [Header("Ground Checking")] public float playerHeight;
        public LayerMask groundLayer;
        bool isGrounded;

        [Header("Input Data")] public Transform orientation;
        float horizontalInput;
        float verticalInput;
        private Vector3 moveDirection;
        private Rigidbody rb;
        public MoveState moveState;
        [Header("Raycast Attack")]
public Transform handTransform; // Vị trí tay
public float rayLength = 10f;
public LayerMask hitLayer;      // Layer chứa các vật có thể bị đánh
public int damage = 10;
        public enum MoveState
        {
            Walking,
            Crouching,
            Sprinting,
            Air
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            readyToJump = true;
            starYScale = transform.localScale.y;
        }

        void Update()
        {
            

            // check ground bằng raycast hướng xuống 
            isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, groundLayer);
            //Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.3f), Color.red);

            ReadRawInput();
            SpeedControl();
            StateHandler();
            Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.3f), Color.red);

            if (isGrounded)
            {
                rb.drag = groundDrag;
                //Debug.Log("Player is ground");
            }
            else
            {
                rb.drag = 0f;
            }
            RaycastFromHand();
        }

        private void StateHandler()
        {
            if (Input.GetKey(crouchKey))
            {
                moveState = MoveState.Crouching;
                moveSpeed = crouchSpeed;
            }
            else if (isGrounded && Input.GetKey(sprintKey))
            {
                moveState = MoveState.Sprinting;
                moveSpeed = sprintSpeed;
            }
            else if (isGrounded)
            {
                moveState = MoveState.Walking;
                moveSpeed = walkSpeed;
            }

            else
            {
                moveState = MoveState.Air;
            }
        }

        void FixedUpdate()
        {
            //Debug info
            SpeedText.SetText("Speed : " + moveSpeed);
            StateText.SetText("State : " + moveState);
            ////////////
            MovePlayer();
        }

        // GetAxisRaw vì InputSystem gặp 1 vài bug khi tích hợp mirror-networking (dm unity)
        private void ReadRawInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            //Slope logic
            if (SlopeCheck() && !isOnSlope)
            {
                rb.AddForce(GetSlopeMoveDirection() * (moveSpeed * 20f), ForceMode.Force);
                if (rb.velocity.y > 0)
                {
                    rb.AddForce(Vector3.down * 80f, ForceMode.Force);
                }
            }
            // jump logic

            if (Input.GetKey(jumpKey) && readyToJump && isGrounded)
            {
                readyToJump = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }

            // crouch logic
            if (Input.GetKeyDown(crouchKey))
            {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            }

            if (Input.GetKeyUp(crouchKey))
            {
                transform.localScale = new Vector3(transform.localScale.x, starYScale, transform.localScale.z);
            }

            // tắt trọng lực khi đi vào slope(đoạn dốc)
            rb.useGravity = !SlopeCheck();
        }

        private void MovePlayer()
        {
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
            if (isGrounded)
            {
                rb.AddForce(moveDirection.normalized * (moveSpeed * 10f), ForceMode.Force);
            }
            else if (!isGrounded)
            {
                rb.AddForce(moveDirection.normalized * (moveSpeed * 10f * airMultiplier), ForceMode.Force);
            }
        }

        // đảm bảo speed của nhân vật ko vượt quá giá trị của var moveSpeed 
        private void SpeedControl()
        {
            if (SlopeCheck() && !isOnSlope)
            {
                if (rb.velocity.magnitude > moveSpeed)
                {
                    rb.velocity = rb.velocity.normalized * moveSpeed;
                }
            }
            else
            {
                Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                if (flatVelocity.magnitude > moveSpeed)
                {
                    Vector3 limitVelocity = flatVelocity.normalized * moveSpeed;
                    rb.velocity = new Vector3(limitVelocity.x, rb.velocity.y, limitVelocity.z);
                }
            }
        }

        private void Jump()
        {
            isOnSlope = true;
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }

        private void ResetJump()
        {
            readyToJump = true;
            isOnSlope = false;
        }

        private bool SlopeCheck()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                return angle < maxSlopeAngle && angle != 0;
            }

            Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.3f), Color.blue);

            return false;
        }

        private Vector3 GetSlopeMoveDirection()
        {
            return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
        }
        private void RaycastFromHand()
{
    Vector3 origin = handTransform.position;
    Vector3 direction = handTransform.forward;

    // Luôn vẽ tia đỏ để debug (chỉ hiển thị trong Scene View)
    Debug.DrawRay(origin, direction * rayLength, Color.red);

    // Kiểm tra xem ray có chạm vào vật thể nào hay không
    if (Physics.Raycast(origin, direction, out RaycastHit hit, rayLength, hitLayer))
    {
        // Nếu bấm chuột trái trong frame này
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Kiểm tra xem vật thể đó có component Health không
            Health health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }
}
    }
}