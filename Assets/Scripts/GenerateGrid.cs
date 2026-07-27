using UnityEditor;
using UnityEngine;

public class GenerateGrid : MonoBehaviour
{
    public GameObject blockGameObject;
    [SerializeField] private int worldSizeX = 10;
    [SerializeField] private int worldSizeZ = 10;
    [SerializeField] private int noiseHeight = 4;
    [SerializeField] private int gridOffset = 2;
    [SerializeField] private float detailScale = 8f;

    private bool rebuildQueued = false;

    void Start()
    {
        GenerateWorld();
    }

    private void OnValidate()               //use this to change in the editor, without having to keep reloading the game, it is called 
    {
        if (rebuildQueued) return;
        rebuildQueued = true;

        #if UNITY_EDITOR
                EditorApplication.delayCall += () =>
                {
                    rebuildQueued = false;
                    if (this == null) return;
                    GenerateWorld();
                };
        #else
                rebuildQueued = false;
        #endif
    }

    private void GenerateWorld() 
    {
        ClearChildren();

        for (int x = 0; x < worldSizeX; x++)
        {
            for (int z = 0; z < worldSizeZ; z++)
            {
                Vector3 pos = new Vector3(
                    x * gridOffset,
                    generateNoise(x, z, detailScale) * noiseHeight,
                    z * gridOffset
                );

                GameObject block = Instantiate(blockGameObject, pos, Quaternion.identity);
                block.transform.SetParent(transform);
            }
        }
    }

    private void ClearChildren()            // remove the previous sets of blocks
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private float generateNoise(int x, int z, float scale)
    {
        float xNoise = (x + transform.position.x) / scale;
        float zNoise = (z + transform.position.z) / scale;
        return Mathf.PerlinNoise(xNoise, zNoise);
    }
}