using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;



public class bootstrapper : MonoBehaviour
{
    [SerializeField] private SupabaseManager _supabaseManager;
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private SceneReference mainMenuScene;
    void Start()
    {
        _ = waitUntillLoaded();
    }

    private async UniTask waitUntillLoaded()
    {
        while (_supabaseManager.CurrentStatus != Status.Ready || _saveManager.IsReady == false)
        {
            await UniTask.Yield();
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
