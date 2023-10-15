using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Player
{
    //TODO: Implement state machine
   public class PlayerController : MonoBehaviour
{
    #region Movement variables
    public int FirstJumpHeight;
    public int OtherJumpHeight;
    public float JumpTimeToPeak;
    private float OtherJumpTimeToPeak;
    public float Speed;
    private float FirstJumpForce;
    private float OtherJumpForce;
    public int NumberOfJumps = 2;
    private int jumpsRemaining;
    public int _ExtraGravityFactor;
    public float JumpGraceTime = 0.2f;
    public float JumpBufferTime = 0.2f;
    private float jumpGraceTimer;
    private float wallJumpGraceTimer;
    private float jumpBufferTimer;
    private float wallJumpBufferTimer;
    public float DashSpeed;
    public float DashTime = 0.5f;
    private float dashTimer;
    private float _GravityScale = 1;
    private bool canDash = false;
    #endregion

    private int direction;
    private bool isJumping;
    private bool wantsToOtherJump;
    private bool wantsToWallClimb;
    private Rigidbody2D rb;
    private BoxCollider2D collider;
    private LayerMask groundLayer;
    private Vector2 _moveInput;
    private bool onGround;
    private bool onWall;
    private SpriteRenderer _spriteRenderer;
    // public Transform groundCheck;
    
    #region Finding Contacts
    private float minDotValue;
    private ContactFilter2D _groundFilter;
    private ContactPoint2D[] _contacts;
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<BoxCollider2D>();  
        groundLayer = LayerMask.GetMask("GroundLayer");
        _groundFilter.layerMask = LayerMask.GetMask("GroundLayer");
        _contacts = new ContactPoint2D[16];
        minDotValue = 0.5f;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //First jump
        FirstJumpForce = 2 * FirstJumpHeight / JumpTimeToPeak;
        float gravityNeeded = -2 * FirstJumpHeight / (JumpTimeToPeak * JumpTimeToPeak);
        _GravityScale = gravityNeeded / -9.81f;
        // Debug.Log(_GravityScale);
        // Debug.Log(FirstJumpForce);
        
        //Other jumps
        OtherJumpTimeToPeak = Mathf.Sqrt(-2 * OtherJumpHeight / gravityNeeded);
        OtherJumpForce = 2 * OtherJumpHeight / OtherJumpTimeToPeak;
        // Debug.Log(OtherJumpForce);
    }
    
    /*
     * TODO:
     * - Make variable height jumping
     */
    // Update is called once per frame
    void Update()
    {
        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");
        // //Get Ground
        // if (rb.velocity.y >= 0f)
        // {
        //     // research about better ways to check grounded
        //     onGround = isPlayerGrounded();
        //     // if (onGround)
        //     //     isJumping = false;
        //     // add checkingg onground true and jumpBufferTimer > 0 -> jump 
        // }
        onGround = isPlayerGrounded();
        onWall = isPlayerOnWall();


        Debug.Log(onGround);
        // Debug.Log(onWall);
        
        if (onGround)
        {
            isJumping = false;
            jumpGraceTimer = JumpGraceTime;
            canDash = true;
            jumpsRemaining = NumberOfJumps;
        }
        else 
        {
            jumpGraceTimer -= Time.deltaTime;
        }

        if (onWall)
        {
            isJumping = false;
            wallJumpGraceTimer = JumpGraceTime;
            jumpsRemaining = NumberOfJumps;
        }
        else 
        {
            wallJumpGraceTimer -= Time.deltaTime;
        }
        
        // Debug.Log(Input.GetButtonDown("Jump"));
        if (Input.GetButtonDown("Jump")) // need to change later when more states are added
        {
            // Debug.Log("Space Presseed");
            jumpBufferTimer = JumpBufferTime;
            wallJumpBufferTimer = JumpBufferTime;
            if (isJumping)
                wantsToOtherJump = true;
            else
                wantsToOtherJump = false;
        }else 
        {
            jumpBufferTimer -= Time.deltaTime;
            wallJumpBufferTimer -= Time.deltaTime;
        }
        // Variable jump
        if (Input.GetButtonUp("Jump"))
        {
            // Debug.Log("JUMP BUTTON UP");
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            SetGravityScale(_GravityScale * _ExtraGravityFactor);
        }

        if (Input.GetKey(KeyCode.L) && onWall)
        {
            wantsToWallClimb = true;
        }
        else
        {
            wantsToWallClimb = false;
        }
        // Debug.Log(wantsToWallClimb);

        if (Input.GetKeyDown(KeyCode.K) && canDash)
        {
            dashTimer = DashTime;
            canDash = false;
        }else 
        {
            dashTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {   
        // UpdateContacts();
        direction = rb.velocity.x < 0 ? -1 : 1;
        MoveCharacter();
        //Jump
        // Debug.Log(jumpBufferTimer);
        if ((jumpGraceTimer > 0f && jumpBufferTimer > 0f) || (wallJumpGraceTimer > 0f && wallJumpBufferTimer > 0f))
        {
            // Debug.Log("JUmped");
            // isJumping = true;
            Jump(FirstJumpForce);
            jumpGraceTimer = 0f;
            wallJumpGraceTimer = 0f;
            jumpsRemaining--;
            isJumping = true;
        }
        
        if (wantsToOtherJump && 0 < jumpsRemaining && jumpsRemaining <= NumberOfJumps - 1)
        {
            Debug.Log("Other jump entered");
            Jump(OtherJumpForce);
            jumpsRemaining--;
        }

        // TODO: fix dash into wall and re-bounding of it, set velocity as 0 when hit wall
        if (dashTimer > 0f)
        {
            Dash();
        }
        
        // Set gravity scale
        if (wantsToWallClimb)
        {
            SetGravityScale(0);
        }else if (rb.velocity.y < 0f)
        {
            SetGravityScale(_GravityScale * _ExtraGravityFactor);
        }
        else
        {
            SetGravityScale(_GravityScale);
        }
    }
    
    //TODO: Optimize jump feel, rn jump feels very airy
    void Jump(float jumpForce)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    //TODO: change dash to dash into direction of player movement, or if there is no input, dash into facing direction
    void Dash()
    {
        rb.velocity = new Vector2(direction*DashSpeed, 0);
    }

    void SetGravityScale(float gravity)
    {
        rb.gravityScale = gravity;
    }
    void MoveCharacter()
    {
        Vector2 newVelocity = new Vector2(_moveInput.x * Speed, rb.velocity.y);
        if (wantsToWallClimb)
            newVelocity.y = _moveInput.y * Speed;
        else if (onWall && newVelocity.x != 0)
            newVelocity.y /= 10;

        rb.velocity = newVelocity;
    }
    
    private bool isPlayerGrounded()
    {
        Vector2 groundCheck = transform.position;
        groundCheck.y -= _spriteRenderer.bounds.size.y / 2;
        // Debug.Log(groundCheck);
        // Debug.DrawRay(groundCheck, Vector2.down, Color.red);

        
        Collider2D[] groundCollisions = Physics2D.OverlapCircleAll(groundCheck, 0.2f, groundLayer);
        foreach (Collider2D collision in groundCollisions)
        {
            // Debug.Log(collision.gameObject);
            if (collision.gameObject != gameObject)
            {
                return true;
            }
        }
        return false;
    }
    
    private bool isPlayerOnWall()
    {
        Vector2 wallCheckLeft = transform.position;
        Vector2 wallCheckRight = transform.position;
        wallCheckLeft.x -= _spriteRenderer.bounds.size.x / 2;
        wallCheckRight.x += _spriteRenderer.bounds.size.x / 2;
        
        Collider2D[] wallCollisionsLeft = Physics2D.OverlapCircleAll(wallCheckLeft, 0.2f, groundLayer);
        foreach (Collider2D collision in wallCollisionsLeft)
        {
            // Debug.Log(collision.gameObject);
            if (collision.gameObject != gameObject)
            {
                return true;
            }
        }
        
        Collider2D[] wallCollisionsRight = Physics2D.OverlapCircleAll(wallCheckRight, 0.2f, groundLayer);
        foreach (Collider2D collision in wallCollisionsRight)
        {
            // Debug.Log(collision.gameObject);
            if (collision.gameObject != gameObject)
            {
                return true;
            }
        }
        return false;
    }

    /*
     Main ideas: 
     - try using dot product to help finding contacts
     
     Valuable resources:
     - https://docs.unity3d.com/ScriptReference/ContactPoint.html
     - https://docs.unity3d.com/ScriptReference/ContactFilter2D.html
        
    Exploration:
    - Explore/research about OnEnter() and OnStay() functions as a potentially more efficient alternative
    */
    private void UpdateContacts()
    {
        //Syntax for C# array creation, no pointers I believe
        int numberOfContacts = rb.GetContacts(_groundFilter, _contacts);
        // Syntax for C# "foreach" loop
        for (int i = 0; i < numberOfContacts; i++)
        {
            // Dot returns scalar value
            float projection = Vector2.Dot(Vector2.up, _contacts[i].normal);
            if (projection > minDotValue)
            {
                onGround = true;
            }
            // if (projection >= -minDotValue)
        } 
        _contacts = new ContactPoint2D[16];
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var pos = transform.position - new Vector3(0, _spriteRenderer.bounds.size.y / 2, 0);
        Gizmos.DrawSphere(pos, 0.2f);
    }
}
}