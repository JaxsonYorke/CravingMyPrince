using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using Assets.Scripts.CustomDebug;
using UnityEngine.Assertions;


/*
 * Notes:
 * - The movement controls for the characters are going to all stay in the moveVector2, x is the left and right, the y is up and down
 */


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(SpriteRenderer))]
public class Princess : MonoBehaviour
{
    // Variable to hold the state object for the princess
    [HideInInspector] public PrincessState State { get; set; }

    [SerializeField] private EventHandler EventHandler;
    [SerializeField] private GameObject Monster;
    private Rigidbody2D monsterRb;

    [SerializeField] public PrincessSprites PrincessSprites;

    // Reference to the monster GameObject

    private Rigidbody2D rb;
    private SpriteRenderer sr;



    [SerializeField] public float speed;
    [SerializeField] public float jumpForce;
    [SerializeField] public float InAirMovementSpeedDampener;
    [SerializeField] public float coyoteTime;
    
    
    // Input Actions
    public InputActionAsset InputActions;
    private InputAction m_moveAction;
    private InputAction m_jumpAction;
    private InputAction m_fallThroughAction;


    private void OnEnable()
    {
        print("enabled");
        InputActions.FindActionMap("Princess").Enable();
        m_moveAction = InputActions["Princess/Move"];
        m_jumpAction = InputActions["Princess/Jump"];
        m_fallThroughAction = InputActions["Princess/FallThrough"];

    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Princess").Disable();
    }


    private void Awake()
    {
        
        // Initialize the state using the StateFactory
        State = (PrincessState) StateFactory.InitState(this.gameObject);
        
        // Set the references to the components to the actual components
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        
        // This makes it so the princess doesnt fall over (rotate on the z axis)
        rb.freezeRotation = true;
    }
    
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Assert.IsNotNull(EventHandler, "EventHandler reference is not set in the inspector");
        Assert.IsNotNull(InputActions, "InputActions reference is not set in the inspector");
        Assert.IsNotNull(Monster, "Monster reference is not set in the inspector");

        Assert.IsNotNull(rb, "Rigidbody2D component not found on Princess");
        Assert.IsNotNull(sr, "SpriteRenderer component not found on Princess");    

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
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Princess Horizontal Input", () => horizontal));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Princess Input", () => m_moveAction.ReadValue<float>()));
        DebugStatsDisplay.Instance.RegisterDebugStatsRequest(
            new DebugStatsRequest("Princess Velocity", () => rb.linearVelocity));
        

        monsterRb = Monster.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(monsterRb, "Monster Rigidbody2D component not found");

        // Listen for own death
        EventHandler.OnPrincessDeath.AddListener(Death);
    }


    // Update is called once per frame, This is for detection of inputs from users
    // and logic that doesn't involve physics

    private Coroutine coyoteTimeCouroutineRef;
    private float horizontal = 0;
    private bool jumpPressed = false;
    private bool jumpWasReleased = false;
    void Update()
    {
        //TODO: this is going to have to change when we change to other movement
        State.IsFalling = rb.linearVelocity.y < 0 && !State.IsGrounded;
        // We are only setting movement variables here because physics has to happen in a FixedUpdate
        // so we use the variables we set here in FixedUpdate to find out how to move the character


        // We use a horizontal variable to store the direction of movement inputed by the user
        horizontal = m_moveAction.ReadValue<float>();

        // Jumping
        if (m_jumpAction.WasPressedThisFrame() && State.IsGrounded) {
            print("Jump button pressed and princess is grounded, setting jumpPressed to true");
            //? maybe at some point we implement asking the game if they can jump. This way we could have alot more control over the characters being able to do certain actions based on the gamestate or powers or something like that 
            jumpPressed = true;
            if(coyoteTimeCouroutineRef != null)
            {
                StopCoroutine(coyoteTimeCouroutineRef);
            }
            coyoteTimeCouroutineRef = StartCoroutine(ResetCoyoteTime());
        }
        if(m_jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0.1f && State.IsJumping)
        {
            jumpWasReleased = true;
        }

        // Going Down Through a block
        if (m_fallThroughAction.WasPressedThisFrame() && State.IsOnMonster)
        {
            EventHandler.FallThroughHeadPlatform(rb);
        }



        // flipping the sprite
        if (State.IsMoving)
        {
            sr.flipX = horizontal < 0;
            sr.sprite = PrincessSprites.leftOrRightSprite;
        }
        else sr.sprite = PrincessSprites.notMovingSprite;
    }

    // FixedUpdate is called at a fixed interval and is independent of frame rate. 
    // Put physics code here so the physics simulation is smooth regardless of framerate.
    Vector2 _inheritedVelocityFromMonster = Vector2.zero;
    Vector2 _playerVelocity = Vector2.zero;
    private float _lastHorizontalInput = 0;
    private bool _justLeftMonster = false;

    private bool _maxSpeedSet = false;
    private float _maxAirborneHorizontalSpeed = 0;
    private void FixedUpdate()
    {
        State.IsMoving = horizontal != 0;
        State.IsFalling = rb.linearVelocity.y < 0 && !State.IsGrounded;

        float onGroundMovement = speed * horizontal * Time.fixedDeltaTime;
        float inAirMovement =  speed * horizontal * Time.fixedDeltaTime * InAirMovementSpeedDampener;
        float onMonsterMovement = onGroundMovement + monsterRb.linearVelocity.x;
        
        void clearMaxAirborneHorizontalSpeed()
        {
            _maxSpeedSet = false;
            _maxAirborneHorizontalSpeed = 0;
        }

        void setMaxAirborneHorizontalSpeed()
        {
            if(!_maxSpeedSet)
            {
                _maxAirborneHorizontalSpeed = Math.Max(Math.Abs(_playerVelocity.x), Math.Abs(speed * Time.fixedDeltaTime));
                _maxSpeedSet = true;
            }
        }




        _playerVelocity = rb.linearVelocity;

        if(State.IsGrounded){
            clearMaxAirborneHorizontalSpeed();
            if(State.IsOnMonster) {
                _playerVelocity.x = onMonsterMovement;
            } else {
                _playerVelocity.x = onGroundMovement;
            }

        } else if(State.IsInAir) {
            setMaxAirborneHorizontalSpeed();
            _playerVelocity.x += inAirMovement;

// ! This clamps the other side aswell, for now its fine but if we get them to fall really far, this might stop them from going far in the other direction.
            _playerVelocity.x = Mathf.Clamp(_playerVelocity.x, -Mathf.Abs(_maxAirborneHorizontalSpeed), Mathf.Abs(_maxAirborneHorizontalSpeed));
        }



        // Jumping
        if (jumpPressed && State.IsGrounded)
        {
            try
            {
                StopCoroutine(coyoteTimeCouroutineRef);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Tried to stop coyote time coroutine but it was already stopped. Exception: " + e);
            }

            _inheritedVelocityFromMonster = Vector2.zero;

            // only jump once per press
            jumpPressed = false;
            State.IsJumping = true;

            // Princess normal jump velocity
            Vector2 jumpVelocity = Vector2.up * jumpForce;


            // Add the final Vertical jump velocity to the new velocity
            _playerVelocity += jumpVelocity;
        }

        // This makes it so if the player releases the jump button while going up, they will fall faster
        if (jumpWasReleased)
        {
            jumpWasReleased = false;
            _playerVelocity.y *= 0.5f;
        }

        // Check if princess is on top of monster and moving, If so her movement should be the monsters, plus her own input
        // if (State.IsOnMonster)
        // {
        //     _inheritedVelocityFromMonster.x = monsterRb.linearVelocity.x;
        // }

        



        // print($"Horizontal Input: {horizontal}, Player Velocity: {_playerVelocity}, Inherited Monster Velocity: {_inheritedVelocityFromMonster.x}");

        // Apply the new velocity
        rb.linearVelocity = _playerVelocity;


    // Reset certain variables after applying movement
        _lastHorizontalInput = horizontal;
        if(_justLeftMonster)
        {
            _justLeftMonster = false;
        }
    }

    #region Collision Handler



    // MARK:Enter
    void OnCollisionEnter2D(Collision2D collision)
        {
            var (princessFootCollider, 
            tilemapCollider, 
            headPlatformCollider, 
            crushCollider) = CollectCollisionRefs(collision);
    
    
            // print(GenerateObjectsPresentInCollisionDebugMessage("Princess Collision Enter Check:", princessFootCollider, tilemapCollider, headPlatformCollider, crushCollider));
    
    
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

                _inheritedVelocityFromMonster = Vector2.zero;
                Debug.Log("Princess landed on Tilemap");
            }
            else if (headPlatformCollider && princessFootCollider && collision.GetContact(0).normal.y > 0.5f)
            {
                State.IsGrounded = true;
                State.IsOnMonster = true;
                State.IsJumping = false;
                State.IsFalling = false;
                _inheritedVelocityFromMonster = Vector2.zero;
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
    
            // string debugMsg = GenerateObjectsPresentInCollisionDebugMessage("Princess Stay Collision:", tilemapCollider, headPlatformCollider, princessFootCollider);
            // print(debugMsg);
    
            // if (tilemapCollider && princessFootCollider && collision.GetContact(0).normal.y > 0.5f)
            // {
            //     State.IsGrounded = true;
            //     State.IsJumping = false;
            //     State.IsFalling = false;
                
            // }
            // else if (headPlatformCollider && princessFootCollider && collision.GetContact(0).normal.y > 0.5f)
            // {
            //     State.IsGrounded = true;
            //     State.IsOnMonster = true;
            //     State.IsJumping = false;
            //     State.IsFalling = false;
            // }
        }
    
// MARK:Exit

        void OnCollisionExit2D(Collision2D collision)
        {
            var (princessFootCollider, 
            tilemapCollider, 
            headPlatformCollider, 
            crushCollider) = CollectCollisionRefs(collision);
    
            // string debugMsg = GenerateObjectsPresentInCollisionDebugMessage("Princess Collision Exit Check: ", tilemapCollider, headPlatformCollider, princessFootCollider);
            // print(debugMsg);
    
            if (tilemapCollider && princessFootCollider)
            {
                State.IsGrounded = false;
            }
            else if (headPlatformCollider && princessFootCollider)
            {
                State.IsGrounded = false;
                State.IsOnMonster = false;

                _justLeftMonster = true;
                _inheritedVelocityFromMonster = Vector2.zero;
            }
            
        }
    #endregion


    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Finish"))
        {
            EventHandler.WinGame();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Resets the coyote time, allowing the princess to jump again after a short delay.
    /// </summary>
    private System.Collections.IEnumerator ResetCoyoteTime()
        {
            yield return new WaitForSeconds(coyoteTime);
            print("Coyote time reset, princess can jump again");
            jumpPressed = false;
        }

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

[System.Serializable]
public class PrincessSprites
{
    public Sprite leftOrRightSprite;
    public Sprite notMovingSprite;
}
