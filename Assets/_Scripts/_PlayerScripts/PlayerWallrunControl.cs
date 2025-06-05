using System;
using Mirror;
using UnityEngine;

namespace _Scripts._PlayerScripts
{
    public class PlayerWallrunControl : NetworkBehaviour
    {
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
        [Header("Detection")] public float wallCheckDistance;
        public float minJumpHeight;
        private RaycastHit leftWall;
        private RaycastHit rightWall;
        private bool isWallLeft;
        private bool isWallRight;
        [Header("References")] public Transform orientation;
        private PlayerMoveControl playerMoveControl;
        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            playerMoveControl = GetComponent<PlayerMoveControl>();
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                WallCheck();
                WallRunStateMachine();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void FixedUpdate()
        {
            if (isLocalPlayer)
            {
                if (playerMoveControl.isWallRunning)
                {
                    WallRun();
                }
            }
        }

        private void WallCheck()
        {
            isWallRight = Physics.Raycast(transform.position, orientation.right, out rightWall, wallCheckDistance,
                wallLayer);
            isWallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWall, wallCheckDistance,
                wallLayer);
            //Debug ray 
            Debug.DrawRay(transform.position, orientation.right * wallCheckDistance,
                isWallRight ? Color.green : Color.red);
            Debug.DrawRay(transform.position, -orientation.right * wallCheckDistance,
                isWallLeft ? Color.green : Color.red);
        }

        private bool AboveGround()
        {
            return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, groundLayer);
        }

        private void WallRunStateMachine()
        {
            //lấy H và V input
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
            // state 1 Wallrunning 
            if ((isWallLeft || isWallRight) && verticalInput > 0 && AboveGround() && !isExitingWall)
            {
                if (!playerMoveControl.isWallRunning)
                {
                    StartWallRun();
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
                    WallJump();
                }
            }
            // State 2 Exiting Wall
            else if (isExitingWall)
            {
                if (playerMoveControl.isWallRunning)
                {
                    StopWallRun();
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
            //State 3 
            else
            {
                if (playerMoveControl.isWallRunning)
                {
                    StopWallRun();
                }
            }
        }

        private void StartWallRun()
        {
            playerMoveControl.isWallRunning = true;
            wallRunTimer = maxWallRunTime;
        }

        private void WallRun()
        {
            rb.useGravity = false;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
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

        private void StopWallRun()
        {
            playerMoveControl.isWallRunning = false;
        }

        private void WallJump()
        {
            // state enter , exit của wall run
            isExitingWall = true;
            exitingWallTimer = exitingWallTime;
            /////////////////////////////////
            Vector3 wallNormal = isWallRight ? rightWall.normal : leftWall.normal;
            Vector3 force = transform.up * wallRunUpForce + wallNormal * wallRunSideForce;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}