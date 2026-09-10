using UnityEngine;

public class DeadManBomb : MonoBehaviour
{
    [SerializeField] GameObject ExplosionVFX;
    public float BombDamage;
    [SerializeField] private float BombRange;

    public void SelfDestruct(Transform TargetBuilding)
    {
        Instantiate(ExplosionVFX, transform.position, Quaternion.identity);

        if (Vector3.Distance(transform.position, TargetBuilding.transform.position) < BombRange)
        if (TargetBuilding.TryGetComponent<Health>(out Health H))
        {
            H.TakeDamage(BombDamage);
        }
    }
}
