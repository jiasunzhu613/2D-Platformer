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
    private float RunAccel;
    public float RunAccelRate = 0.6f;
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
    private float DashAccel;
    public float DashAccelRate;
    public float DashTime = 0.2f;
    private Vector2 dashTarget;
    private Vector2 dashAccel;
    private float dashTimer;
    private float _GravityScale = 1;
    private bool canDash = false;
    private int _flipX;
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
    public SpriteRenderer _spriteRenderer;
    public Animator _animator;
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
        // _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //First jump
        FirstJumpForce = 2 * FirstJumpHeight / JumpTimeToPeak;
        float gravityNeeded = -2 * FirstJumpHeight / (JumpTimeToPeak * JumpTimeToPeak);
        _GravityScale = gravityNeeded / -9.81f;
        RunAccel = Speed * RunAccelRate;
        DashAccel = DashSpeed * DashAccelRate;
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
        _flipX = _spriteRenderer.flipX ? -1 : 1;
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
        UpdateAnimationParameters();

        // Debug.Log(onGround);
        // Debug.Log(onWall);
        // Debug.Log(Math.Sign(0));
        
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
            float dfull = 5 * Speed;
            float dhalf = dfull * 0.70710678118f; //multiply by component
            Vector2 temp = Vector2.zero;
            // set initial dash speeds (we will go from high to low, decelerate throughout dash)
            // Debug.Log(_moveInput.y);
            if (_moveInput != Vector2.zero)
            {
                if (_moveInput.x != 0 && _moveInput.y != 0)
                {
                    temp.Set(_moveInput.x * dhalf, _moveInput.y * dhalf);
                    Debug.Log("45 degg entered");
                }else if (_moveInput.x != 0)
                {
                    temp.Set(_moveInput.x * dfull, 0);
                    Debug.Log("horizontal entered");
                }
                else
                {
                    temp.Set(0, _moveInput.y * dfull);
                    Debug.Log("vertical entered");
                } 
            }
            else
            {
                temp.Set(_flipX * dfull, 0);
            }
            Debug.Log(temp);
            Debug.Log("vertical math sign is " + Math.Sign(temp.y));

            
            //set dashTarget and dashAccel
            dashTarget.x = 2 * Speed * Math.Sign(temp.x);
            dashTarget.y = 2 * Speed * Math.Sign(temp.y);
            Debug.Log(dashTarget);
            dashAccel.x = Math.Abs(DashAccelRate * dashTarget.x);
            dashAccel.y = Math.Abs(DashAccelRate * dashTarget.y);
            // Debug.Log(dashTarget.y);
            if (rb.velocity.y < 0)
            {
                dashTarget.y *= 0.75f;
            }
            if (temp.x != 0)
            {
                dashAccel.x *= 0.70710678118f;
            }
            if (temp.y != 0)
            {
                dashAccel.y *= 0.70710678118f;
            }

            rb.velocity = temp;
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

    //TODO: MAKE DASH FEEL GOOD
    //TODO: change dash to dash into direction of player movement, or if there is no input, dash into facing direction
    //TODO: add dash particles (fart propel LMFAO)
    void Dash()
    {
        //Should prolly use forces
        Vector2 newVector = rb.velocity;
        newVector.x = approach(newVector.x,  dashTarget.x, dashAccel.x);
        newVector.y = approach(newVector.y,  dashTarget.y, dashAccel.y);
        rb.velocity = newVector;
    }

    void SetGravityScale(float gravity)
    {
        rb.gravityScale = gravity;
    }
    void MoveCharacter()
    {
        //Currently just setting velocity
        // Vector2 newVelocity = new Vector2(_moveInput.x * Speed, rb.velocity.y); 
        // // for some reason rb.velocity.y gets reset?
        // if (wantsToWallClimb)
        //     newVelocity.y = _moveInput.y * Speed;
        // else if (onWall && newVelocity.x != 0)
        //     newVelocity.y /= 10f;
        // rb.velocity = newVelocity;
        
        //New 
        //TODO: maybe also implement custom friction later? (not sure if needed tho)
        Vector2 newVelocity = new Vector2(rb.velocity.x, rb.velocity.y);
        newVelocity.x = approach(rb.velocity.x, _moveInput.x * Speed, RunAccel);
        if (wantsToWallClimb)
            newVelocity.y = approach(rb.velocity.y, _moveInput.y * Speed, RunAccel);
        else if (onWall && newVelocity.x != 0) // TODO: add custom scratching wall/wall sliding animation
            newVelocity.y /= 10;
        rb.velocity = newVelocity;
    }

    private float approach(float val, float target, float amount)
    {
        return val > target ? Math.Max(val - amount, target) : Math.Min(val + amount, target);
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
        wallCheckLeft.x -= collider.bounds.size.x / 2;
        wallCheckRight.x += collider.bounds.size.x / 2;
        
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

    private void UpdateAnimationParameters()
    {
        Flip(rb.velocity.x);
        _animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        _animator.SetFloat("VerticalVelocity", rb.velocity.y);
        _animator.SetFloat("VerticalSpeed", Mathf.Abs(rb.velocity.y));
        _animator.SetBool("OnGround", onGround);
        _animator.SetBool("OnWall", onWall);
        _animator.SetBool("WantsToWallClimb", wantsToWallClimb);
    }

    private void Flip(float x)
    {
        if ((x < -0.1f && !_spriteRenderer.flipX) || (x > 0.1f && _spriteRenderer.flipX))
        {
            _spriteRenderer.flipX = !_spriteRenderer.flipX;
        }
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var pos = transform.position - new Vector3(0, _spriteRenderer.bounds.size.y / 2);
        Gizmos.DrawSphere(pos, 0.2f);
        var pos2 = transform.position - new Vector3(collider.bounds.size.x / 2,0);
        Gizmos.DrawSphere(pos2, 0.2f);
        var pos3 = transform.position + new Vector3(collider.bounds.size.x / 2,0);
        Gizmos.DrawSphere(pos3, 0.2f);
    }
}
}
