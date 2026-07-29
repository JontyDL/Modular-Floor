using UnityEngine;

public class DestructableObject : MonoBehaviour, IInteractable
{

    [SerializeField] private GameObject destroyEffectPrefab;            // when I make destruction vfx/particles
    [SerializeField] private AudioClip destroySound;                    // when I add sound
    // When I add currancy, I'd also add a cost to destroying this object

    public void OnInteract(InteractionMode mode)
    {
        if (mode != InteractionMode.Destroy) return;
        HandleDestruction();
    }

    private void HandleDestruction()
    {
        if (destroyEffectPrefab != null)
            Instantiate(destroyEffectPrefab, transform.position, transform.rotation);

        if (destroySound != null)
            AudioSource.PlayClipAtPoint(destroySound, transform.position);

        Destroy(gameObject);
    }
}
