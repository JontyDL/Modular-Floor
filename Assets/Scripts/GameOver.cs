using UnityEngine;

public class GameOver : MonoBehaviour
{
    private void OnDestroy()
    {
        GameStateManager.Instance.GameOver();
    }
}
