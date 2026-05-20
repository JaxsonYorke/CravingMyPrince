using UnityEngine;
using Supabase;
using System;
using System.Text;
using System.Net.Http;
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
    public Client _supabase;
    public Player _player;
    public Stats _playerStats;

    [HideInInspector]
    public bool IsReady { get; private set; }
    
    [HideInInspector]
    public string _deviceId;


    private Task _initializeTask;
    private readonly HttpClient _httpClient = new HttpClient();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _deviceId = GetOrCreateDeviceId();
        Debug.Log($"DeviceId: {_deviceId}");

        _initializeTask = InitializeAsync();
        _ = StartFlowAsync();
    }

#region Initialization
    private async Task StartFlowAsync()
    {
        try
        {
            await _initializeTask;
            await EnsurePlayerRecord();
        }
        catch (Exception e)
        {
            Debug.LogError($"Supabase init failed: {e}");
        }
    }

    private async Task EnsurePlayerRecord()
    {
        print("initializing player record...");
        await EnsureInitializedAsync();
        Player newPlayer = null;
        Stats newStats = null;

        var playerRes = await _supabase.From<Player>().Where(p => p.device_id == _deviceId).Get();
        _player = playerRes.Models.Count > 0 ? playerRes.Models[0] : null;

        var playerStatsRes = await _supabase.From<Stats>().Where(s => s.player_id == _player.id).Get();
        _playerStats = playerStatsRes.Models.Count > 0 ? playerStatsRes.Models[0] : null;

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

    private void PostMessage(NotificationType type, string title, Exception exception = null)
    {
        string message = $"[{type}] {title}";
        if (exception != null)
        {
            message += $"\n{exception.Message}";
        }
        
        Debug.Log(message);
        // Add your UI notification logic here
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




    // public async Task SendAnalyticsEvent(string eventName, string payloadJson = null)
    // {
    //     await EnsureInitializedAsync();

    //     var analyticsEvent = new AnalyticsEvent
    //     {
    //         device_id = _deviceId,
    //         event_name = eventName,
    //         payload = payloadJson,
    //         created_at = DateTime.UtcNow
    //     };

    //     await _supabase.From<AnalyticsEvent>().Insert(analyticsEvent);
    // }

    // public async Task<bool> UploadCloudSave(string saveSlot, string dataJson)
    // {
    //     await EnsureInitializedAsync();

    //     var request = new CloudSaveRequest
    //     {
    //         device_id = _deviceId,
    //         save_slot = saveSlot,
    //         data_json = dataJson
    //     };

    //     string url = $"{SupabaseSettings.SupabaseURL}/functions/v1/{SupabaseSettings.SaveUploadFunction}";
    //     string json = JsonUtility.ToJson(request);
    //     var response = await PostJsonAsync(url, json);

    //     if (!response.IsSuccessStatusCode)
    //     {
    //         Debug.LogError($"Save upload failed: {(int)response.StatusCode} {response.ReasonPhrase}");
    //         return false;
    //     }

    //     return true;
    // }

    // public async Task<string> DownloadCloudSave(string saveSlot)
    // {
    //     await EnsureInitializedAsync();

    //     var request = new CloudSaveRequest
    //     {
    //         device_id = _deviceId,
    //         save_slot = saveSlot
    //     };

    //     string url = $"{SupabaseSettings.SupabaseURL}/functions/v1/{SupabaseSettings.SaveDownloadFunction}";
    //     string json = JsonUtility.ToJson(request);
    //     var response = await PostJsonAsync(url, json);

    //     if (!response.IsSuccessStatusCode)
    //     {
    //         Debug.LogError($"Save download failed: {(int)response.StatusCode} {response.ReasonPhrase}");
    //         return null;
    //     }

    //     return await response.Content.ReadAsStringAsync();
    // }

    // private async Task<HttpResponseMessage> PostJsonAsync(string url, string json)
    // {
    //     var request = new HttpRequestMessage(HttpMethod.Post, url)
    //     {
    //         Content = new StringContent(json, Encoding.UTF8, "application/json")
    //     };

    //     request.Headers.Add("apikey", SupabaseSettings.SupabaseAnonKey);
    //     request.Headers.Add("Authorization", $"Bearer {SupabaseSettings.SupabaseAnonKey}");

    //     return await _httpClient.SendAsync(request);
    // }


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
