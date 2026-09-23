using UnityEngine;
using Supabase;
using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Client = Supabase.Client;

using Assets.data.databaseTypes;

public enum NotificationType
{
    Debug,
    Info,
    Warning,
    Error
}

public enum Status
{
    Initializing,
    Ready,
    Offline
}

public class SupabaseManager : MonoBehaviour
{
    public static SupabaseManager Instance { get; private set; }


    private Client _supabase;
    private Player _player;
    private Stats _playerStats;

    private Status _status;
    public Status CurrentStatus => _status;

    public bool IsOffline => _status == Status.Offline;
    public bool IsReady => _status == Status.Ready;


    [HideInInspector]
    public string _deviceId;


    private UniTask _initializeTask;
    // This completion source acts as a gatekeeper that other scripts can await
    private UniTaskCompletionSource _initCompletionSource = new UniTaskCompletionSource();
    
    /// <summary>
    /// Expose this task so any script in your game can do: await SupabaseManager.Instance.EnsureReady();
    /// </summary>
    public UniTask EnsureReady() => _initCompletionSource.Task;



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

        _deviceId = GetOrCreateDeviceId();
        Debug.Log($"DeviceId: {_deviceId}");

        Initialize().Forget(); // Start the initialization process without awaiting it
    }

#region Initialization
    private async UniTaskVoid Initialize()
    {
        _status = Status.Initializing;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("No internet connection detected. SupabaseManager will operate in offline mode.");
            _status = Status.Offline;
            _initCompletionSource.TrySetResult(); // Signal that initialization is complete
            return;
        }

        await createSupabaseClient();

        if (_status == Status.Offline)
        {
            Debug.LogWarning("SupabaseManager is offline. Skipping record initialization.");
            _initCompletionSource.TrySetResult(); // Signal that initialization is complete
            return;
        }


        // Supabase is online and initialized, now ensure the player and stats records exist
        try
        {
            await EnsureRecords();
        }
        catch (Exception e)
        {
            Debug.LogError($"Supabase init failed: {e}");
            _status = Status.Offline;
            return;
        }

        _status = Status.Ready; // Set to true to allow the game to continue
        _initCompletionSource.TrySetResult(); // Signal that initialization is complete
        Debug.Log("SupabaseManager is ready.");
    }

    private async UniTask createSupabaseClient()
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
        try
        {
            _supabase = new Client(SupabaseSettings.SupabaseURL, SupabaseSettings.SupabaseAnonKey, options);
            await _supabase.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to create Supabase client: {e}");
            _status = Status.Offline;
            return;
        }
    }



    private async UniTask EnsureRecords()
    {
        print("initializing player record...");
        try
        {
            await EnsurePlayerRecord();

            await UniTask.WhenAll(
                EnsurePlayerStatsRecord(),
                EnsurePlayerSavesRecord()
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error ensuring records: {e}");
            _status = Status.Offline;
        }
    }

    private async UniTask EnsurePlayerRecord()
    {
        var playerRes = await _supabase.From<Player>().Where(p => p.device_id == _deviceId).Get();
        
        if (playerRes.Models.Count > 0)
        {
            _player = playerRes.Models[0];
        }
        else
        {
            var newPlayer = new Player
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

    private async UniTask EnsurePlayerStatsRecord()
    {
        var playerStatsRes = await _supabase.From<Stats>().Where(s => s.player_id == _player.id).Get();

        if (playerStatsRes.Models.Count == 0)
        {
            var newStats = new Stats
            {
                player_id = _player.id,
                deaths = 0,
                total_playtime_seconds = 0,
                total_achievements = 0
            };
            _playerStats = newStats;
            await _supabase.From<Stats>().Insert(newStats);
            Debug.Log("Created missing player stats record.");
        }
        else
        {
            _playerStats = playerStatsRes.Models[0];
        }
    }

    private async UniTask EnsurePlayerSavesRecord()
    {
        var playerSavesRes = await _supabase.Storage.From("player_saves").List(_player.id + "/");
        
        // Use a HashSet for quick lookup of existing files
        var existingSaves = new System.Collections.Generic.HashSet<string>();
        foreach (var item in playerSavesRes)
        {
            existingSaves.Add(item.Name);
        }

        // Clean logic up by treating 0 saves and missing saves identically
        byte[] emptySaveData = null;

        for (int i = 1; i <= 3; i++)
        {
            string saveName = $"save_{i}.json";
            if (!existingSaves.Contains(saveName))
            {
                if (emptySaveData == null)
                {
                    emptySaveData = Encoding.UTF8.GetBytes(Save.GetEmptySaveJson());
                }
                
                await _supabase.Storage.From("player_saves").Upload(emptySaveData, $"{_player.id}/{saveName}");
                Debug.Log($"Created missing {saveName} for player.");
            }
        }
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





#endregion

    private async UniTask ensureSupabaseInitialized()
    {
        if (_status == Status.Initializing)
        {
            await _initializeTask;
        }
    }


    // This method will update the deaths based on a paramater
    public async UniTask UpdatePlayerDeaths(int deaths)
    {
        await _initializeTask;

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

    public async UniTask IncrementDeaths()
    {
        print("Incrementing deaths in Supabase...");
        await _initializeTask;

        try{
            await _supabase.Rpc("increment_deaths", new { p_player_id = _player.id });
        }
        catch (Exception e){
            Debug.LogError($"Failed to increment deaths: {e}");
        }
    }





    // Gathering saves
    public async UniTask<Save> GetSaveFromSupabase(int slot)
    {
        await ensureSupabaseInitialized();
        

        try
        {
            var path = $"{_player.id}/save_{slot}.json";


            var saveData = await _supabase.Storage
                .From("player_saves")
                .Download(path, (EventHandler<float>)null, CancellationToken.None, null);

            var saveJson = Encoding.UTF8.GetString(saveData);
            return new Save(saveJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to download save: {e}");
            return null;
        }
    }

    public async UniTask<bool> SaveSaveToSupabase(int slot, Save save)
    {
        if (_status == Status.Offline)
        {
            Debug.LogWarning("Cannot save to cloud while in offline mode.");
            return false;
        }

        await _initializeTask;

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
