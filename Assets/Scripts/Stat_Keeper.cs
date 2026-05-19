using UnityEngine;

public class Stat_Keeper : MonoBehaviour
{
    public SupabaseManager supabaseManager;
    public EventHandler eventHandler;

    private void Start()
    {
        eventHandler.OnPrincessDeath.AddListener(IncrementDeaths);
        eventHandler.OnMonsterDeath.AddListener(IncrementDeaths);
    }

    public void IncrementDeaths()
    {
        _ = supabaseManager.IncrementDeaths();
    }

    
}
