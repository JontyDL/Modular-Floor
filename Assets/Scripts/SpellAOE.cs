using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SpellAOE : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float Lifetime = 30f;
    [SerializeField] private float ShrinkDuration = 5f;

    [SerializeField] private DecalProjector SpellDecal;
    private SphereCollider trigger;

    private bool placed = false;
    private float DecalWidthatStart;
    private float DecalHeightatStart;
    private float TriggerRadiusatStart;

    // Things currently affected by THIS spell.
    private readonly HashSet<Health> AffectedObjects = new HashSet<Health>();

    private void OnTriggerEnter(Collider other)
    {
        if (!placed) return;

        TryAffect(other);
    }

    private void Awake()
    {
        trigger = GetComponent<SphereCollider>();

        DecalWidthatStart = SpellDecal.size.x;
        DecalHeightatStart = SpellDecal.size.y;
        TriggerRadiusatStart = trigger.radius;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!placed) return;

        TryRemoveEffect(other);
    }

    public void Placed()
    {

        if (placed) return;

        placed = true;
        trigger.enabled = true;
        CheckAlreadyInside();       // try find all that were already inside the collider when it was placed

        StartCoroutine(SpellLifetimeRoutine());
    }

    private IEnumerator SpellLifetimeRoutine()
    {
        yield return new WaitForSeconds(Lifetime);      // stays at full size for it's lifetime

        float Elapsed = 0f;

        while (Elapsed < ShrinkDuration)
        {
            Elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(Elapsed / ShrinkDuration);

            float scale = 1f - t;

            SpellDecal.size = new Vector3(DecalWidthatStart * scale, DecalHeightatStart * scale, SpellDecal.size.z);

            trigger.radius = TriggerRadiusatStart * scale;

            yield return null;
        }

        ClearEffects();
        Destroy(gameObject);
    }

    private void TryAffect(Collider other)
    {
        if (other.gameObject.TryGetComponent<Health>(out Health health))
        {
            // Don't add the same object twice.
            if (AffectedObjects.Add(health))
            {
                health.AddDoubleDamageSource();
            }
        }
    }

    private void TryRemoveEffect(Collider other)
    {
        if (other.gameObject.TryGetComponent<Health>(out Health health))
        {
            if (AffectedObjects.Remove(health))
            {
                health.RemoveDoubleDamageSource();
            }
        }
    }

    private void CheckAlreadyInside()
    {
        Vector3 worldCenter = transform.TransformPoint(trigger.center);

        Collider[] overlappingColliders = Physics.OverlapSphere(worldCenter, trigger.radius, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider collider in overlappingColliders)
        {
            // Don't accidentally detect the spell's own collider.
            if (collider == trigger)
                continue;

            TryAffect(collider);
        }
    }

    private void ClearEffects()
    {
        foreach (Health health in AffectedObjects)
        {
            if (health != null)
            {
                health.RemoveDoubleDamageSource();
            }
        }

        AffectedObjects.Clear();
    }

    private void OnDestroy()
    {
        ClearEffects();
    }
}
