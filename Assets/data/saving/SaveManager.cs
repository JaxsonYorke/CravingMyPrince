using System.IO;
using System.Threading.Tasks;

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
    public bool OfflineMode => _supabaseManager.IsOffline;


#region Initilization
    async void Awake()
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

        // Load all the local saves first
        string basePath = Application.persistentDataPath + "/saves/";
        save1 = new Save(File.Exists(basePath + "save1.json") ? File.ReadAllText(basePath + "save1.json") : Save.GetEmptySaveJson());
        save2 = new Save(File.Exists(basePath + "save2.json") ? File.ReadAllText(basePath + "save2.json") : Save.GetEmptySaveJson());
        save3 = new Save(File.Exists(basePath + "save3.json") ? File.ReadAllText(basePath + "save3.json") : Save.GetEmptySaveJson());

        await SupabaseManager.Instance.Initialization;

        if(_supabaseManager.IsOffline)
        {
            Debug.LogWarning("SupabaseManager is offline. SaveManager will operate in offline mode.");
        } else {
            // Load saves from the cloud
            cloudSave1 = await _supabaseManager.GetSaveFromSupabase(1);
            cloudSave2 = await _supabaseManager.GetSaveFromSupabase(2);
            cloudSave3 = await _supabaseManager.GetSaveFromSupabase(3);

            // Check the differences between local and cloud saves
            LocalSave1IsModified = !save1.Equals(cloudSave1);
            LocalSave2IsModified = !save2.Equals(cloudSave2);
            LocalSave3IsModified = !save3.Equals(cloudSave3);

        }
        IsReady = true; // Set to true to allow the game to continue

    }

    public static void SelectSaveSlot(int saveSlot)
    {
        SelectedSaveSlot = saveSlot;
        Instance._currentSave = Instance.GetSave(saveSlot)?.Clone();
    }



#endregion

    public async Task setSave(int saveSlot, Save cloudSave){
        await SaveSaveToDisk(saveSlot, cloudSave);
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

    public Task<bool> SaveSaveToDisk(int slot, Save save)
    {
        string basePath = Application.persistentDataPath + "/saves/";
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        string json = save.ToJsonString();
        File.WriteAllText(basePath + $"save{slot}.json", json);
        return Task.FromResult(true);
    }

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
    

    public async Task<bool> SaveSaveToCloud(int slot, Save save)
    {
        if (_supabaseManager.IsOffline)
        {
            Debug.LogWarning("Cannot save to cloud while in offline mode.");
            return false;
        }

        return await _supabaseManager.SaveSaveToSupabase(slot, save);
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
        
        _ = SaveSaveToDisk(SelectedSaveSlot, committedSave);

        if(!_supabaseManager.IsOffline){
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
