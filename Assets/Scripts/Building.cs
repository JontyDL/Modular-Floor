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

    // Enemies currently in range, in the order they entered. First is always the current target.
    private readonly List<Transform> NearbyEnemies = new List<Transform>();

    private void Awake()
    {
        BuildingHealth = GetComponent<Health>();
        BuildingAttacker = GetComponent<Attacker>();

        Collider AttackRange = GetComponent<Collider>();
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
        NearbyEnemies.RemoveAll(T => T == null);

        if (NearbyEnemies.Count == 0)
        {
            BuildingAttacker.StopAttacking();
            return;
        }

        Transform Next = NearbyEnemies[0];
        Health NextHealth = Next.GetComponent<Health>();
        if (NextHealth == null)
        {
            NearbyEnemies.RemoveAt(0);                  // the enemy is missing a health component for some reason, forget about it
            TargetNext();
            return;
        }

        BuildingAttacker.SetTarget(NextHealth);
    }

    public void Placed()        // so the buildings don't function when they are just a grid system ghost
    {
        gameObject.GetComponent<LumberMillBonus>()?.Placed();       // add the bonus to the manager
        gameObject.GetComponent<GoldMine>()?.Placed();          // start generating money
        gameObject.tag = "Building";                            // changing the tag so that the enemy can attack it
        CanAttack = true;                                   // so that the building can start attacking the enemies
    }
}