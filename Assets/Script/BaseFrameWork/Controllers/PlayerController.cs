using Assets.Script.Player.States;
using Assets.Script.Player.VFX;
using Assets.Script.SaveData;
using NUnit.Framework;
using Player_State;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
public class PlayerController : MonoBehaviour
{
    
    [Header("Input")]
    [SerializeField] InputAction moveAction;
    [SerializeField] InputAction Jump;
    [SerializeField] InputAction Dash;
    [SerializeField] InputAction WallClimb;
    [SerializeField] float buffertime = 0.05f;
    [Header("Movement Settings")]
    [SerializeField] float movementSpeed = 10;
    [SerializeField] float jumpForce = 10;
    [SerializeField] float BaseGravityScale = 3;
    [Header("Wall Climb Settings")]
    [SerializeField] float maxClimbTime = 1.2f;   // tClimb
    [SerializeField] float wallClimbCooldown = 0.5f; // t
    [SerializeField] float wallJumpForce = 2f;
    [SerializeField] float wallClimbSpeed = 3f; // t?c d? leo tu?ng khi gi? phím Z
    [Header("Foot and Hand Object")]
    [SerializeField] Transform footPosition;
    [SerializeField] Transform handPosition;
    [Header("Hair")]
    [SerializeField] Sinx.HairMovement hairMovement;


    // private fields
    PlayerData data;
    bool isDeathed = false;
    float wallCooldownTimer;
    PlayerState state;
    Animator animator;
    Rigidbody2D rb;
    // private component
    LandingEffect landingEffect;
    Vector2 originalScale;
    public PlayerState nextState;
    int _Direction = 1;
    float currentSpeed = 5;
    //buffer and coyote
    float jumpbuffer = 0;
    float coyotetime = 0;
    float jumpcount = 0;
    int maxDashes = 1;
    int currentDashes = 1;
    
    public void SetPlayerData(PlayerData data)
    {
        this.data = data;
    }

    public PlayerData GetPlayerData()
    {
        return data;
    }


    public int Direction
    {
        get { return _Direction; }
        set {
            _Direction = value;
            transform.localScale = new Vector2(Direction * originalScale.x, originalScale.y);
        }
    }
    private void OnEnable()
    {
        moveAction.Enable();
        Jump.Enable();
        Dash.Enable();
        WallClimb.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        Jump.Disable();
        Dash.Disable();
        WallClimb.Disable();
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component is missing on PlayerController.");
        }
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing on PlayerController.");
        }
        SetState(new Idle(this));
        originalScale = transform.localScale;
        landingEffect = GetComponent<LandingEffect>();
    }

    private void Update()
    {
        if (state != null)
        {
            state.Update();
        }
        // if anyone use this for better logic refactor all the jump to seperate between ground jump and air jump
        if (jumpcount==0&&state.GetStateName()=="Fall"&&coyotetime<=0)
        {
            jumpcount = 1;
        }

        if (IsOnTheGround())
        {
            jumpcount = 0;
            coyotetime = buffertime;
        }
        else if (IsTouchingWall())
        {
            jumpcount = 0;
            coyotetime = buffertime;
        }

        if (coyotetime > 0)
        {
            coyotetime -= Time.deltaTime;
        }
        else if (coyotetime < 0)
        {
            coyotetime = 0;
        }


        if (Jump.WasPressedThisFrame())
        {
            jumpbuffer = buffertime;
        }
        if (jumpbuffer > 0)
        {
            jumpbuffer -= Time.deltaTime;
        }
        else if (jumpbuffer < 0)
        {
            jumpbuffer = 0;
        }
    }
    private void FixedUpdate()
    {
        
        if (wallCooldownTimer > 0)
            wallCooldownTimer -= Time.fixedDeltaTime;

        if (nextState != state && nextState != null && nextState.GetType() != state.GetType())//do this for only one enter exit once per frame
        {
            state.Exit();
            nextState.prevState = state;
            state = nextState;
            state.Enter();
        }
        else if(nextState==null)
        {
            Debug.Log("Next state is null");
        }
        if (state != null)
        {
            state.FixedUpdate();
        }
        
        if(IsOnTheGround())
        {
           ResetDash();
           maxDashes = 1; // Reset to single dash when grounded
        }

    }

    public void SetState(PlayerState newState)
    {
        if (state != null)
        {
            nextState = newState;
        }
        else//the first state
        {
            state = newState;
            state.Enter();
        }
    }

    public PlayerState GetState()
    {
        return state;
    }
    public Animator GetAnimator()
    {
        return animator;
    }
    // Can Aplly Control Hear Or In The State
    public bool HandleMovement()
    {
        Vector2 inputVector = moveAction.ReadValue<Vector2>();
        if (inputVector.x != 0)
        {
            Direction = inputVector.x > 0 ? 1 : -1;
            currentSpeed = movementSpeed;
            rb.linearVelocity=new Vector2(currentSpeed*Direction,rb.linearVelocity.y);
            return true;
        }
        else
        {
            rb.linearVelocity=new Vector2(0, rb.linearVelocity.y);
            currentSpeed = 0;
        }
        return false;
    }

    // if anyone use this for better logic refactor all the jump to seperate between ground jump and air jump
    public bool HandleJump()
    {  
        if (jumpbuffer>0&&jumpcount<2)
        {
            jumpcount++;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            SetState(new Jump(this));
            jumpbuffer = 0;
            return true;
        }
        return false;
    }

    public bool IsOnTheGround()
    {
        if(footPosition==null)
        {
            Debug.LogError("Foot Position is null in IsOnTheGround");
            return false;
        }
        return Physics2D.OverlapCircle(footPosition.position, 0.1f, 1 << LayerMask.NameToLayer("Ground"));
    }

    public bool IsTouchingWall()
    {
        return Physics2D.OverlapCircle(handPosition.position, 0.1f, 1 << LayerMask.NameToLayer("Ground"));
    }

    public Vector2 GetObjectVelocity()
    {
        return rb.linearVelocity;
    }

    public void SetObjectVelocity(float x,float y)
    {
        rb.linearVelocity = new Vector2(x, y);
    }
    
    public void HandleDash()
    {
        if(Dash.IsPressed()&&IsCanDash())
        {
            rb.linearVelocity = new Vector2(Direction * movementSpeed * 2, rb.linearVelocity.y);
            SetState(new Dash(this));
        }
    }

    public void SpawnLandingEffect()
    {

        landingEffect.SpawnLandingEffect(footPosition.position);
    }

    public float dashSpeed
    {
        get { return movementSpeed * 2; }
    }

    public float MaxClimbTime { get => maxClimbTime;}
    public float WallClimbCooldown { get => wallClimbCooldown; }

    public float WallJumpForce { get => wallJumpForce; }
    public float WallClimbSpeed { get => wallClimbSpeed; }

    // Ki?m tra phím Z (leo tu?ng) có dang du?c gi? không
    public bool IsClimbKeyPressed()
    {
        return WallClimb.IsPressed();
    }    

    public float GetMoveInputX()
    {
        return moveAction.ReadValue<Vector2>().x;
    }

    public bool IsPressingTowardWall()
    {
        float inputX = GetMoveInputX();

        if (inputX == 0) return false;

        // dang nh?n v? phía dang quay m?t
        return Mathf.Sign(inputX) == Direction;
    }

    public bool CanClimbWall()
    {
        return wallCooldownTimer <= 0;
    }

    public void StartWallCooldown()
    {
        wallCooldownTimer = wallClimbCooldown;
    }


    public void SetPlayerPosition(Vector2 newPos)
    {
        transform.position = newPos;
    }

    public void DisableInput()
    {
        rb.angularVelocity = 0;
        moveAction.Disable();
        Jump.Disable();
        Dash.Disable();
        WallClimb.Disable();
    }

    public void EnableInput()
    {
        moveAction.Enable();
        Jump.Enable();
        Dash.Enable();
        WallClimb.Enable();
    }
    public void Death()
    {
        if(isDeathed)
        {
            return;
        }
        isDeathed = true;
        GameManager.GetInstance().OnPlayerDeath();
    }

    public Vector2 GetMoveVector()
    {
        return moveAction.ReadValue<Vector2>();
    }

    public bool IsCanDash()
    {
        return state.GetStateName() != "Dash" && HasDash();
    }

    public void TurnOffDashAble()
    {
        UseDash();
    }

    public float GetBaseGravityScale()
    {
        return BaseGravityScale;
    }

    /// <summary>
    /// Reset dash count to max dashes (called by springs, dash crystals, landing)
    /// </summary>
    public void ResetDash()
    {
        currentDashes = maxDashes;
    }

    /// <summary>
    /// Set the maximum number of dashes (for double dash crystals)
    /// </summary>
    public void SetMaxDashes(int max)
    {
        maxDashes = max;
        currentDashes = max;
    }

    /// <summary>
    /// Get current available dashes
    /// </summary>
    public int GetCurrentDashes()
    {
        return currentDashes;
    }

    /// <summary>
    /// Use one dash (returns true if dash was available)
    /// </summary>
    public bool UseDash()
    {
        if (currentDashes > 0)
        {
            currentDashes--;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if player has dashes available
    /// </summary>
    public bool HasDash()
    {
        return currentDashes > 0;
    }

    /// <summary>
    /// Check if player is grounded (alias for IsOnTheGround)
    /// </summary>
    public bool GetIsGrounded()
    {
        return IsOnTheGround();
    }
}

