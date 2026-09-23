using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static int SelectedSaveSlot { get; private set; }
    public static Save CurrentSave => Instance._currentSave;

    private SupabaseManager _supabaseManager;
    private Save _currentSave;

    public Save save1;
    public Save save2;
    public Save save3;

    public Save cloudSave1;
    public Save cloudSave2;
    public Save cloudSave3;

    public bool LocalSave1IsModified { get; private set; }
    public bool LocalSave2IsModified { get; private set; }
    public bool LocalSave3IsModified { get; private set; }

    public bool IsReady { get; private set; }
    public bool OfflineMode => SupabaseManager.Instance == null || SupabaseManager.Instance.IsOffline;
    

    private UniTaskCompletionSource _saveInitCompletion = new UniTaskCompletionSource();
    
    /// <summary>
    /// Exposes a gatekeeper task for your UI layer to ensure local/cloud profiles are fully queried.
    /// </summary>
    public UniTask EnsureReady() => _saveInitCompletion.Task;


#region Initilization
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple instances of SaveManager detected! Destroying duplicate.");
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        _supabaseManager = SupabaseManager.Instance;
        DontDestroyOnLoad(this.gameObject);

    }

    void Start()
    {
        InitalizeAsync().Forget();
    }


    private async UniTaskVoid InitalizeAsync()
    {
        // Load all the local saves first
        string basePath = Application.persistentDataPath + "/saves/";
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }
        save1 = new Save(File.Exists(basePath + "save1.json") ? File.ReadAllText(basePath + "save1.json") : Save.GetEmptySaveJson());
        save2 = new Save(File.Exists(basePath + "save2.json") ? File.ReadAllText(basePath + "save2.json") : Save.GetEmptySaveJson());
        save3 = new Save(File.Exists(basePath + "save3.json") ? File.ReadAllText(basePath + "save3.json") : Save.GetEmptySaveJson());

        await SupabaseManager.Instance.EnsureReady();

        if(OfflineMode)
        {
            Debug.LogWarning("SupabaseManager is offline. SaveManager will operate in offline mode.");
        } else {
            try
            {
                var cloudTasks = await UniTask.WhenAll(
                    _supabaseManager.GetSaveFromSupabase(1),
                    _supabaseManager.GetSaveFromSupabase(2),
                    _supabaseManager.GetSaveFromSupabase(3)
                );
                cloudSave1 = cloudTasks.Item1;
                cloudSave2 = cloudTasks.Item2;
                cloudSave3 = cloudTasks.Item3;

                // Check the differences between local and cloud saves
                LocalSave1IsModified = !save1.Equals(cloudSave1);
                LocalSave2IsModified = !save2.Equals(cloudSave2);
                LocalSave3IsModified = !save3.Equals(cloudSave3);
            } catch (System.Exception ex)
            {
                Debug.LogError($"Error fetching cloud saves: {ex.Message}");
            }
        }
        IsReady = true; // Set to true to allow the game to continue
        _saveInitCompletion.TrySetResult();
    }

    public static void SelectSaveSlot(int saveSlot)
    {
        SelectedSaveSlot = saveSlot;
        Instance._currentSave = Instance.GetSave(saveSlot)?.Clone();
    }



#endregion

    public Save GetSave(int slot)
    {
        return slot switch
        {
            1 => save1,
            2 => save2,
            3 => save3,
            _ => throw new System.ArgumentException("Invalid save slot: " + slot)
        };
    }


    public async UniTask setSave(int saveSlot, Save cloudSave){
        await SaveSaveToDiskAsync(saveSlot, cloudSave);
        switch (saveSlot)
        {
            case 1:
                save1 = cloudSave;
                LocalSave1IsModified = false;
                break;
            case 2:
                save2 = cloudSave;
                LocalSave2IsModified = false;
                break;
            case 3:
                save3 = cloudSave;
                LocalSave3IsModified = false;
                break;
            default:
                Debug.LogError("Invalid save slot: " + saveSlot);
                break;
        }

        if (SelectedSaveSlot == saveSlot)
        {
            _currentSave = cloudSave?.Clone();
        }
    }


    public async UniTask<bool> SaveSaveToCloud(int slot, Save save)
    {
        if (OfflineMode)
        {
            Debug.LogWarning("Cannot save to cloud while in offline mode.");
            return false;
        }

        return await SupabaseManager.Instance.SaveSaveToSupabase(slot, save);
    }


    private async UniTask<Save> LoadLocalSaveFromDiskAsync(string directoryPath, string filename)
    {
        string fullPath = Path.Combine(directoryPath, filename);
        if (!File.Exists(fullPath))
        {
            return new Save(Save.GetEmptySaveJson());
        }

        try
        {
            // Offload the heavy file stream generation away from Unity's main frame loop
            byte[] fileBytes;
            using (FileStream sourceStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                fileBytes = new byte[sourceStream.Length];
                await sourceStream.ReadAsync(fileBytes, 0, (int)sourceStream.Length);
            }
            
            string json = Encoding.UTF8.GetString(fileBytes);
            return new Save(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading local profile stream target: {filename}. Fallback implemented. Exception: {e}");
            return new Save(Save.GetEmptySaveJson());
        }
    }

    private async UniTask SaveSaveToDiskAsync(int slot, Save save)
    {
        string basePath = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        string fullPath = Path.Combine(basePath, $"save{slot}.json");
        byte[] encodedData = Encoding.UTF8.GetBytes(save.ToJsonString());

        try
        {
            using (FileStream sourceStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedData, 0, encodedData.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed pushing profile snapshot down to data partition: {e}");
        }
    }

    


    public void OverwriteCloudSaveWithLocal(int saveSlot) {
        Save localSave = GetSave(saveSlot);
        _ = SaveSaveToCloud(saveSlot, localSave);
        switch (saveSlot)
        {
            case 1:
                cloudSave1 = localSave;
                LocalSave1IsModified = false;
                break;
            case 2:
                cloudSave2 = localSave;
                LocalSave2IsModified = false;
                break;
            case 3:
                cloudSave3 = localSave;
                LocalSave3IsModified = false;
                break;
            default:
                Debug.LogError("Invalid save slot: " + saveSlot);
                break;
        }

        if (SelectedSaveSlot == saveSlot)
        {
            _currentSave = localSave?.Clone();
        }
    }


    public void SaveCurrentProgress(){
        Save saveToUse = _currentSave;

        if (saveToUse == null)
        {
            Debug.LogError("Cannot save current progress because the working save is null.");
            return;
        }

        Save committedSave = saveToUse.Clone();

        switch (SelectedSaveSlot)
        {
            case 1:
                save1 = committedSave;
                break;
            case 2:
                save2 = committedSave;
                break;
            case 3:
                save3 = committedSave;
                break;
            default:
                Debug.LogError("Invalid selected save slot: " + SelectedSaveSlot);
                return;
        }
        
        _ = SaveSaveToDiskAsync(SelectedSaveSlot, committedSave);

        if(!OfflineMode){
            _ = SaveSaveToCloud(SelectedSaveSlot, committedSave);
        }
    }


}



//MARK: Save Class
[System.Serializable]
public class Save : System.IEquatable<Save>
{
    // Core save fields (explicitly compare these)
    [SerializeField] public string locationId; // The ID of the current location on the map
    [SerializeField] public int coins; // The number of coins the player has
    // Add other relevant save data fields here and include them in Equals/GetHashCode

    public Save(string json)
    {
        // Deserialize the JSON string to populate the save data fields
        JsonUtility.FromJsonOverwrite(json, this);
    }

    public Save Clone() => new Save(ToJsonString());

    public string ToJsonString()
    {
        // Serialize the save data fields to a JSON string
        return JsonUtility.ToJson(this);
    }

    public static Save GetEmptySave()
    {
        return new Save("{}");
    }

    public static string GetEmptySaveJson()
    {
        return GetEmptySave().ToJsonString();
    }

    // Field-based equality. Prefer explicit checks to avoid JSON fragility.
    public bool Equals(Save other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        return locationId == other.locationId
            && coins == other.coins;
    }

    public override bool Equals(object obj) => Equals(obj as Save);

    // Provide a matching hash code implementation when overriding Equals
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + locationId.GetHashCode();
            hash = hash * 31 + coins.GetHashCode();
            return hash;
        }
    }
}
