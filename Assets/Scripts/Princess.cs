using System;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(SpriteRenderer))]
public class Princess : PCCharacter
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

    // Movement variables
    private int horizontal = 0;
    private bool jumpPressed = false;
    public float speed;
    public float jumpForce;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize the state using the StateFactory
        State = (PrincessState)StateFactory.InitState(this.gameObject);

        // Set the references to the components to the actual components
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // This makes it so the princess doesnt fall over (rotate on the z axis)
        rb.freezeRotation = true;
    }

    // Update is called once per frame, This is for detection of inputs from users
    // and logic that doesn't involve physics
    void Update()
    {
        State.IsGrounded = IsGrounded();
        State.IsFalling = rb.linearVelocity.y < 0 && !State.IsGrounded;
        // We are only setting movement variables here because physics has to happen in a FixedUpdate
        // so we use the variables we set here in FixedUpdate to find out how to move the character

        // We use a horizontal variable to store the direction of movement inputed by the user
        if (Input.GetKey(KeyCode.A)) { horizontal = -1; State.IsMoving = true; }
        else if (Input.GetKey(KeyCode.D)) { horizontal = 1; State.IsMoving = true; }
        else { horizontal = 0; State.IsMoving = false; }

        // Jumping
        if (Input.GetKeyDown(KeyCode.W) && State.IsGrounded) { jumpPressed = true; State.IsJumping = true; }
        else { State.IsJumping = false; }


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
        State.IsOnMonster = IsOnMonster();

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
    // to check if the princess is on top of the monster
    private bool IsOnMonster()
    {
        // Check if there's a monster below the princess
        // Use -1 as layerMask to check all layers regardless of collision matrix
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            new Vector2(transform.position.x, transform.position.y - 0.6f),
            new Vector2(1f, 0.05f),
            0f, -1
        );
        // check each collider hit from the overlap circle to see if it is the head platform collider
        // In which case we return true
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.name == "HeadPlatformCollider")
            {
                return true;
            }
        }
        return false;
    }
    
}
