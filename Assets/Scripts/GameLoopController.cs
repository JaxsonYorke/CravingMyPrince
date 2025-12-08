using UnityEngine;

public class GameLoopControler : MonoBehaviour
{

    public GameObject GameoverScreen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameoverScreen.SetActive(false);
    }

    public void GameOver()
    {
        print("Game Over");
        GameoverScreen.SetActive(true);
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}
