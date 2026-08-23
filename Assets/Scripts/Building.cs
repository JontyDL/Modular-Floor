using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Attacker))]
[RequireComponent(typeof(Collider))]
public class Building : MonoBehaviour
{
    private Health BuildingHealth;
    private Attacker BuildingAttacker;
    private bool CanAttack = false;
    public int Cost;
    public bool IsMainTower = false;
    // Enemies currently in range, in the order they entered. First is always the current target.
    private readonly List<Transform> NearbyEnemies = new List<Transform>();

    private void Awake()
    {
        BuildingHealth = GetComponent<Health>();
        BuildingAttacker = GetComponent<Attacker>();

        Collider AttackRange = GetComponent<Collider>();

        if (IsMainTower)
            CanAttack = true;
    }

    private void OnEnable()
    {
        BuildingAttacker.OnTargetLost += HandleTargetLost;
    }

    private void OnDisable()
    {
        BuildingAttacker.OnTargetLost -= HandleTargetLost;
    }

    private void OnTriggerEnter(Collider Other)
    {
        if (CanAttack)
        {
            Transform Enemy = ResolveRoot(Other);
            if (!Enemy.CompareTag("Enemy")) return;

            NearbyEnemies.RemoveAll(IsGone);   // drop any stale/dead entries before checking

            if (NearbyEnemies.Contains(Enemy)) return;
            NearbyEnemies.Add(Enemy);

            if (!BuildingAttacker.HasTarget)
            {
                TargetNext();
            }
        }
    }

    private void OnTriggerExit(Collider Other)
    {
        Transform Enemy = ResolveRoot(Other);
        if (!Enemy.CompareTag("Enemy")) return;

        bool WasCurrentTarget = BuildingAttacker.CurrentTargetTransform == Enemy;
        NearbyEnemies.Remove(Enemy);

        if (WasCurrentTarget)
        {
            TargetNext();
        }
    }

    private static Transform ResolveRoot(Collider Other)
    {
        return Other.attachedRigidbody != null ? Other.attachedRigidbody.transform : Other.transform;
    }

    private void HandleTargetLost()
    {
        // Our current target died - move on to whoever's next in the queue, if anyone.
        TargetNext();
    }

    private void TargetNext()
    {
        NearbyEnemies.RemoveAll(IsGone);

        if (NearbyEnemies.Count == 0)
        {
            BuildingAttacker.StopAttacking();
            return;
        }

        Transform Next = NearbyEnemies[0];
        BuildingAttacker.SetTarget(Next.GetComponent<Health>());
    }

    // True if this entry is no longer a valid attack target: destroyed, missing its
    // Health component, or already dead. Pooled enemies are deactivated rather than
    // destroyed on death, so the plain null-check alone misses them.
    private static bool IsGone(Transform Enemy)
    {
        if (Enemy == null) return true;
        Health H = Enemy.GetComponent<Health>();
        return H == null || H.IsDead;
    }

    public void Placed()        // so the buildings don't function when they are just a grid system ghost
    {
        gameObject.GetComponent<LumberMillBonus>()?.Placed();       // add the bonus to the manager
        gameObject.GetComponent<GoldMine>()?.Placed();          // start generating money
        gameObject.tag = "Building";                            // changing the tag so that the enemy can attack it
        CanAttack = true;                                   // so that the building can start attacking the enemies
    }
}