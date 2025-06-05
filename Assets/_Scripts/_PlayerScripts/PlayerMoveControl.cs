// _Scripts/_PlayerScripts/PlayerMoveControl.cs
using TMPro;
using UnityEngine;
using Mirror;

namespace _Scripts._PlayerScripts
{
    public class PlayerMoveControl : NetworkBehaviour
    {
       
        public TMP_Text SpeedText;
        public TMP_Text StateText;

        [Header("Movement")]
        private float moveSpeed;
        public float walkSpeed;
        public float sprintSpeed;
        public float groundDrag;
        public float wallRunSpeed;

        [Header("Jumping")]
        public float jumpForce;
        public float jumpCooldown;
        public float airMultiplier;
        bool readyToJump;

        [Header("Keybindings")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode sprintKey = KeyCode.LeftShift;
        public KeyCode crouchKey = KeyCode.C;

        [Header("Crouching")]
        public float crouchSpeed;
        public float crouchYScale;
        private float starYScale;

       
        public float maxSlopeAngle;
        private RaycastHit slopeHit;
        private bool isOnSlope;

        [Header("Ground Checking")]
        public float playerHeight;
        public LayerMask groundLayer;
        bool isGrounded;

       
        public Transform orientation;
        float horizontalInput;
        float verticalInput;
        private Vector3 moveDirection;
        private Rigidbody rb;
        public MoveState moveState;
        public bool isWallRunning;

        // private PredictedRigidbody predictedRb;

        public enum MoveState
        {
            Walking,
            Crouching,
            Sprinting,
            Air,
            WallRunning,
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            readyToJump = true;
            starYScale = transform.localScale.y;
            
            if (!isLocalPlayer)
            {
                if (SpeedText!= null) SpeedText.gameObject.SetActive(false);
                if (StateText!= null) StateText.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (!isLocalPlayer) return;

            isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, groundLayer);

            ReadRawInput();
            StateHandler();
            Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.3f), Color.red);

            if (isGrounded)
            {
                rb.drag = groundDrag;
            }
            else
            {
                rb.drag = 0f;
            }
        }

        private void StateHandler()
        {
            if (isWallRunning)
            {
                moveState = MoveState.WallRunning;
                moveSpeed = wallRunSpeed;
            }
            else if (Input.GetKey(crouchKey))
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

            if (SpeedText!= null) SpeedText.SetText("Speed : " + rb.velocity.magnitude.ToString("F1"));
            if (StateText!= null) StateText.SetText("State : " + moveState);
        }

        void FixedUpdate()
        {
            
            // Server cũng xử lý vật lý để là authoritative
            if (isServer)
            {
                MovePlayer();
                SpeedControl();
            }
        }

        private void ReadRawInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            if (SlopeCheck() &&!isOnSlope)
            {
                // Áp dụng lực cục bộ và gửi lệnh
                Vector3 slopeForce = GetSlopeMoveDirection() * (moveSpeed * 20f);
                rb.AddForce(slopeForce, ForceMode.Force);
                CmdApplyForce(slopeForce, ForceMode.Force);

                if (rb.velocity.y > 0)
                {
                    rb.AddForce(Vector3.down * 80f, ForceMode.Force);
                    CmdApplyForce(Vector3.down * 80f, ForceMode.Force);
                }
            }

            if (Input.GetKey(jumpKey) && readyToJump && isGrounded)
            {
                readyToJump = false;
                CmdJump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }

            if (Input.GetKeyDown(crouchKey))
            {
                CmdSetCrouch(true);
            }
            if (Input.GetKeyUp(crouchKey))
            {
                CmdSetCrouch(false);
            }

            rb.useGravity =!SlopeCheck();
        }

        // COMMANDS: Các lệnh từ client gửi lên server để thực hiện hành động
        [Command]
        private void CmdJump()
        {
            // Logic nhảy chỉ thực hiện trên server
            isOnSlope = true;
            // Sử dụng predictedRigidbody để đảm bảo tương thích với prediction

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }

        [Command]
        private void CmdSetCrouch(bool isCrouching)
        {
            if (isCrouching)
            {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
           
                
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            }
            else
            {
                transform.localScale = new Vector3(transform.localScale.x, starYScale, transform.localScale.z);
            }
        }

        // THÊM COMMAND để gửi lực từ client lên server cho prediction
        [Command]
        private void CmdApplyForce(Vector3 force, ForceMode mode)
        {
            // Server cũng áp dụng lực này
            // Sử dụng predictedRigidbody để đảm bảo tương thích với prediction
        
            rb.AddForce(force, mode);
        }


        private void MovePlayer()
        {
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

            Vector3 forceToApply;
            if (isGrounded)
            {
                forceToApply = moveDirection.normalized * (moveSpeed * 10f);
            }
            else
            {
                forceToApply = moveDirection.normalized * (moveSpeed * 10f * airMultiplier);
            }

            // Áp dụng lực cục bộ cho phản hồi tức thì
            // Sử dụng predictedRigidbody để đảm bảo tương thích với prediction
           // Rigidbody currentRb = predictedRb!= null? predictedRb.predictedRigidbody : rb;
            rb.AddForce(forceToApply, ForceMode.Force);

            // Gửi lệnh lên server để server cũng áp dụng lực này
            CmdApplyForce(forceToApply, ForceMode.Force);
        }

        private void SpeedControl()
        {
            // Sử dụng predictedRigidbody để đảm bảo tương thích với prediction
            if (SlopeCheck() &&!isOnSlope)
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

        private void ResetJump()
        {
            readyToJump = true;
            isOnSlope = false;
        }

        private bool SlopeCheck()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f, groundLayer))
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                return angle < maxSlopeAngle && angle!= 0;
            }
            return false;
        }

        private Vector3 GetSlopeMoveDirection()
        {
            return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
        }
    }
}