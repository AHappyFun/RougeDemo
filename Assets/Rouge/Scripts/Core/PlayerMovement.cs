using UnityEngine;
using UnityEngine.AI;

namespace Rouge
{
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 6f;
        public PlayerStats stats;

        private NavMeshAgent agent;
        private PlayerAnimController animController;

        private void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            agent = GetComponent<NavMeshAgent>();
            animController = GetComponent<PlayerAnimController>();
        }

        private void Start()
        {
            // 有 NavMeshAgent 则对齐到 NavMesh
            if (agent != null && agent.isOnNavMesh)
                agent.Warp(transform.position);
        }

        private void Update()
        {
            if (GameManager.IsPaused) return;

            float spd = stats != null ? stats.moveSpeed : speed;
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 moveInput = new Vector3(h, 0, v);
            bool isMoving = moveInput.magnitude > 0.1f;

            // 动画状态
            if (animController != null)
                animController.SetSpeed(isMoving ? 1f : 0f);

            // 面朝移动方向
            if (isMoving)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveInput.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
            }

            Vector3 move = moveInput.normalized * spd * Time.deltaTime;

            // 优先走 NavMesh；无 NavMesh（编辑器测试）时直接平移
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Move(move);
            }
            else
            {
                transform.position += move;
            }
        }
    }
}
