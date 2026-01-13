using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoopControler : MonoBehaviour
{
    public EventHandler EventHandler;

    public GameObject GameoverScreen;

    public GameObject Princess;
    public GameObject Monster;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameoverScreen.SetActive(false);

        // subscribe to death events
        EventHandler.OnPrincessDeath.AddListener(GameOver);
        EventHandler.OnMonsterDeath.AddListener(GameOver);

        EventHandler.OnGameRestart.AddListener(reloadCurrentScene);
    }

    public void GameOver()
    {
        print("Game Over");
        GameoverScreen.SetActive(true);
    }

    public void reloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void restartGame()
    {
        EventHandler.RestartGame();
    }

    public void tryCrushPrincess()
    {
        //This would contain logic relating to the powers or the gamestate as to if the princess should be crushed
        EventHandler.TriggerPrincessDeath();
    }

    public void tryKillPrincess()
    {
        EventHandler.TriggerPrincessDeath();
    }

    public void tryKillMonster()
    {
        EventHandler.TriggerMonsterDeath();
    }
}
