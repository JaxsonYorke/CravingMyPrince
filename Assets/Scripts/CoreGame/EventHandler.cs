using UnityEngine;
using UnityEngine.Events;

// AI says that at some point this should be changed to using regualr C# events instead of UnityEvents, but for now this is easier to work with in the inspector and is more flexible for prototyping purposes.


public class EventHandler : MonoBehaviour
{
    const int INSPECTORSPACING = 8;
    [Header("Game Wide")][Space(INSPECTORSPACING)]
    [SerializeField] private UnityEvent _OnGameRestart = new();
    [SerializeField] private UnityEvent _OnGameWin = new();

    [Header("Character Events")][Space(INSPECTORSPACING)]
    [SerializeField] private MonsterEvents _MonsterEvents = new();
    [SerializeField] private PrincessEvents _PrincessEvents = new();

    [Header("Character Interaction Events")][Space(INSPECTORSPACING)]
    [SerializeField] private UnityEvent _OnPrincessJumpedOnTopOfMonster = new();
    [SerializeField] private UnityEvent _OnPrincessLeftTopOfMonster = new();
    [SerializeField] private UnityEvent _OnCrushPrincess = new();




    public void TriggerPrincessDeath() {_PrincessEvents.OnDeath.Invoke();}
    public void TriggerMonsterDeath() {_MonsterEvents.OnDeath.Invoke();}

    public void FallThroughHeadPlatform(Rigidbody2D rb) {_PrincessEvents.OnFallingThroughHeadPlatform.Invoke(rb);}

    public void RestartGame() {_OnGameRestart.Invoke();}
    public void WinGame() {_OnGameWin.Invoke();}

// Make the public getters

    //Princess
    public UnityEvent OnPrincessDeath => _PrincessEvents.OnDeath;
    public UnityEvent OnPrincessLandedOnGround => _PrincessEvents.OnLandedOnGround;
    public Rigidbody2DEvent OnFallingThroughHeadPlatform => _PrincessEvents.OnFallingThroughHeadPlatform;

    //Monster
    public UnityEvent OnMonsterDeath => _MonsterEvents.OnDeath;
    public UnityEvent OnMonsterLandedOnGround => _MonsterEvents.OnLandedOnGround;

    //Character Interactions
    public UnityEvent OnPrincessJumpedOnTopOfMonster => _OnPrincessJumpedOnTopOfMonster;
    public UnityEvent OnPrincessLeftTopOfMonster => _OnPrincessLeftTopOfMonster;
    public UnityEvent OnCrushPrincess => _OnCrushPrincess;

    //Game Wide
    public UnityEvent OnGameRestart => _OnGameRestart;
    public UnityEvent OnGameWin => _OnGameWin; // For now we can just use the same event for restarting and winning, but this can be changed later if we want different behaviour for each.
}

[System.Serializable]
public class PrincessEvents
{
    public UnityEvent OnLandedOnGround = new();
    public UnityEvent OnDeath = new();
    public Rigidbody2DEvent OnFallingThroughHeadPlatform = new();
}

[System.Serializable]
public class MonsterEvents
{
    public UnityEvent OnLandedOnGround = new();
    public UnityEvent OnDeath = new();
}


public class Rigidbody2DEvent : UnityEvent<Rigidbody2D> {}