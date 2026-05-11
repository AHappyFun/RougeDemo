using UnityEngine;
using UnityEngine.AI;

namespace Rouge
{
    public class BaseEnemy : MonoBehaviour
    {
        public float moveSpeed = 2f;
        public int contactDamage = 10;

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
                agent.stoppingDistance = 0.3f;
                agent.updateRotation = false;
            }
        }

        private void Update()
        {
            if (GameManager.IsPaused) return;
            if (player == null) return;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(player.position);
            }
            else
            {
                // Fallback: direct movement when no NavMesh
                Vector3 dir = (player.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;
            }
            transform.LookAt(player);
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
