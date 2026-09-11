using UnityEngine;

public class DeadManBomb : MonoBehaviour
{
    [SerializeField] GameObject ExplosionVFX;
    public float BombDamage;
    [SerializeField] private float BombRange;
    [SerializeField] private bool SelfDestroy = false;
    public void SelfDestruct(Transform Target)
    {
        Instantiate(ExplosionVFX, transform.position, Quaternion.identity);

        if (Target != null)
        {
            if (Vector3.Distance(transform.position, Target.transform.position) < BombRange)
            {
                if (Target.TryGetComponent<Health>(out Health H))
                {
                    H.TakeDamage(BombDamage);
                }
            }
        }

        if (SelfDestroy)
        {
            Destroy(gameObject);
        }

    }
}
