using System;
using Mirror;
using UnityEngine;

namespace _Scripts._PlayerScripts
{
    public class PlayerWallrunControl : NetworkBehaviour
    {
        #region DATA

        [Header("Wall running")] public LayerMask wallLayer;
        public LayerMask groundLayer;
        public float wallRunForce;
        public float maxWallRunTime;
        public float wallRunUpForce;
        public float wallRunSideForce;
        private float wallRunTimer;
        [Header("Exiting Wall")] public bool isExitingWall;
        public float exitingWallTime;
        public float exitingWallTimer;
        [Header("Input")] public KeyCode wallJump = KeyCode.Space;
        private float horizontalInput;
        private float verticalInput;
        public float wallCheckDistance;
        public float minJumpHeight;
        private RaycastHit leftWall;
        private RaycastHit rightWall;
        private bool isWallLeft;
        private bool isWallRight;
        public Transform orientation;
        private PlayerMoveControl playerMoveControl;
        private Rigidbody rb;
        // THÊM PredictedRigidbody

        #endregion

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            playerMoveControl = GetComponent<PlayerMoveControl>();
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            WallCheck();
            WallRunStateMachine();
        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (playerMoveControl.isWallRunning)
            {
                WallRun();
            }
        }

        private void WallCheck()
        {
            isWallRight = Physics.Raycast(transform.position, orientation.right, out rightWall, wallCheckDistance,
                wallLayer);
            isWallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWall, wallCheckDistance,
                wallLayer);
            //Debug ray 
            if (isLocalPlayer)
            {
                Debug.DrawRay(transform.position, orientation.right * wallCheckDistance,
                    isWallRight ? Color.green : Color.red);
                Debug.DrawRay(transform.position, -orientation.right * wallCheckDistance,
                    isWallLeft ? Color.green : Color.red);
            }
        }

        private bool AboveGround()
        {
            return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, groundLayer);
        }

        private void WallRunStateMachine()
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
            if ((isWallLeft || isWallRight) && verticalInput > 0 && AboveGround() && !isExitingWall)
            {
                if (!playerMoveControl.isWallRunning)
                {
                    CmdStatWallRun();
                }

                if (wallRunTimer > 0)
                {
                    wallRunTimer -= Time.deltaTime;
                }

                if (wallRunTimer <= 0 && playerMoveControl.isWallRunning)
                {
                    isExitingWall = true;
                    exitingWallTimer = exitingWallTime;
                }

                if (Input.GetKeyDown(wallJump))
                {
                    CmdWallJump(isWallLeft, isWallRight, leftWall.normal, rightWall.normal);
                }
            }
            else if (isExitingWall)
            {
                if (playerMoveControl.isWallRunning)
                {
                    CmdStopWallRun();
                }

                if (exitingWallTimer > 0)
                {
                    exitingWallTimer -= Time.deltaTime;
                }

                if (exitingWallTimer <= 0)
                {
                    isExitingWall = false;
                }
            }
            else
            {
                if (playerMoveControl.isWallRunning)
                {
                    CmdStopWallRun();
                }
            }
        }

        [Command]
        private void CmdStatWallRun()
        {
            playerMoveControl.isWallRunning = true;
            wallRunTimer = maxWallRunTime;
        }

        [Command]
        private void CmdStopWallRun()
        {
            playerMoveControl.isWallRunning = false;
            // Sử dụng predictedRigidbody để đảm bảo tương thích với prediction
            rb.useGravity = true;
        }

        [Command]
        private void CmdWallJump(bool isWLeft, bool isWRight, Vector3 leftWallNormal, Vector3 rightWallNormal)
        {
            isExitingWall = true;
            exitingWallTimer = exitingWallTime;
            Vector3 wallNormal = isWRight ? rightWallNormal : leftWallNormal;
            Vector3 force = transform.up * wallRunUpForce + wallNormal * wallRunSideForce;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(force, ForceMode.Impulse);
            CmdStopWallRun();
        }

        private void WallRun()
        {
            // rb.useGravity = false; // Đã chuyển vào CmdStatWallRun
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Xóa vận tốc Y trên server
            Vector3 wallNormal = isWallRight ? rightWall.normal : leftWall.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
            if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            {
                wallForward = -wallForward;
            }

            rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
            // push to wall force
            if (!(isWallLeft && horizontalInput > 0) && !(isWallRight && horizontalInput < 0))
                rb.AddForce(-wallNormal * 100, ForceMode.Force);
        }
    }
}