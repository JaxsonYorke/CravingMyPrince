using UnityEngine;
using UnityEngine.UIElements;

public class youWin : MonoBehaviour
{
    [SerializeField] private GameObject youWinScreen;
    [SerializeField] private EventHandler eventHandler;
    private void Start()
    {
        youWinScreen.SetActive(false);
    }

    private void OnEnable()
    {
        eventHandler.OnGameWin.AddListener(() => youWinScreen.SetActive(true));
    }

    private void OnDisable()
    {
        eventHandler.OnGameWin.RemoveListener(() => youWinScreen.SetActive(true));
    }
}
