using UnityEngine;

public class EnemyGirlController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController animatorController;
    private bool isFacingForward = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            if (!animator.enabled)
            {
                animator.enabled = true;
                Debug.LogWarning("EnemyGirlController: Animator found but disabled. Enabling now.");
            }

            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
                Debug.Log("EnemyGirlController: AnimatorController set.");
            }
            else
            {
                Debug.LogWarning("EnemyGirlController: animatorController is null!");
            }

            Debug.Log("EnemyGirlController: Animator initialized successfully.");
        }
        else
        {
            Debug.LogWarning("EnemyGirlController: No Animator component found!");
        }
    }

    public void TurnBack()
    {
        if (isFacingForward)
        {
            if (animator != null)
            {
                animator.SetTrigger("Turn Back");
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            isFacingForward = false;
            Debug.Log("EnemyGirlController: Turned back.");
        }
    }

    public void TurnForward()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("EnemyGirlController: Cannot turn forward because object is inactive.");
            return;
        }

        if (!isFacingForward)
        {
            Debug.Log(">>> TurnForward() called");

            if (animator != null)
            {
                Debug.Log("Available parameters:");
                foreach (var param in animator.parameters)
                {
                    Debug.Log(param.name);
                }

                animator.ResetTrigger("Turn Back");
                animator.SetTrigger("Turn Forwerd");
                Debug.Log(">>> Trigger Turn Forwerd sent");

                isFacingForward = true;

                StartCoroutine(CheckAnimatorState());
            }
            else
            {
                Debug.LogWarning(">>> Animator is null!");
            }
        }
    }

    public void EnsureForwardDuringRed()
    {
        if (!isFacingForward)
        {
            Debug.Log("EnsureForwardDuringRed: Not facing forward, forcing TurnForward.");
            TurnForward();
        }
    }

    private System.Collections.IEnumerator CheckAnimatorState()
    {
        yield return new WaitForSeconds(0.1f);
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"Current State: {stateInfo.fullPathHash}, IsName Turn Forwerd? {stateInfo.IsName("turn Forwerd")}");
    }
}
