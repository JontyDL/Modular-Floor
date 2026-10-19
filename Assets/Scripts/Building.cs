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
    [SerializeField] private GameObject RangeRing;
    // Enemies currently in range, in the order they entered. First is always the current target.
    private readonly List<Transform> NearbyEnemies = new List<Transform>();

    // ---- Range (used by the upgrade system) ----
    private SphereCollider RangeCollider;
    public bool HasRange => RangeCollider != null;

    private void Awake()
    {
        BuildingHealth = GetComponent<Health>();
        BuildingAttacker = GetComponent<Attacker>();

        // The range is the trigger sphere. Buildings without one (gold mine, lumber mill) simply have no upgradeable range.
        foreach (SphereCollider Sphere in GetComponents<SphereCollider>())
        {
            if (Sphere.isTrigger)
            {
                RangeCollider = Sphere;
                break;
            }
        }

        if (IsMainTower)
            CanAttack = true;
    }

    public float Range
    {
        get => RangeCollider != null ? RangeCollider.radius : 0f;
        set
        {
            if (RangeCollider == null) return;

            float OldRadius = RangeCollider.radius;
            float NewRadius = Mathf.Max(0.1f, value);
            if (Mathf.Approximately(OldRadius, NewRadius)) return;

            RangeCollider.radius = NewRadius;

            // Scale the ring by the same ratio so it keeps matching the collider
            if (RangeRing != null && OldRadius > 0f)
                RangeRing.transform.localScale *= NewRadius / OldRadius;
        }
    }

    public void ShowRangeRing(bool Show)
    {
        if (RangeRing != null)
            RangeRing.SetActive(Show);
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
        if (Other.isTrigger) return;

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
        gameObject.GetComponent<SpellAOE>()?.Placed();          // start affecting enemies and buildings alike
        
        if (!gameObject.TryGetComponent<DeadManBomb>(out DeadManBomb bomb) && !gameObject.TryGetComponent<SpellAOE>(out SpellAOE SAOE))     // we don't want to set the tag if it's a bomb or Spell, as enemies shouldn't attack them
            gameObject.tag = "Building";                            // changing the tag so that the enemy can attack it
        
        CanAttack = true;                                   // so that the building can start attacking the enemies
        if (RangeRing != null)
            RangeRing.SetActive(false);
    }
}