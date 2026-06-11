using UnityEngine;
using UnityEngine.AI;

namespace Rouge
{
    public class BaseEnemy : MonoBehaviour
    {
        public float moveSpeed = 2f;
        public int contactDamage = 10;
        public int xpReward = 10;

        private Transform player;
        private NavMeshAgent agent;
        private Animator animator;
        private float lastDamageTime;
        private const float DamageCD = 1f;
        private Vector3 lastPosition;

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.updateRotation = false;
                if (agent.isOnNavMesh)
                    agent.Warp(transform.position);
            }
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (GameManager.IsPaused)
            {
                if (agent != null && agent.isOnNavMesh && !agent.isStopped)
                    agent.isStopped = true;
                return;
            }
            if (agent != null && agent.isOnNavMesh && agent.isStopped)
                agent.isStopped = false;

            if (player == null) return;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(player.position);
                transform.LookAt(player);
            }
            else
            {
                Vector3 dir = (player.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;
                transform.LookAt(player);
            }

            // 动画速度参数：是否在移动
            float speed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
            if (animator != null)
                animator.SetFloat("speed", speed > 0.1f ? 1f : 0f);
            lastPosition = transform.position;
        }

        private void OnTriggerStay(Collider other)
        {
            if (GameManager.IsPaused) return;
            if (!other.CompareTag("Player")) return;
            if (Time.time - lastDamageTime < DamageCD) return;

            var health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(contactDamage);
                lastDamageTime = Time.time;
            }
        }
    }
}
