using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Shared health component. Attach to both AI and Buildings. Anything with
/// this component can be damaged and, on reaching zero, is destroyed. Also
/// flashes any renderers on this object (and its children) when damaged.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] private float StartingMaxHealth = 100f;

    [Header("Damage Flash")]
    [SerializeField] private Color FlashColour = Color.red;
    [SerializeField] private float FlashDuration = 0.15f;   // total time for the whole red-white-red-back sequence

    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    public event Action OnDeath;

    private Renderer[] CachedRenderers;         // saving all the renderers and materials from an object so that we can flash when we take damage
    private Material[][] CachedMaterials;
    private Color[][] OriginalColours;
    private Coroutine FlashRoutine;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/HDRP Lit
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in Standard/Legacy

    private HealthBar HB;
    private void Awake()
    {
        MaxHealth = StartingMaxHealth;
        CurrentHealth = MaxHealth;
        CacheRenderers();

        HB = GetComponentInChildren<HealthBar>();
    }

    public void SetMaxHealth(float NewMax, bool RefillToFull = true)        // use this to refill building health/upgrade it
    {
        MaxHealth = Mathf.Max(1f, NewMax);

        if (RefillToFull)
        {
            CurrentHealth = MaxHealth;
            if (HB != null) HB.UpdateHealthBar(MaxHealth, CurrentHealth);
        }
    }

    public void TakeDamage(float Amount)
    {
        if (IsDead || Amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - Amount);

        if (CurrentHealth <= 0f)
        {
            OnDeath?.Invoke();
            Destroy(gameObject);
            // play some death sfx here
        }
        else
        {
            DamageColours();
            if (HB != null) HB.UpdateHealthBar(MaxHealth, CurrentHealth);
        }
    }

    private void CacheRenderers()
    {
        CachedRenderers = GetComponentsInChildren<Renderer>();
        CachedMaterials = new Material[CachedRenderers.Length][];
        OriginalColours = new Color[CachedRenderers.Length][];

        for (int i = 0; i < CachedRenderers.Length; i++)
        {
            Material[] Mats = CachedRenderers[i].materials;
            CachedMaterials[i] = Mats;
            OriginalColours[i] = new Color[Mats.Length];

            for (int j = 0; j < Mats.Length; j++)
            {
                OriginalColours[i][j] = GetMaterialColour(Mats[j]);
            }
        }
    }

    private void DamageColours()
    {
        if (!isActiveAndEnabled) return; // can't run a coroutine on a disabled object

        if (FlashRoutine != null) StopCoroutine(FlashRoutine);
        FlashRoutine = StartCoroutine(FlashDamageColour());
    }

    private IEnumerator FlashDamageColour()                     // quick red to white to red and back to original colours flash
    {
        Color[] Sequence = { FlashColour, Color.white, FlashColour };
        float StepDuration = Mathf.Max(0.01f, FlashDuration / Sequence.Length);

        foreach (Color Step in Sequence)
        {
            ApplyColourToAll(Step);
            yield return new WaitForSeconds(StepDuration);
        }

        RestoreOriginalColours();
        FlashRoutine = null;
    }

    private void ApplyColourToAll(Color NewColour)
    {
        for (int i = 0; i < CachedMaterials.Length; i++)
        {
            Material[] Mats = CachedMaterials[i];
            if (Mats == null) continue;

            for (int j = 0; j < Mats.Length; j++)
            {
                SetMaterialColour(Mats[j], NewColour);
            }
        }
    }

    private void RestoreOriginalColours()
    {
        for (int i = 0; i < CachedMaterials.Length; i++)
        {
            Material[] Mats = CachedMaterials[i];
            if (Mats == null) continue;

            for (int j = 0; j < Mats.Length; j++)
            {
                SetMaterialColour(Mats[j], OriginalColours[i][j]);
            }
        }
    }

    private static Color GetMaterialColour(Material Mat)
    {
        if (Mat.HasProperty(BaseColorId)) return Mat.GetColor(BaseColorId);
        if (Mat.HasProperty(ColorId)) return Mat.GetColor(ColorId);
        return Color.white;
    }

    private static void SetMaterialColour(Material Mat, Color NewColour)
    {
        if (Mat.HasProperty(BaseColorId)) Mat.SetColor(BaseColorId, NewColour);
        else if (Mat.HasProperty(ColorId)) Mat.SetColor(ColorId, NewColour);
    }

    private void OnDestroy()
    {
        // Clean up the material instances we created so they don't leak.
        if (CachedMaterials == null) return;
        foreach (Material[] Mats in CachedMaterials)
        {
            if (Mats == null) continue;
            foreach (Material Mat in Mats)
            {
                if (Mat != null) Destroy(Mat);
            }
        }
    }
}