using System.Collections.Generic;
using UnityEngine;

namespace Rouge
{
    public class ChainBullet : BaseBullet
    {
        private Transform currentTarget;
        private int chainCount;
        private int maxChains = 3;
        private float chainRange = 5f;
        private List<Transform> hitTargets = new List<Transform>();
        private LineRenderer lineRenderer;

        private enum State { Traveling, Cooldown }
        private State state = State.Traveling;
        private float cooldownTimer;

        private Vector3 startPos;
        private Vector3 targetPos;
        private float travelProgress;

        public void Init(Transform firstTarget, int maxChainsOverride = -1, float chainRangeOverride = -1f)
        {
            if (maxChainsOverride > 0) maxChains = maxChainsOverride;
            if (chainRangeOverride > 0f) chainRange = chainRangeOverride;

            damage = 12;
            chainCount = 0;
            currentTarget = firstTarget;
            hitTargets.Clear();
            speed = 25f;
            lifetime = 5f;
            state = State.Traveling;
            cooldownTimer = 0f;

            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.06f;
            lineRenderer.endWidth = 0.03f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(0.5f, 0.8f, 1f, 0.8f);
            lineRenderer.endColor = new Color(0f, 0.5f, 1f, 0.4f);

            startPos = transform.position;
            targetPos = firstTarget.position;
            travelProgress = 0f;
        }

        // Disable base trigger — chain handles damage manually
        protected override void OnTriggerEnter(Collider other) { }

        protected override void Update()
        {
            base.Update();

            switch (state)
            {
                case State.Traveling:
                    UpdateTravel();
                    break;
                case State.Cooldown:
                    cooldownTimer -= Time.deltaTime;
                    if (cooldownTimer <= 0f)
                    {
                        if (chainCount >= maxChains) { Destroy(gameObject); return; }
                        currentTarget = FindNextChainTarget();
                        if (currentTarget == null) { Destroy(gameObject); return; }
                        startPos = transform.position;
                        targetPos = currentTarget.position;
                        travelProgress = 0f;
                        state = State.Traveling;
                    }
                    break;
            }

            UpdateLine();
        }

        private void UpdateTravel()
        {
            if (currentTarget == null)
            {
                Destroy(gameObject);
                return;
            }

            travelProgress += Time.deltaTime * speed;
            float dist = Vector3.Distance(startPos, targetPos);
            float t = dist > 0.001f ? travelProgress / dist : 1f;
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            // Face toward target
            Vector3 dir = (targetPos - transform.position).normalized;
            if (dir != Vector3.zero)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            if (t >= 1f || Vector3.Distance(transform.position, targetPos) < 0.15f)
            {
                // Hit current target
                if (currentTarget != null)
                {
                    var health = currentTarget.GetComponent<EnemyHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(damage);
                        MeshGenerator.SpawnHitParticles(currentTarget.position, Color.cyan);
                    }
                    hitTargets.Add(currentTarget);
                }

                chainCount++;
                if (chainCount >= maxChains)
                {
                    Destroy(gameObject);
                    return;
                }

                // Brief visual pause before next chain
                state = State.Cooldown;
                cooldownTimer = 0.08f;
            }
        }

        private void UpdateLine()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && lineRenderer != null)
            {
                lineRenderer.SetPosition(0, player.transform.position);
                lineRenderer.SetPosition(1, transform.position);
            }
        }

        private Transform FindNextChainTarget()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform nearest = null;
            float minDist = chainRange;

            foreach (var obj in enemies)
            {
                if (hitTargets.Contains(obj.transform)) continue;
                float d = Vector3.Distance(transform.position, obj.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = obj.transform;
                }
            }
            return nearest;
        }
    }
}
