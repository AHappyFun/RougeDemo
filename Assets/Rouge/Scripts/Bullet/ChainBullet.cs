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
        private HashSet<int> hitTargetIDs = new HashSet<int>();
        private LineRenderer lineRenderer;

        private enum State { Traveling, Zap }
        private State state = State.Traveling;
        private float zapTimer;
        private const float ZapDuration = 0.1f;
        private Vector3 zapFrom;
        private Vector3 zapTo;
        private Transform nextTarget;

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
            hitTargetIDs.Clear();
            speed = 25f;
            lifetime = 5f;
            state = State.Traveling;
            nextTarget = null;

            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.5f;
            lineRenderer.endWidth = 0.2f;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            lineRenderer.material = new Material(shader);
            lineRenderer.startColor = new Color(0.3f, 0.8f, 1f, 1f);
            lineRenderer.endColor = new Color(0f, 0.3f, 1f, 0.6f);
            lineRenderer.enabled = false;

            startPos = transform.position;
            targetPos = firstTarget.position;
            travelProgress = 0f;
        }

        // Disable base trigger — chain handles damage manually
        protected override void OnTriggerEnter(Collider other) { }

        protected override void Update()
        {
            if (GameManager.IsPaused) return;
            base.Update();

            switch (state)
            {
                case State.Traveling: UpdateTravel(); break;
                case State.Zap:       UpdateZap();    break;
            }
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

            // Face toward target (XZ plane, Y-up)
            Vector3 dir = (targetPos - transform.position).normalized;
            if (dir != Vector3.zero)
            {
                float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, angle, 0);
            }

            if (t >= 1f || Vector3.Distance(transform.position, targetPos) < 0.15f)
            {
                chainCount++;

                // Capture position before target may be destroyed by damage
                Vector3 hitPos = currentTarget.position;

                if (currentTarget != null)
                {
                    var health = currentTarget.GetComponent<EnemyHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(damage);
                        MeshGenerator.SpawnHitParticles(hitPos, Color.cyan);
                    }
                    hitTargetIDs.Add(currentTarget.GetInstanceID());
                }

                if (chainCount >= maxChains)
                {
                    Destroy(gameObject);
                    return;
                }

                // Find next target to chain to
                nextTarget = FindNextChainTarget();
                if (nextTarget == null)
                {
                    Destroy(gameObject);
                    return;
                }

                // Show lightning zap from hit position to next target (above ground)
                zapFrom = hitPos + Vector3.up * 0.2f;
                zapTo = nextTarget.position + Vector3.up * 0.2f;
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, zapFrom);
                lineRenderer.SetPosition(1, zapTo);
                zapTimer = ZapDuration;
                state = State.Zap;
            }
        }

        private void UpdateZap()
        {
            zapTimer -= Time.deltaTime;
            // Brief flash of the lightning line, then fly to next target
            if (zapTimer <= 0f)
            {
                lineRenderer.enabled = false;
                currentTarget = nextTarget;
                nextTarget = null;
                if (currentTarget == null) { Destroy(gameObject); return; }
                startPos = transform.position;
                targetPos = currentTarget.position;
                travelProgress = 0f;
                state = State.Traveling;
            }
        }

        private Transform FindNextChainTarget()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Transform nearest = null;
            float minDist = chainRange;

            foreach (var obj in enemies)
            {
                int id = obj.transform.GetInstanceID();
                if (hitTargetIDs.Contains(id)) continue;
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
