using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(SpriteRenderer))]
public class Monster : PCCharacter
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
    private PrincessState princessState;

    // Movement variables
    public float speed;
    public float jumpForce;
    public float carryingJumpForceMult;
    private float horizontal = 0;
    private bool jumpPressed = false;

    void Start()
    {
        // Initialize the state using the StateFactory
        State = (MonsterState)StateFactory.InitState(this.gameObject);

        // Set the references to the components to the actual components
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // get the princess game object
        Princess = GameObject.Find("Princess");
        princessState = Princess.GetComponent<Princess>().State;

        // This makes it so the Monster doesnt fall over (rotate on the z axis)
        rb.freezeRotation = true;
    }

    // Update is called once per frame, This is for detection of inputs from users
    // and logic that doesn't involve physics
    void Update()
    {
        if (Princess == null || princessState == null) Start();
        State.IsGrounded = IsGrounded();

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
        CheckIfCrush();
    }

    // FixedUpdate is called at a fixed interval and is independent of frame rate. 
    // Put physics code here so the physics simulation is smooth regardless of framerate.
    void FixedUpdate()
    {
        bool princessOnTop = princessState.IsOnMonster;
        print("Princess on top: " + princessOnTop);
        /* bool princessOnTop = false;
        if (Princess != null)
        {
            // Define a small box above the monster to check for the princess, this is for the extra jump force
            Vector2 boxCenter = new(transform.position.x, transform.position.y + 1f);
            Vector2 boxSize = new(1.2f, 0.5f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0);
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == Princess)
                {
                    princessOnTop = true;
                    break;
                }
            }
        } */

        // Due to the princess weighing down the monster, we increase the jump force when she is on top
        // so the monter jump stays the same
        float currentJumpForce = princessOnTop ? jumpForce * carryingJumpForceMult : jumpForce * 1f;

        // Apply horizontal movement
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);

        if (jumpPressed)
        {
            jumpPressed = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
        }

    }

    // Check if the princess is crushed by the monster
    private void CheckIfCrush()
    {
        // Check for collision with the crush collider
        Collider2D[] colliders = Physics2D.OverlapBoxAll(new Vector2(transform.position.x, transform.position.y - 0.9f), new Vector2(1.5f, 0.5f), 0);
        foreach (Collider2D collider in colliders)
        {
            if (collider.name == "CrushCollider" && rb.linearVelocity.y < -8.0f)
            {
                collider.GetComponentInParent<Princess>().Death();
            }
        }
    }


}
