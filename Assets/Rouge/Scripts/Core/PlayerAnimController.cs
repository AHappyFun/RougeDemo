using UnityEngine;

namespace Rouge
{
    /// <summary>控制主角模型的动画播放，由其他组件驱动</summary>
    public class PlayerAnimController : MonoBehaviour
    {
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void SetSpeed(float value)
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetFloat("speed", value);
        }

        public void PlayAttack()
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetTrigger("attack");
        }

        public void PlayHurt()
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetTrigger("hurt");
        }

        public void PlayDead()
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetBool("isDead", true);
        }

        public void Revive()
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetBool("isDead", false);
        }
    }
}
