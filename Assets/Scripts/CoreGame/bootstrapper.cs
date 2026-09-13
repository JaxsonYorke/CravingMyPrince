using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class bootstrapper : MonoBehaviour
{
    [SerializeField] private SupabaseManager _supabaseManager;
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private SceneReference mainMenuScene;
    void Start()
    {
        StartCoroutine(waitUntillLoaded());
    }

    private IEnumerator waitUntillLoaded()
    {
        while (!_supabaseManager.Initialization.IsCompleted || _saveManager.IsReady == false)
        {
            yield return null;
        }
        if (mainMenuScene != null)
        {
            mainMenuScene.Load();
        }
        else
        {
            Debug.LogError("mainMenuScene reference is not assigned in bootstrapper.");
        }
    }
}
