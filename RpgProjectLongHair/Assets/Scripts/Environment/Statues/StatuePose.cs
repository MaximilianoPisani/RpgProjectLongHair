using UnityEngine;

[ExecuteAlways]
public class StatuePose : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip clip;
    [SerializeField] private int frame;

    private void OnValidate()
    {
        ApplyPose();
    }

    private void Start()
    {
        ApplyPose();
    }

    private void ApplyPose()
    {
        if (animator == null || clip == null)
            return;

        if (!animator.isActiveAndEnabled)
            return;

        float normalizedTime = frame / (clip.length * clip.frameRate);

        animator.Play(0, 0, normalizedTime);
        animator.Update(0f);
        animator.speed = 0f;
        animator.enabled = false;
    }
}