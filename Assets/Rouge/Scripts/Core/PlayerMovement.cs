using UnityEngine;
using UnityEngine.AI;

namespace Rouge
{
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 6f;
        public PlayerStats stats;

        private NavMeshAgent agent;

        private void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            if (agent != null && agent.isOnNavMesh)
                agent.Warp(transform.position);
        }

        private void Update()
        {
            if (GameManager.IsPaused) return;

            float spd = stats != null ? stats.moveSpeed : speed;
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 move = new Vector3(h, 0, v).normalized * spd * Time.deltaTime;

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
