using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private SceneReference mainMenuScene;
    [SerializeField] private GameObject pauseMenuUI;

    [SerializeField] private GameObject ExitConfirmation;
    [SerializeField] private GameObject SaveConfirmation;
    [SerializeField] private GameObject SavedMessage;

    private bool isPaused = false;
    public InputActionAsset inputActions;
    public InputAction _pause;

    void OnEnable()
    {
        inputActions.FindActionMap("Player").Enable();
        _pause = inputActions["Player/Pause"];
        _pause.performed += TogglePause;        
    }

    void OnDisable()
    {
        if(_pause != null)
        {
            _pause.performed -= TogglePause;
        }

        if(inputActions != null)
        {
            inputActions.FindActionMap("Player").Disable();
        }
    }

    public void TogglePause(InputAction.CallbackContext context)
    {
        if (isPaused) { Resume(); }
        else { Pause(); }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void BackToGame()
    {
        Resume();
    }

    public void OpenQuitToMainMenuConfirmation()
    {
        // Implementation for quitting to main menu
        ExitConfirmation.SetActive(true);
    }

    public void QuitToMainMenu()
    {
        // Implementation for quitting to main menu
        Time.timeScale = 1f; // Ensure time scale is reset
        if (mainMenuScene != null)
        {
            mainMenuScene.Load();
        }
        else
        {
            Debug.LogError("mainMenuScene reference is not assigned in PauseMenuController.");
        }
    }

    public void QuitAndSaveToMainMenu()
    {
        // Implementation for quitting and saving
        SaveManager.Instance.SaveCurrentProgress();
        Time.timeScale = 1f; // Ensure time scale is reset
        if (mainMenuScene != null)
        {
            mainMenuScene.Load();
        }
        else
        {
            Debug.LogError("mainMenuScene reference is not assigned in PauseMenuController.");
        }
    }

    public void CancelQuitToMainMenu()
    {
        ExitConfirmation.SetActive(false);
    }

    public void OpenSaveGameConfirmation()
    {
        SaveConfirmation.SetActive(true);
    }

    public void ConfirmSaveGame()
    {
        // TODO: rn this doesnt check if it worked or not, so that sohuld change
        SaveManager.Instance.SaveCurrentProgress();
        SaveConfirmation.SetActive(false);
        SavedMessage.SetActive(true);
        StartCoroutine(hideSavedMessageAfterDelay(2f));
    }

    public void CancelSaveGame()
    {
        SaveConfirmation.SetActive(false);
    }

    public IEnumerator hideSavedMessageAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SavedMessage.SetActive(false);
    }
}
