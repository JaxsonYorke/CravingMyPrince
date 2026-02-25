using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Assets.Scripts.CustomDebug;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(SpriteRenderer))]
public class Princess : MonoBehaviour
{
    // Variable to hold the state object for the princess
    [HideInInspector]
    public PrincessState State { get; set; }
    // Two references for the princess's components
    public Rigidbody2D rb;
    public SpriteRenderer sr;

    // Sprites for left/right movement and idle
    public Sprite leftOrRightSprite;
    public Sprite notMovingSprite;

    // Reference to the monster GameObject
    public GameObject Monster;

    [SerializeField] EventHandler EventHandler;

    // Movement variables
    private int horizontal = 0;
    private bool jumpPressed = false;
    public float speed;
    public float jumpForce;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize the state using the StateFactory
        State = (PrincessState) StateFactory.InitState(this.gameObject);

        // Set the references to the components to the actual components
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();


        // This makes it so the princess doesnt fall over (rotate on the z axis)
        rb.freezeRotation = true;
    

        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Princess IsGrounded", () => State.IsGrounded));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Princess IsOnMonster", () => State.IsOnMonster));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Princess IsMoving", () => State.IsMoving));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Princess IsJumping", () => State.IsJumping));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Princess IsFalling", () => State.IsFalling));

        if (rb != null)
        {
            DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
                new DebugStatsRequest("Princess Velocity", () => rb.linearVelocity));
        }

        // Listen for own death
        EventHandler.OnPrincessDeath.AddListener(Death);
    }

    // Update is called once per frame, This is for detection of inputs from users
    // and logic that doesn't involve physics
    void Update()
    {
        //TODO: this is going to have to change when we change to other movement
        State.IsFalling = rb.linearVelocity.y < 0 && !State.IsGrounded;
        // We are only setting movement variables here because physics has to happen in a FixedUpdate
        // so we use the variables we set here in FixedUpdate to find out how to move the character


        // We use a horizontal variable to store the direction of movement inputed by the user
        if (Input.GetKey(KeyCode.A)) {       horizontal = -1;  State.IsMoving = true; }
        else if (Input.GetKey(KeyCode.D)) {  horizontal = 1;   State.IsMoving = true; }
        else {                               horizontal = 0;   State.IsMoving = false; }

        // Jumping
        if (Input.GetKeyDown(KeyCode.W) && State.IsGrounded) { 
            jumpPressed = true; State.IsJumping = true; 
        }

// Going Down Through a block
        if (Input.GetKeyDown(KeyCode.S) && State.IsOnMonster)
        {
            // Turn off the head platform collider and the enable if above script
            GameObject HeadPlatformColliderGameObject = Monster.GetComponentInChildren<HeadPlatformCollider>().gameObject;
            HeadPlatformColliderGameObject.GetComponent<EnableIfAbove>().enabled = false;
            HeadPlatformColliderGameObject.GetComponent<BoxCollider2D>().enabled = false;
        }

        if (Input.GetKeyUp(KeyCode.S))
        {
            GameObject HeadPlatformColliderGameObject = Monster.GetComponentInChildren<HeadPlatformCollider>().gameObject;
            HeadPlatformColliderGameObject.GetComponent<EnableIfAbove>().enabled = true;
            HeadPlatformColliderGameObject.GetComponent<BoxCollider2D>().enabled = true;
        }


        // flipping the sprite
        if (State.IsMoving)
        {
            sr.flipX = horizontal < 0;
            sr.sprite = leftOrRightSprite;
        }
        else sr.sprite = notMovingSprite;
    }

    // FixedUpdate is called at a fixed interval and is independent of frame rate. 
    // Put physics code here so the physics simulation is smooth regardless of framerate.
    void FixedUpdate()
    {

        //TODO: all going to have to change when we change to new movement style
        // Jumping
        if (jumpPressed)
        {
            // only jump once per press
            jumpPressed = false;
            // set the y velocity to the jump force
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        // This makes it so if the player releases the jump button while going up, they will fall faster
        else if (Input.GetKeyUp(KeyCode.W) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        // Check if princess is on top of monster and moving, If so her movement should be the monsters, plus her own input
        if (State.IsOnMonster)
        {
            // When on monster, add player input to existing velocity (which includes monster's movement)
            Vector2 currentVelocity = rb.linearVelocity;
            Vector2 monsterVelocity = Monster.GetComponent<Rigidbody2D>().linearVelocity;
            float relativeYVelocity = Math.Clamp(currentVelocity.y - monsterVelocity.y, 0, float.PositiveInfinity);
            print("Relative Y Velocity: " + relativeYVelocity);
            print("Monster Prior Velocity: " + monsterVelocity);
            print("Current Prior Velocity: " + currentVelocity);
            rb.linearVelocity = new Vector2(monsterVelocity.x + (speed * horizontal), relativeYVelocity + monsterVelocity.y);
            print("New Velocity: " + rb.linearVelocity);
        }
        else
        {
            // Normal movement when not on monster
            rb.linearVelocity = new Vector2(speed * horizontal, rb.linearVelocity.y);
        }

    }

    #region Collision Handl



// MARK:Enter
        void OnCollisionEnter2D(Collision2D collision)
        {
            var (princessFootCollider, 
            tilemapCollider, 
            headPlatformCollider, 
            crushCollider) = CollectCollisionRefs(collision);
    
    
            print(GenerateObjectsPresentInCollisionDebugMessage("Princess Collision Enter Check:", princessFootCollider, tilemapCollider, headPlatformCollider, crushCollider));
    
    
            if(collision.contactCount == 0)
            {
                Debug.LogWarning("No contact points in collision");
                return;
            }
    
            if (tilemapCollider && princessFootCollider && collision.GetContact(0).normal.y > 0.5f)
            {
                State.IsGrounded = true;
                State.IsJumping = false;
                State.IsFalling = false;
                EventHandler.OnPrincessLandedOnGround.Invoke();
                Debug.Log("Princess landed on Tilemap");
            }
            else if (headPlatformCollider && princessFootCollider && collision.GetContact(0).normal.y > 0.5f)
            {
                State.IsGrounded = true;
                State.IsOnMonster = true;
                State.IsJumping = false;
                State.IsFalling = false;
                EventHandler.OnPrincessJumpedOnTopOfMonster.Invoke();
                Debug.Log("Princess landed on HeadPlatformCollider");
            }
        }
    
// MARK:Stay

        void OnCollisionStay2D(Collision2D collision)
        {
            var (princessFootCollider, 
            tilemapCollider, 
            headPlatformCollider, 
            crushCollider) = CollectCollisionRefs(collision);
    
            string debugMsg = GenerateObjectsPresentInCollisionDebugMessage("Princess Stay Collision:", tilemapCollider, headPlatformCollider, princessFootCollider);
            print(debugMsg);
    
            if (tilemapCollider && princessFootCollider && collision.GetContact(0).normal.y > 0.5f)
            {
                State.IsGrounded = true;
                State.IsJumping = false;
                State.IsFalling = false;
                
            }
            else if (headPlatformCollider && princessFootCollider && collision.GetContact(0).normal.y > 0.5f)
            {
                State.IsGrounded = true;
                State.IsOnMonster = true;
                State.IsJumping = false;
                State.IsFalling = false;
            }
        }
    
// MARK:Exit

        void OnCollisionExit2D(Collision2D collision)
        {
            var (princessFootCollider, 
            tilemapCollider, 
            headPlatformCollider, 
            crushCollider) = CollectCollisionRefs(collision);
    
            string debugMsg = GenerateObjectsPresentInCollisionDebugMessage("Princess Collision Exit Check: ", tilemapCollider, headPlatformCollider, princessFootCollider);
            print(debugMsg);
    
            if (tilemapCollider && princessFootCollider)
            {
                print($"Princess left the tilemap");
                State.IsGrounded = false;
            }
            else if (headPlatformCollider && princessFootCollider)
            {
                State.IsGrounded = false;
                State.IsOnMonster = false;
                //TODO: This should add the monsters movement to the princess's movement when she leaves 
                Debug.Log("Princess left HeadPlatformCollider");
            }
            
        }
    #endregion

    #region Helper Methods

        /// <summary>
        /// Generates a debug message indicating which objects are present in the collision.
        /// </summary>
        /// <param name="prefix">The initial part of the debug message.</param>
        /// <param name="items">The objects to check for presence.</param>
        /// <returns>A formatted debug message listing the present objects.</returns>
        private static string GenerateObjectsPresentInCollisionDebugMessage(string prefix, params object[] items)
        {
            foreach (var item in items)
            {
                if(item == null) {continue;} else { prefix += $"{item} is present; "; }
            }
            return prefix;
        }

        /// <summary>
        /// Collects and returns references to various collision components from the collision objects.
        /// </summary>
        /// <param name="collision">The collision information containing references to the colliders involved.</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>PrincessFootCollider - The foot collider component from the other colliding object.</description></item>
        /// <item><description>TilemapCollider2D - The tilemap collider component from the colliding object.</description></item>
        /// <item><description>HeadPlatformCollider - The head platform collider component from the colliding object.</description></item>
        /// <item><description>CrushCollider - The crush collider component from the colliding object.</description></item>
        /// </list>
        /// </returns>
        private 
        (PrincessFootCollider,
        TilemapCollider2D,
        HeadPlatformCollider,
        CrushCollider)
            CollectCollisionRefs(Collision2D collision){
            return (
                collision.otherCollider.gameObject.GetComponent<PrincessFootCollider>(),
                collision.collider.gameObject.GetComponent<TilemapCollider2D>(),
                collision.collider.gameObject.GetComponent<HeadPlatformCollider>(),
                collision.collider.gameObject.GetComponent<CrushCollider>()
            );
        }
    #endregion

    public void Death()
    {
        Destroy(this.gameObject);
        Debug.LogFormat("{0} is dead", this.gameObject.name);
    }
    
}
