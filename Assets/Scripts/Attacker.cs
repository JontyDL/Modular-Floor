using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Attacker : MonoBehaviour
{
    [SerializeField] private float AttackDamage = 10f;
    [SerializeField] private float AttackRate = 1f;   // attacks per second

    private Health TargetHealth;
    private Coroutine AttackRoutine;

    public bool HasTarget => TargetHealth != null;
    public Transform CurrentTargetTransform => TargetHealth != null ? TargetHealth.transform : null;

    public event System.Action OnTargetLost;            // when our target dies

    public void SetStats(float Damage, float Rate)
    {
        AttackDamage = Damage;
        AttackRate = Rate;
    }

    public void SetTarget(Health Target)
    {
        if (TargetHealth == Target) return;

        if (AttackRoutine != null) StopCoroutine(AttackRoutine);
        TargetHealth = Target;
        AttackRoutine = TargetHealth != null ? StartCoroutine(AttackLoop()) : null;
    }

    public void StopAttacking()
    {
        if (AttackRoutine != null) StopCoroutine(AttackRoutine);
        AttackRoutine = null;
        TargetHealth = null;
    }

    private IEnumerator AttackLoop()
    {
        float Interval = AttackRate > 0f ? 1f / AttackRate : 1f;
        WaitForSeconds Wait = new WaitForSeconds(Interval);

        while (TargetHealth != null)
        {
            TargetHealth.TakeDamage(AttackDamage);
            yield return Wait;
        }

        // TargetHealth went fake-null, meaning the target GameObject was destroyed.
        AttackRoutine = null;
        OnTargetLost?.Invoke();
    }
}
