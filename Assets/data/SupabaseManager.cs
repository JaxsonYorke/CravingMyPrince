using UnityEngine;
using Supabase;
using System;
using System.Text;
using System.Threading.Tasks;
using Client = Supabase.Client;

using Assets.data.databaseTypes;

public enum NotificationType
{
    Debug,
    Info,
    Warning,
    Error
}

public class SupabaseManager : MonoBehaviour
{
    public static SupabaseManager Instance { get; private set; }
    public Client _supabase;
    public Player _player;
    public Stats _playerStats;

    [HideInInspector]
    public bool IsReady { get; private set; }
    [HideInInspector]
    public bool IsOffline { get; private set; }

    [HideInInspector]
    public string _deviceId;


    private Task _initializeTask;

    private TaskCompletionSource<bool> _initializationTcs = new TaskCompletionSource<bool>();
    public Task Initialization => _initializationTcs.Task;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _deviceId = GetOrCreateDeviceId();
        Debug.Log($"DeviceId: {_deviceId}");

        _initializeTask = InitializeAsync();
        _ = StartFlowAsync();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple SupabaseManager instances detected! Destroying duplicate.");
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

#region Initialization
    private async Task StartFlowAsync()
    {
        // Check the internet connection, if there is no internet connection, set IsOffline to true and skip initialization
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("No internet connection detected. SupabaseManager will operate in offline mode.");
            IsReady = true; // Set to true to allow the game to continue, but you may want to handle this differently
            IsOffline = true;
            _initializationTcs.SetResult(false);
            return;
        }
        try
        {
            await _initializeTask;
            await EnsureRecords();

            _initializationTcs.SetResult(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Supabase init failed: {e}");
            _initializationTcs.SetException(e);
        }
    }

    private async Task EnsureRecords()
    {
        print("initializing player record...");
        await EnsureInitializedAsync();
        await EnsurePlayerRecord();
        await EnsurePlayerStatsRecord();
        await EnsurePlayerSavesRecord();

    }

    private async Task EnsurePlayerRecord(){
        Player newPlayer = null;
        var playerRes = await _supabase.From<Player>().Where(p => p.device_id == _deviceId).Get();
        _player = playerRes.Models.Count > 0 ? playerRes.Models[0] : null;

        if(playerRes.Models.Count == 0)
        {
            // create the player
            newPlayer = new Player
            {
                device_id = _deviceId,
                display_name = $"Player-{_deviceId.Substring(0, 8)}",
                created_at = DateTime.UtcNow
            };
            _player = newPlayer;
            await _supabase.From<Player>().Insert(newPlayer);
            Debug.Log($"Created new player with device id {_deviceId}");

        }
    }

    private async Task EnsurePlayerStatsRecord()
    {
        Stats newStats = null;
        var playerStatsRes = await _supabase.From<Stats>().Where(s => s.player_id == _player.id).Get();
        _playerStats = playerStatsRes.Models.Count > 0 ? playerStatsRes.Models[0] : null;

        if (playerStatsRes.Models.Count == 0)
        {
            // create the stats for the player
            newStats = new Stats
            {
                player_id = _player.id,
                deaths = 0,
                total_playtime_seconds = 0,
                total_achievements = 0
            };
            _playerStats = newStats;
            await _supabase.From<Stats>().Insert(newStats);
            Debug.Log($"Created stats for existing player with device id {_deviceId}");
        }
    }

    private async Task EnsurePlayerSavesRecord()
    {
        var playerSavesRes = await _supabase.Storage.From("player_saves").List(_player.id + "/");

        foreach(var item in playerSavesRes)
        {
            Debug.Log($"Found save file: {item.Name}");
        }

        if (playerSavesRes.Count == 0)
        {
            // create empty saves for the player
            var emptySaveData = Encoding.UTF8.GetBytes(Save.GetEmptySaveJson());
            await _supabase.Storage.From("player_saves").Upload(emptySaveData, _player.id + "/save_1.json");
            await _supabase.Storage.From("player_saves").Upload(emptySaveData, _player.id + "/save_2.json");
            await _supabase.Storage.From("player_saves").Upload(emptySaveData, _player.id + "/save_3.json");
            Debug.Log($"Created empty saves for existing player with device id {_deviceId}");
        }
        if(playerSavesRes.Count == 1 || playerSavesRes.Count == 2){
            bool hasSave1 = false;
            bool hasSave2 = false;
            bool hasSave3 = false;

            foreach(var item in playerSavesRes)
            {
                Debug.Log($"Found save file: {item.Name}");
                if(item.Name == "save_1.json") hasSave1 = true;
                if(item.Name == "save_2.json") hasSave2 = true;
                if(item.Name == "save_3.json") hasSave3 = true;
            }

            if(!hasSave1)
            {
                var emptySaveData = Encoding.UTF8.GetBytes(Save.GetEmptySaveJson());
                await _supabase.Storage.From("player_saves").Upload(emptySaveData, _player.id + "/save_1.json");
                Debug.Log($"Created missing save1 for existing player with device id {_deviceId}");
            }
            if(!hasSave2)
            {
                var emptySaveData = Encoding.UTF8.GetBytes(Save.GetEmptySaveJson());
                await _supabase.Storage.From("player_saves").Upload(emptySaveData, _player.id + "/save_2.json");
                Debug.Log($"Created missing save2 for existing player with device id {_deviceId}");
            }
            if(!hasSave3)
            {
                var emptySaveData = Encoding.UTF8.GetBytes(Save.GetEmptySaveJson());
                await _supabase.Storage.From("player_saves").Upload(emptySaveData, _player.id + "/save_3.json");
                Debug.Log($"Created missing save3 for existing player with device id {_deviceId}");
            }
            
        }
    }


    private async Task InitializeAsync()
    {
        // Create a Supabase objects object.
        SupabaseOptions options = new()
        {
            // No auth flow in this client; keep refresh disabled.
            AutoRefreshToken = false
        };

        // Create the client object. Note that the project URL and the anon key are
        // available in the Supabase dashboard. In addition, note that the public anon
        // key is not a security risk - in JavaScript projects, this key is visible
        // in the browser viewing source!
        _supabase = new Client(SupabaseSettings.SupabaseURL, SupabaseSettings.SupabaseAnonKey, options);

        await _supabase.InitializeAsync();

        IsReady = true;
    }

    private string GetOrCreateDeviceId()
    {
        const string key = "device_id";
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetString(key);

        string id = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(key, id);
        PlayerPrefs.Save();
        return id;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initializeTask == null)
            _initializeTask = InitializeAsync();

        await _initializeTask;
    }




#endregion





    // This method will update the deaths based on a paramater
    public async Task UpdatePlayerDeaths(int deaths)
    {
        await EnsureInitializedAsync();

        var playerRes = await _supabase.From<Stats>().Where(s => s.player_id == _player.id).Get();

        if(playerRes.Models.Count == 0)
        {
            Debug.LogError($"No player found with device id {_deviceId}");
            return;
        }

        var player = playerRes.Models[0];
        player.deaths = deaths;

        await _supabase.From<Stats>().Update(player);
    }

    public async Task IncrementDeaths()
    {
        print("Incrementing deaths in Supabase...");
        await EnsureInitializedAsync();

        try{
            await _supabase.Rpc("increment_deaths", new { p_player_id = _player.id });
        }
        catch (Exception e){
            Debug.LogError($"Failed to increment deaths: {e}");
        }
    }





    // Gathering saves
    public async Task<Save> GetSaveFromSupabase(int slot)
    {
        await EnsureInitializedAsync();

        try
        {
            var path = $"{_player.id}/save_{slot}.json";


            var saveData = await _supabase.Storage
                .From("player_saves")
                .Download(path, null);

            var saveJson = Encoding.UTF8.GetString(saveData);
            return new Save(saveJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to download save: {e}");
            return null;
        }
    }

    public async Task<bool> SaveSaveToSupabase(int slot, Save save)
    {
        if (IsOffline)
        {
            Debug.LogWarning("Cannot save to cloud while in offline mode.");
            return false;
        }

        await EnsureInitializedAsync();

        try
        {
            var path = $"{_player.id}/save_{slot}.json";
            var saveData = Encoding.UTF8.GetBytes(save.ToJsonString());

            await _supabase.Storage.From("player_saves").Upload(saveData, path, new Supabase.Storage.FileOptions { Upsert = true });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to upload save: {e}");
            return false;
        }
    }
}



/// <summary>
/// Holds Supabase configuration settings
/// </summary>
public static class SupabaseSettings
{
    public static string SupabaseURL = "https://vilsiejotgkmzoixncuz.supabase.co";
    public static string SupabaseAnonKey = "sb_publishable_Z5wOm1DbSmVdg8F3KtPngA_NYPKQ_aM";
    public static string SaveUploadFunction = "save-upload";
    public static string SaveDownloadFunction = "save-download";
}

public class CloudSaveRequest
{
    public string device_id;
    public string save_slot;
    public string data_json;
}
