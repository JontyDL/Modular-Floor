using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class Attacker : MonoBehaviour
{
    [SerializeField] private float AttackDamage = 10f;
    [SerializeField] private float AttackRate = 1f;   // attacks per second

    [Header("Knockback")]
    [Tooltip("Only applied to targets tagged \"Enemy\" - e.g. buildings hitting AI, not the other way round.")]
    [SerializeField] private float KnockbackForce = 2f;

    private Health TargetHealth;
    private Coroutine AttackRoutine;
    private float NextAttackTime;   // absolute Time.time; persists across SetTarget calls so knocking an enemy out and back in of range doesn't bypass the cooldown

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
        while (TargetHealth != null)
        {
            float WaitTime = NextAttackTime - Time.time;
            if (WaitTime > 0f)
                yield return new WaitForSeconds(WaitTime);

            // Target may have been lost, killed, or swapped out while we were waiting.
            if (TargetHealth == null) break;

            TargetHealth.TakeDamage(AttackDamage);
            float Interval = AttackRate > 0f ? 1f / AttackRate : 1f;
            NextAttackTime = Time.time + Interval;

            if (!TargetHealth.IsDead)                       // if it's going to di this frame anyway, no need to calculate knockback
            {
                ApplyKnockbackIfEnemy(TargetHealth);
            }
        }

        // TargetHealth went fake-null, meaning the target GameObject was destroyed.
        AttackRoutine = null;
        OnTargetLost?.Invoke();
    }

    private void ApplyKnockbackIfEnemy(Health Target)           // obviously the enemy cant knockback the building
    {
        if (Target == null || KnockbackForce <= 0f) return;
        if (!Target.CompareTag("Enemy")) return;

        NavMeshAgent TargetAgent = Target.GetComponent<NavMeshAgent>();
        if (TargetAgent == null) return;

        Vector3 Direction = Target.transform.position - transform.position;
        Direction.y = 0f;
        if (Direction.sqrMagnitude < 0.0001f) Direction = Target.transform.forward;
        Direction.Normalize();

        TargetAgent.Move(Direction * KnockbackForce);           // displaces the agent, but keeps it on the navmesh
    }
}