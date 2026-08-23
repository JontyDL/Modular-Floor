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

        if (TargetHealth != null)
            TargetHealth.OnDeath -= HandleTargetDeath;

        if (AttackRoutine != null) StopCoroutine(AttackRoutine);
        TargetHealth = Target;

        if (TargetHealth != null)
        {
            TargetHealth.OnDeath += HandleTargetDeath;
            AttackRoutine = StartCoroutine(AttackLoop());
        }
        else
        {
            AttackRoutine = null;
        }
    }

    public void StopAttacking()
    {
        if (TargetHealth != null)
            TargetHealth.OnDeath -= HandleTargetDeath;

        if (AttackRoutine != null) StopCoroutine(AttackRoutine);
        AttackRoutine = null;
        TargetHealth = null;
    }

    private void HandleTargetDeath()
    {
        // Pooled targets (e.g. enemies) survive as deactivated-but-not-destroyed
        // objects, so we can't rely on TargetHealth going fake-null anymore -
        // OnDeath is now the source of truth for "this target is gone".
        StopAttacking();
        OnTargetLost?.Invoke();
    }

    private IEnumerator AttackLoop()
    {
        while (TargetHealth != null)
        {
            float WaitTime = NextAttackTime - Time.time;
            if (WaitTime > 0f)
                yield return new WaitForSeconds(WaitTime);

            Health CurrentTarget = TargetHealth;
            if (CurrentTarget == null) break;

            CurrentTarget.TakeDamage(AttackDamage);
            float Interval = AttackRate > 0f ? 1f / AttackRate : 1f;
            NextAttackTime = Time.time + Interval;

            // TakeDamage may have killed the target and, via HandleTargetDeath, already
            // cleared TargetHealth and stopped this coroutine - guard before touching it again.
            if (TargetHealth == CurrentTarget && !CurrentTarget.IsDead)
            {
                ApplyKnockbackIfEnemy(CurrentTarget);
            }
        }

        AttackRoutine = null;
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