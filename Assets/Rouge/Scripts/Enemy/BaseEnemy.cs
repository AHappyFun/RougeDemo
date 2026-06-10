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
        private float lastDamageTime;
        private const float DamageCD = 1f;

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.updateRotation = false;
                if (agent.isOnNavMesh)
                    agent.Warp(transform.position);
            }
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
