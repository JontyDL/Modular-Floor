using UnityEngine;

public class Billboard : MonoBehaviour
{
    public bool lockYRotation = true;

    private Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        if (lockYRotation)
        {
            Vector3 dir = transform.position - cam.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            transform.rotation = cam.rotation;
        }
    }
}