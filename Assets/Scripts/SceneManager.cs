using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance {  get; private set; }

    [SerializeField] private GameObject PauseMenu;
    [SerializeField] private GameObject GameOverMenu;
    [SerializeField] private TextMeshProUGUI GameStatsText;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(gameObject);
        }
    }

    public void ToggleGamePause()
    {
        if (Time.timeScale == 1.0f)
        {
            Time.timeScale = 0f;
            PauseMenu.SetActive(true);
        } else
        {
            Time.timeScale = 1f;
            PauseMenu.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void GameOver()
    {
        GameStatsText.text = BuildGameStats(StatCollector.Instance.GetFinalStats());

        // sfx and stats would be updated here
        if (GameOverMenu == null) return;

        GameOverMenu.SetActive(true);
    }

    private string BuildGameStats(GameStats stats)
    {
        string s;

        s = "Waves Survived: " + stats.WavesSurvived.ToString() + "\n";
        s += "Enemies Killed: " + stats.EnemiesKilled.ToString() + "\n";
        s += "Gold Spent: " + stats.AmountSpent.ToString();

        return s;
    }
}
