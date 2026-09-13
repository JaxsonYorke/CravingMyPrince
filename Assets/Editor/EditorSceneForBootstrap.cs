using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class EditorSceneForBootstrap
{
    static EditorSceneForBootstrap()
    {
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Bootstrap.unity");
    }

}
