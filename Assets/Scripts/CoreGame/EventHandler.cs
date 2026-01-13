using UnityEngine;
using UnityEngine.Events;


public class EventHandler : MonoBehaviour
{
    const int INSPECTORSPACING = 8;
    [Header("Game Wide")][Space(INSPECTORSPACING)]
    [SerializeField] private UnityEvent _OnGameRestart = new();

    [Header("Character Events")][Space(INSPECTORSPACING)]
    [SerializeField] private MonsterEvents _MonsterEvents = new();
    [SerializeField] private PrincessEvents _PrincessEvents = new();

    [Header("Character Interaction Events")][Space(INSPECTORSPACING)]
    [SerializeField] private UnityEvent _OnPrincessJumpedOnTopOfMonster = new();
    [SerializeField] private UnityEvent _OnPrincessLeftTopOfMonster = new();
    [SerializeField] private UnityEvent _OnCrushPrincess = new();




    void Start()
    {

    }

    public void TriggerPrincessDeath() {_PrincessEvents.OnDeath.Invoke();}
    public void TriggerMonsterDeath() {_MonsterEvents.OnDeath.Invoke();}

    public void FallThroughHeadPlatform() {_PrincessEvents.OnFallingThroughHeadPlatform.Invoke();}

    public void RestartGame() {_OnGameRestart.Invoke();}

// Make the public getters

    //Princess
    public UnityEvent OnPrincessDeath => _PrincessEvents.OnDeath;
    public UnityEvent OnPrincessLandedOnGround => _PrincessEvents.OnLandedOnGround;
    public UnityEvent OnFallingThroughHeadPlatform => _PrincessEvents.OnFallingThroughHeadPlatform;

    //Monster
    public UnityEvent OnMonsterDeath => _MonsterEvents.OnDeath;
    public UnityEvent OnMonsterLandedOnGround => _MonsterEvents.OnLandedOnGround;

    //Character Interactions
    public UnityEvent OnPrincessJumpedOnTopOfMonster => _OnPrincessJumpedOnTopOfMonster;
    public UnityEvent OnPrincessLeftTopOfMonster => _OnPrincessLeftTopOfMonster;
    public UnityEvent OnCrushPrincess => _OnCrushPrincess;

    //Game Wide
    public UnityEvent OnGameRestart => _OnGameRestart;
}

[System.Serializable]
public class PrincessEvents
{
    public UnityEvent OnLandedOnGround = new();
    public UnityEvent OnDeath = new();
    public UnityEvent OnFallingThroughHeadPlatform = new();
}

[System.Serializable]
public class MonsterEvents
{
    public UnityEvent OnLandedOnGround = new();
    public UnityEvent OnDeath = new();
}
