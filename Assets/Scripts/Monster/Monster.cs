using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using Assets.Scripts.CustomDebug;



[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(SpriteRenderer))]
public class Monster : MonoBehaviour
{
    // Variable to hold the state object for the princess
    [HideInInspector]
    public MonsterState State { get; set; }

    // Two references for the princess's components
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    // Sprites for left/right movement and idle
    [Header("Monster Sprites")]
    public Sprite leftOrRightSprite;
    public Sprite notMovingSprite;

    // Reference to the monster GameObject
    public GameObject Princess;
    public PrincessState princessState;

    [SerializeField] GameLoopControler GameLoopControler;
    [SerializeField] EventHandler EventHandler;

    // Movement variables
    public float speed;
    public float jumpForce;
    public float carryingJumpForceMult;
    private float horizontal = 0;
    private bool jumpPressed = false;






    void Start()
    {
        // Initialize the state using the StateFactory
        State = (MonsterState) StateFactory.InitState(this.gameObject);

        // Set the references to the components to the actual components
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // get the princess game object
        Princess = GameObject.Find("Princess");
        princessState = Princess.GetComponent<Princess>().State;

        // This makes it so the Monster doesnt fall over (rotate on the z axis)
        rb.freezeRotation = true;


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
    void Update()
    {
        // We are only setting movement variables here because physics has to happen in a FixedUpdate
        // so we use the variables we set here in FixedUpdate to find out how to move the character
        // We use a horizontal variable to store the direction of movement inputed by the user
        if (Input.GetKey(KeyCode.LeftArrow)) { horizontal = -1; State.IsMoving = true; }
        else if (Input.GetKey(KeyCode.RightArrow)) { horizontal = 1; State.IsMoving = true; }
        else { horizontal = 0; State.IsMoving = false; }

        // Jumping
        if (Input.GetKeyDown(KeyCode.UpArrow) && State.IsGrounded) { jumpPressed = true; State.IsJumping = true; }
        else { State.IsJumping = false; }


        // flipping the sprite
        if (horizontal != 0)
        {
            sr.flipX = horizontal < 0;
            // sr.sprite = leftOrRightSprite;
        }
        // else
        // {
        //     sr.sprite = notMovingSprite;
        // }
    }

    // FixedUpdate is called at a fixed interval and is independent of frame rate. 
    // Put physics code here so the physics simulation is smooth regardless of framerate.
    void FixedUpdate()
    {

        //TODO: This should change to be using the new movement style 
        // Apply horizontal movement

        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);


        if (jumpPressed)
        {
            jumpPressed = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }



#region Collision Handl
    
        void OnCollisionEnter2D(Collision2D collision)
        {        
            var (monsterFootCollider, headPlatform, tilemap, princessCrush, princessFoot) = CollectCollisionRefs(collision);
            string debugMsg = GenerateObjectsPresentInCollisionDebugMessage(
                "Collision Enter detected with: ",
                monsterFootCollider, headPlatform, tilemap, princessCrush
            );
            print(debugMsg);

            if (collision.contactCount == 0) { Debug.LogWarning("No contact points in collision"); return; }

            for (int i = 0; i < collision.contactCount; i++)
            {
                // Monster is on the ground
                if (tilemap && monsterFootCollider && collision.GetContact(i).normal.y > 0.5f)
                {
                    State.IsGrounded = true;
                    State.IsJumping = false;
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
    
        void OnCollisionStay2D(Collision2D collision)
        {        
            var (monsterFootCollider, HeadPlatformCollider, Tilemap, princessCrushCollider, princessFootCollider) = CollectCollisionRefs(collision);
    
            string debugMsg = GenerateObjectsPresentInCollisionDebugMessage(
                "Collision Stay detected with: ",
                monsterFootCollider,
                HeadPlatformCollider,
                Tilemap,
                princessCrushCollider,
                princessFootCollider
            ); print(debugMsg);
    

    
            for(int i = 0; i < collision.contactCount; i++)
            {
                // Monster is on the ground
                if (Tilemap && monsterFootCollider && collision.GetContact(i).normal.y > 0.5f)
                {
                    print($"Monster is grounded on Tilemap");
                    State.IsGrounded = true;
                    State.IsJumping = false;
                    State.IsFalling = false;
                    break;
                }

                // Monster hit a wall
                if(Tilemap && monsterFootCollider && -0.5 < collision.GetContact(i).normal.y && collision.GetContact(i).normal.y < 0.5)
                {
                    State.WallHit = collision.GetContact(i).normal.x > 0 ? MonsterState.HitWall.left : MonsterState.HitWall.right;
                    break;
                }
    
                // Princess jumped on top of Monster
                if(princessFootCollider && HeadPlatformCollider && collision.GetContact(i).normal.y < -0.5f)
                {
                    State.PrincessOnTop = true;
                    Debug.Log("Princess is on top of Monster");
                }
            }
        }
    
        void OnCollisionExit2D(Collision2D collision)
        {        
            var (monsterFootCollider, HeadPlatformCollider, Tilemap, princessCrushCollider, princessFootCollider) = CollectCollisionRefs(collision);
    
            string debugMsg = GenerateObjectsPresentInCollisionDebugMessage(
                "Collision Exit detected with: ",
                monsterFootCollider,
                HeadPlatformCollider,
                Tilemap,
                princessCrushCollider,
                princessFootCollider
            ); print(debugMsg);

    
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

}


public class MonsterFoot : MonoBehaviour {}