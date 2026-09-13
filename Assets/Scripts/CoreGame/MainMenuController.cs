
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenuController : MonoBehaviour
{

    [SerializeField] private GameObject savesMenu;
    [SerializeField] private GameObject CloudSavesConfirmation;
    [SerializeField] private TextMeshProUGUI PromptText;
    [SerializeField] private int currentConfirmingSaveSlot;
    [SerializeField] private SceneReference mapScene;


    private bool waitingForCloudSavesConfirmation = false;
    public void OpenSavesMenu()
    {
        savesMenu.SetActive(true);
        
        // if there are any differences in the saves, ask the player if they want to treat local or cloud as source of truth
        StartCoroutine(CheckForCloudSaveConflicts());
    }

    private IEnumerator CheckForCloudSaveConflicts()
    {
        string baseString = "Your local save {0} is different from your cloud save. Do you want to overwrite your cloud save with the local save?";
        if (SaveManager.Instance.LocalSave1IsModified)
        {
            currentConfirmingSaveSlot = 1;
            PromptText.text = string.Format(baseString, 1);
            CloudSavesConfirmation.SetActive(true);
            waitingForCloudSavesConfirmation = true;
            yield return new WaitUntil(() => waitingForCloudSavesConfirmation == false);
        }
        if (SaveManager.Instance.LocalSave2IsModified)
        {
            currentConfirmingSaveSlot = 2;
            PromptText.text = string.Format(baseString, 2);
            CloudSavesConfirmation.SetActive(true);
            waitingForCloudSavesConfirmation = true;
            yield return new WaitUntil(() => waitingForCloudSavesConfirmation == false);
        }
        if (SaveManager.Instance.LocalSave3IsModified)
        {
            currentConfirmingSaveSlot = 3;
            PromptText.text = string.Format(baseString, 3);
            CloudSavesConfirmation.SetActive(true);
            waitingForCloudSavesConfirmation = true;
            yield return new WaitUntil(() => waitingForCloudSavesConfirmation == false);
        }  
        print("Done checking for cloud save conflicts");      
    }

    public void CloseSavesMenu()
    {
        savesMenu.SetActive(false);
        if(waitingForCloudSavesConfirmation)
        {
            CloudSavesConfirmation.SetActive(false);
            waitingForCloudSavesConfirmation = false;
        }
    }

    public void OverwriteCloudSaveWithLocal()
    {
        SaveManager.Instance.OverwriteCloudSaveWithLocal(currentConfirmingSaveSlot);
        CloudSavesConfirmation.SetActive(false);
        waitingForCloudSavesConfirmation = false;
    }

    public void RevertLocalSaveToCloud()
    {
        Save cloudSave = currentConfirmingSaveSlot switch
        {
            1 => SaveManager.Instance.cloudSave1,
            2 => SaveManager.Instance.cloudSave2,
            3 => SaveManager.Instance.cloudSave3,
            _ => throw new System.ArgumentException("Invalid save slot: " + currentConfirmingSaveSlot)
        };
        _ = SaveManager.Instance.setSave(currentConfirmingSaveSlot, cloudSave);
        CloudSavesConfirmation.SetActive(false);
        waitingForCloudSavesConfirmation = false;
    }


    public void StartGameFromSave(int saveSlot)
    {
        SaveManager.SelectSaveSlot(saveSlot);
        if (SaveManager.CurrentSave == null)
        {
            Debug.LogError("Selected save is null for slot " + saveSlot);
            return;
        }
        
        if (mapScene != null)
        {
            mapScene.Load();
        }
        else
        {
            Debug.LogError("mapScene reference is not assigned in MainMenuController.");
        }
    }
}
