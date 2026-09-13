using UnityEngine;

/// <summary>
/// A reusable ScriptableObject that represents a scene reference.
/// This allows you to reference scenes in the inspector without strings,
/// and centralize scene naming in one place.
/// </summary>
[CreateAssetMenu(menuName = "Scene Reference")]
public class SceneReference : ScriptableObject
{
    [SerializeField] private string sceneName;

    public string SceneName => sceneName;

    public void Load()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
