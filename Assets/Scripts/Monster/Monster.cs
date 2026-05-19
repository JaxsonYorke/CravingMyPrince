using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

using Assets.Scripts.CustomDebug;
using System.Collections;
using UnityEngine.Assertions;


/*
 * Notes:
 */


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(SpriteRenderer))]
public class Monster : MonoBehaviour
{
    // Variable to hold the state object for the princess
    public MonsterState State { get; set; }


    // Sprites for left/right movement and idle
    [SerializeField] public MonsterSprites MonsterSprites;

    // Reference to the monster GameObject
    [SerializeField] private GameObject Princess;
    [SerializeField] private PrincessState princessState;

    [SerializeField] private GameLoopControler GameLoopControler;
    [SerializeField] private EventHandler EventHandler;



    [SerializeField] public float speed;
    [SerializeField] public float jumpForce;
    [SerializeField] public float carryingJumpForceMult;
    [SerializeField] public float InAirMovementSpeedDampener;

    // Two references for the princess's components
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private GameObject headPlatformCollider;
    private EnableIfAbove enableIfAbove;


    public InputActionAsset _InputSystem;
    private InputAction m_move;
    private InputAction m_jump;

    private void OnEnable()
    {
        // INPUT
        _InputSystem.FindActionMap("Monster").Enable();

        m_move = _InputSystem["Monster/Move"];
        m_jump = _InputSystem["Monster/Jump"];

        // EVENTS
        EventHandler.OnMonsterDeath.AddListener(Death);
        EventHandler.OnFallingThroughHeadPlatform.AddListener((rb) => LetGameObjectFallThroughHeadPlatform(rb));
    }

    private void OnDisable()
    {
        _InputSystem.FindActionMap("Monster").Disable();
    }



    private void Awake()
    {
        // Initialize the state using the StateFactory
        State = (MonsterState) StateFactory.InitState(this.gameObject);

        // Set the references to the components to the actual components
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        headPlatformCollider = GetComponentInChildren<HeadPlatformCollider>().gameObject;
        enableIfAbove = GetComponentInChildren<EnableIfAbove>();

        // This makes it so the Monster doesnt fall over (rotate on the z axis)
        rb.freezeRotation = true;
    }


    private void Start()
    {

        // get the princess game object
        Princess = GameObject.Find("Princess");
        princessState = Princess.GetComponent<Princess>().State;

        
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Monster IsGrounded", () => {return State.IsGrounded;}));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Monster Princess on top", () => State.PrincessOnTop));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Monster IsMoving", () => State.IsMoving));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Monster IsJumping", () => State.IsJumping));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Monster IsFalling", () => State.IsFalling));

        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Monster Velocity", () => rb.linearVelocity));

        // Listen for own death
        EventHandler.OnMonsterDeath.AddListener(Death);
    }

    // Update is called once per frame, This is for detection of inputs from users
    // and logic that doesn't involve physics
        // Movement variables
    private float horizontal = 0;
    private bool jumpPressed = false;
    private bool jumpWasReleased = false;
    private void Update()
    {
        // We are only setting movement variables here because physics has to happen in a FixedUpdate
        // so we use the variables we set here in FixedUpdate to find out how to move the character
        // We use a horizontal variable to store the direction of movement inputed by the user
        horizontal = m_move.ReadValue<float>();

        // Jumping
        if (m_jump.WasPressedThisFrame() && State.IsGrounded) {
            jumpPressed = true; 
            State.IsJumping = true; 
            print($"Monster IsJumping set to true");
        }

        if(m_jump.WasReleasedThisFrame() && rb.linearVelocity.y > 0.1f && State.IsJumping)
        {
            jumpWasReleased = true;
        }


        // flipping the sprite
        if (horizontal != 0)
        {
            sr.flipX = horizontal < 0;
        }

    }

    // FixedUpdate is called at a fixed interval and is independent of frame rate. 
    // Put physics code here so the physics simulation is smooth regardless of framerate.
    private bool _maxSpeedSet = false;
    private float _maxAirborneHorizontalSpeed = 0;
    void FixedUpdate()
    {
        State.IsFalling = rb.linearVelocity.y < 0 && !State.IsGrounded;

        float onGroundMovement = speed * horizontal * Time.fixedDeltaTime;
        float inAirMovement =  speed * horizontal * Time.fixedDeltaTime * InAirMovementSpeedDampener;

        void clearMaxAirborneHorizontalSpeed()
        {
            _maxSpeedSet = false;
            _maxAirborneHorizontalSpeed = 0;
        }

        void setMaxAirborneHorizontalSpeed()
        {
            if(!_maxSpeedSet)
            {
                _maxAirborneHorizontalSpeed = Mathf.Max(Mathf.Abs(rb.linearVelocityX), Mathf.Abs(speed * Time.fixedDeltaTime));
                _maxSpeedSet = true;
            }
        }
    


        //TODO: This should change to be using the new movement style 
        // Apply horizontal movement
        if(State.IsGrounded)
        {
            clearMaxAirborneHorizontalSpeed();
            rb.linearVelocityX = onGroundMovement;
        } else if(State.IsInAir)
        {
            setMaxAirborneHorizontalSpeed();
            rb.linearVelocityX += inAirMovement;
            rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -Mathf.Abs(_maxAirborneHorizontalSpeed), Mathf.Abs(_maxAirborneHorizontalSpeed));
        }
        else throw new System.Exception("Monster is in an invalid state where it is not grounded but also not in the air");


        // Apply jump
        if (jumpPressed && State.IsGrounded)
        {
            jumpPressed = false;
            State.IsJumping = true;
            print($"Monster IsJumping set to true");

            rb.linearVelocityY += jumpForce * (State.PrincessOnTop ? carryingJumpForceMult : 1);
        }
        if(jumpWasReleased && State.IsJumping && rb.linearVelocity.y > 0.1f)
        {
            rb.linearVelocityY *= 0.5f;
            jumpWasReleased = false;
        }
    }



#region Collision Handler



// MARK:Enter

    void OnCollisionEnter2D(Collision2D collision)
    {    
            
        var (monsterFootCollider, headPlatform, tilemap, princessCrush, princessFoot) = CollectCollisionRefs(collision);
        // string debugMsg = GenerateObjectsPresentInCollisionDebugMessage(
        //     "Collision Enter detected with: ",
        //     monsterFootCollider, headPlatform, tilemap, princessCrush
        // );
        // print(debugMsg);

        if (collision.contactCount == 0) { Debug.LogWarning("No contact points in collision"); return; }

        for (int i = 0; i < collision.contactCount; i++)
        {
            // Monster is on the ground
            if (tilemap && monsterFootCollider && collision.GetContact(i).normal.y > 0.5f)
            {
                State.IsGrounded = true;
                State.IsJumping = false;
                print($"Monster IsJumping set to false");
                State.IsFalling = false;
                EventHandler.OnMonsterLandedOnGround.Invoke();
                Debug.Log("Monster landed on Tilemap");
            }
            
            // Monster hit a wall
            if(tilemap && monsterFootCollider && -0.5 < collision.GetContact(i).normal.y && collision.GetContact(i).normal.y < 0.5)
            {
                State.WallHit = collision.GetContact(i).normal.x > 0 ? MonsterState.HitWall.left : MonsterState.HitWall.right;
            }
    
            // Monster crushed the princess
            if (princessCrush && monsterFootCollider && collision.GetContact(i).normal.y > 0.5f)
            {
                Debug.Log("Monster crushed the Princess!");
                GameLoopControler.tryCrushPrincess();
            }
    
            // Princess jumped on top of Monster
            if (princessFoot && headPlatform && collision.GetContact(i).normal.y < -0.5f)
            {
                State.PrincessOnTop = true;
                EventHandler.OnPrincessJumpedOnTopOfMonster.Invoke();
                Debug.Log("Princess is on top of Monster");
            }

        }

    }

// MARK:Stay
    // void OnCollisionStay2D(Collision2D collision)
    // {        
    //     var (monsterFootCollider, HeadPlatformCollider, Tilemap, princessCrushCollider, princessFootCollider) = CollectCollisionRefs(collision);

    //     // string debugMsg = GenerateObjectsPresentInCollisionDebugMessage(
    //     //     "Collision Stay detected with: ",
    //     //     monsterFootCollider,
    //     //     HeadPlatformCollider,
    //     //     Tilemap,
    //     //     princessCrushCollider,
    //     //     princessFootCollider
    //     // ); print(debugMsg);



    //     for(int i = 0; i < collision.contactCount; i++)
    //     {
    //         // Monster is on the ground
    //         if (Tilemap && monsterFootCollider && collision.GetContact(i).normal.y > 0.5f)
    //         {
    //             // print($"Monster is grounded on Tilemap");
    //             State.IsGrounded = true;
    //             print($"Monster IsJumping set to false");
    //             State.IsJumping = false;
    //             State.IsFalling = false;
    //             break;
    //         }

    //         // Monster hit a wall
    //         if(Tilemap && monsterFootCollider && -0.5 < collision.GetContact(i).normal.y && collision.GetContact(i).normal.y < 0.5)
    //         {
    //             State.WallHit = collision.GetContact(i).normal.x > 0 ? MonsterState.HitWall.left : MonsterState.HitWall.right;
    //             break;
    //         }

    //         // Princess jumped on top of Monster
    //         if(princessFootCollider && HeadPlatformCollider && collision.GetContact(i).normal.y < -0.5f)
    //         {
    //             State.PrincessOnTop = true;
    //             Debug.Log("Princess is on top of Monster");
    //         }
    //     }
    // }

// MARK:Exit
    void OnCollisionExit2D(Collision2D collision)
    {        
        var (monsterFootCollider, HeadPlatformCollider, Tilemap, princessCrushCollider, princessFootCollider) = CollectCollisionRefs(collision);

        // string debugMsg = GenerateObjectsPresentInCollisionDebugMessage(
        //     "Collision Exit detected with: ",
        //     monsterFootCollider,
        //     HeadPlatformCollider,
        //     Tilemap,
        //     princessCrushCollider,
        //     princessFootCollider
        // ); print(debugMsg);


        if (Tilemap && monsterFootCollider)
        {
            print($"Monster left the tilemap");
            State.IsGrounded = false;
        }

        if (princessFootCollider && HeadPlatformCollider)
        {
            State.PrincessOnTop = false;
            EventHandler.OnPrincessLeftTopOfMonster.Invoke();
            Debug.Log("Princess left the top of Monster");
        }


    }

    // void OnTriggerEnter2D(Collider2D otherCollider)
    // {
    // }
    
#endregion


#region Helper Methods

    private 
    (MonsterFootCollider monsterFootCollider,
    HeadPlatformCollider headPlatform, 
    TilemapCollider2D tilemap,
    CrushCollider princessCrush, 
    PrincessFootCollider princessFoot)
        CollectCollisionRefs(Collision2D collision){
        return (
            collision.otherCollider.gameObject.GetComponent<MonsterFootCollider>(),
            collision.collider.gameObject.GetComponent<HeadPlatformCollider>(),
            collision.collider.gameObject.GetComponent<TilemapCollider2D>(),
            collision.collider.gameObject.GetComponent<CrushCollider>(),
            collision.otherCollider.gameObject.GetComponent<PrincessFootCollider>()
        );
    }

    private static string GenerateObjectsPresentInCollisionDebugMessage(string prefix, params object[] items)
    {
        string debugMsg = prefix;
        foreach (var item in items)
        {
            if(item == null) {continue;} else { prefix += $"{item} is present; "; }
        }
        return debugMsg;
    }
#endregion

    public void Death()
    {
        Destroy(this.gameObject);
    }

    public void LetGameObjectFallThroughHeadPlatform(Rigidbody2D gameObject)
    {
        DisableHeadPlatformCollider();
        // we are going to make a courutine here that is going to turn back on the head platform collider once the game object has fallen through it.
        // ! This might make a memory leak if the princess never falls below the head platform, and could cause problems of the player trying to jump back on it or smth.
        StartCoroutine(EnableHeadPlatformColliderAfterObjectFallsThrough(gameObject));
    }

    public IEnumerator EnableHeadPlatformColliderAfterObjectFallsThrough(Rigidbody2D gameObject)
    {

        Assert.IsNotNull(gameObject, "GameObject passed to EnableHeadPlatformColliderAfterObjectFallsThrough is null");
        Assert.IsNotNull(headPlatformCollider, "HeadPlatformCollider is null in EnableHeadPlatformColliderAfterObjectFallsThrough");
        
        // Wait until the game object is below the head platform collider
        while (gameObject.transform.position.y > headPlatformCollider.transform.position.y)
        {
            yield return null;
        }
        EnableHeadPlatformCollider();
    }


    public void DisableHeadPlatformCollider()
    {
        headPlatformCollider.GetComponent<EnableIfAbove>().enabled = false;
        headPlatformCollider.GetComponent<BoxCollider2D>().enabled = false;
    }

    public void EnableHeadPlatformCollider()
    {
        headPlatformCollider.GetComponent<EnableIfAbove>().enabled = true;
        headPlatformCollider.GetComponent<BoxCollider2D>().enabled = true;
    }

}

public class MonsterFoot : MonoBehaviour {}

[System.Serializable]
public class MonsterSprites
{
    public Sprite leftOrRightSprite;
    public Sprite notMovingSprite;
}