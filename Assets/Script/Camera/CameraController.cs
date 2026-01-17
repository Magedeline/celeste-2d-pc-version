using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Class which handles camera movement
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // The camera being controlled by this script
    [HideInInspector] private Camera playerCamera = null;

    [Header("GameObject References")]
    [Tooltip("The target to follow with this camera")]
    public Transform target = null;

    /// <summary>
    /// Enum to determine camera movement styles
    /// </summary>
    public enum CameraStyles
    {
        Locked,
        Overhead,
        DistanceFollow,
        OffsetFollow,
        BetweenTargetAndMouse,
        RoomBounds  // Celeste-style: camera stays within room bounds
    }

    [Header("CameraMovement")]
    [Tooltip("The way this camera moves:\n" +
        "\tLocked: Camera does not move\n" +
        "\tOverhead: Camera stays over that target\n" +
        "\tDistanceFollow: Camera stays within [Max distance From Target] away from the target.\n" +
        "\tOffsetFollow: Camera follows the target at an offset" +
        "\tBetweenTargetAndMouse: Camera stays directly between the mouse position and the target position" +
        "\tRoomBounds: Celeste-style camera that stays within room boundaries")]
    public CameraStyles cameraMovementStyle = CameraStyles.Locked;

    [Tooltip("The maximum distance away from the target that the camera can move")]
    public float maxDistanceFromTarget = 5.0f;
    [Tooltip("The offset from the computed camera position to move the camera to in Offset Follow mode.")]
    public Vector2 cameraOffset = Vector2.zero;
    [Tooltip("The z coordinate to use for the camera position")]
    public float cameraZCoordinate = -10.0f;
    [Tooltip("The percentage distance between the target position and the\n" +
        "mouse position to move the camera to in BetweenTargetAndMouse camera mode.")]
    public float mouseTracking = 0.5f;

    [Header("Room Bounds Settings")]
    [Tooltip("Current room bounds for RoomBounds camera mode")]
    [SerializeField] private Rect currentRoomBounds = new Rect(0, 0, 320, 180);
    [Tooltip("Smooth transition time when switching rooms")]
    [SerializeField] private float roomTransitionTime = 0.3f;
    [Tooltip("Enable smooth follow within room bounds")]
    [SerializeField] private bool smoothFollowInRoom = true;
    [Tooltip("Smooth follow speed")]
    [SerializeField] private float smoothFollowSpeed = 8f;

    // Room transition state
    private bool isTransitioningRooms = false;
    private Vector3 transitionStartPos;
    private Vector3 transitionEndPos;
    private float transitionTimer = 0f;

    [Header("Input Actions & Controls")]
    [Tooltip("The input action(s) that map to where the camera looks")]
    public InputAction lookAction;

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is enabled
    /// </summary>
    void OnEnable()
    {
        lookAction.Enable();
    }

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is disabled
    /// </summary>
    void OnDisable()
    {
        lookAction.Disable();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once before the first update
    /// Input: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    void Start()
    {
        InitilalSetup();
    }

    /// <summary>
    /// Description:
    /// Handles the initial setup of this script and its required components
    /// Input:
    /// none
    /// Returns:
    /// void
    /// </summary>
    void InitilalSetup()
    {
        playerCamera = GetComponent<Camera>();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function that is called every frame
    /// Input: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    void Update()
    {
        if (target==null&& GameManager.GetInstance().GetPlayerController()!=null)
        {
            target = GameManager.GetInstance().GetPlayerController().transform;
        }
        
        // Handle room transition animation
        if (isTransitioningRooms)
        {
            UpdateRoomTransition();
            return;
        }
        
        SetCameraPosition();
    }

    /// <summary>
    /// Updates the camera position during room transitions
    /// </summary>
    private void UpdateRoomTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, transitionTimer / roomTransitionTime);
        
        Vector3 newPos = Vector3.Lerp(transitionStartPos, transitionEndPos, t);
        newPos.z = cameraZCoordinate;
        transform.position = newPos;
        
        if (transitionTimer >= roomTransitionTime)
        {
            isTransitioningRooms = false;
            transform.position = transitionEndPos;
        }
    }

    /// <summary>
    /// Description:
    /// Sets the camera's position according to the settings
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    private void SetCameraPosition()
    {
        if (target != null)
        {
            Vector3 targetPosition = GetTargetPosition();
            Vector3 mousePosition = GetPlayerMousePosition();
            Vector3 desiredCameraPosition = ComputeCameraPosition(targetPosition, mousePosition);

            transform.position = desiredCameraPosition;
        }      
    }

    /// <summary>
    /// Description:
    /// Gets the follow target's position
    /// Input: 
    /// none
    /// Returns: 
    /// Vector3
    /// </summary>
    /// <returns>Vector3: The position of the target assigned to this camera controller.</returns>
    public Vector3 GetTargetPosition()
    {
        if (target != null)
        {
            return target.position;
        }
        return transform.position;
    }

    /// <summary>
    /// Description:
    /// Finds and returns the mouse position
    /// Input: 
    /// none
    /// Returns: 
    /// Vector3
    /// </summary>
    /// <returns>Vector3: The position of the player's mouse in world coordinates</returns>
    public Vector3 GetPlayerMousePosition()
    {
        return playerCamera.ScreenToWorldPoint(lookAction.ReadValue<Vector2>());
    }

    /// <summary>
    /// Description:
    /// Takes the target's position and mouse position, and returns the desired position of the camera
    /// Input: 
    /// Vector3 targetPosition, Vector3 offsetPosition
    /// Returns:
    /// Vector3
    /// </summary>
    /// <param name="targetPosition"> The position of the target the camera is following. </param>
    /// <param name="mousePosition"> The position of the mouse in world space used to determine distance from the target. </param>
    /// <returns>Vector3: The position the camera should be at</returns>
    public Vector3 ComputeCameraPosition(Vector3 targetPosition, Vector3 mousePosition)
    {
        Vector3 result = Vector3.zero;
        switch (cameraMovementStyle)
        {
            case CameraStyles.Locked:
                result = transform.position;
                break;
            case CameraStyles.Overhead:
                result = targetPosition;
                break;
            case CameraStyles.DistanceFollow:
                result = transform.position;
                if ((targetPosition - result).magnitude > maxDistanceFromTarget)
                {
                    result = targetPosition + (result - targetPosition).normalized * maxDistanceFromTarget;
                }
                break;
            case CameraStyles.OffsetFollow:
                result = targetPosition + (Vector3)cameraOffset;
                break;
            case CameraStyles.BetweenTargetAndMouse:
                Vector3 desiredPosition = Vector3.Lerp(targetPosition, mousePosition, mouseTracking);
                Vector3 difference = desiredPosition - targetPosition;
                difference = Vector3.ClampMagnitude(difference, maxDistanceFromTarget);
                result = targetPosition + difference;
                break;
            case CameraStyles.RoomBounds:
                result = ComputeRoomBoundsPosition(targetPosition);
                break;
        }
        result.z = cameraZCoordinate;
        return result;
    }

    /// <summary>
    /// Computes camera position for RoomBounds mode
    /// Camera follows target but stays clamped within room boundaries
    /// </summary>
    private Vector3 ComputeRoomBoundsPosition(Vector3 targetPosition)
    {
        if (playerCamera == null) return targetPosition;

        // Get camera viewport size in world units
        float cameraHeight = playerCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * playerCamera.aspect;

        // Calculate the allowed camera position range within the room
        float minX = currentRoomBounds.xMin + cameraWidth / 2f;
        float maxX = currentRoomBounds.xMax - cameraWidth / 2f;
        float minY = currentRoomBounds.yMin + cameraHeight / 2f;
        float maxY = currentRoomBounds.yMax - cameraHeight / 2f;

        // Handle rooms smaller than camera view
        if (minX > maxX)
        {
            minX = maxX = currentRoomBounds.center.x;
        }
        if (minY > maxY)
        {
            minY = maxY = currentRoomBounds.center.y;
        }

        // Target position is where we want the camera to be (following player)
        Vector3 desiredPos = targetPosition;

        // Clamp to room bounds
        desiredPos.x = Mathf.Clamp(desiredPos.x, minX, maxX);
        desiredPos.y = Mathf.Clamp(desiredPos.y, minY, maxY);

        // Smooth follow or instant
        if (smoothFollowInRoom)
        {
            return Vector3.Lerp(transform.position, desiredPos, smoothFollowSpeed * Time.deltaTime);
        }
        
        return desiredPos;
    }

    /// <summary>
    /// Set the current room bounds for RoomBounds camera mode
    /// </summary>
    public void SetRoomBounds(Rect bounds, bool smoothTransition = true)
    {
        Rect previousBounds = currentRoomBounds;
        currentRoomBounds = bounds;

        if (smoothTransition && cameraMovementStyle == CameraStyles.RoomBounds)
        {
            // Start smooth transition to new room
            StartRoomTransition();
        }
    }

    /// <summary>
    /// Start a smooth camera transition to the new room
    /// </summary>
    private void StartRoomTransition()
    {
        transitionStartPos = transform.position;
        
        // Calculate target position in new room
        Vector3 targetPos = target != null ? target.position : currentRoomBounds.center;
        transitionEndPos = ComputeRoomBoundsPosition(targetPos);
        transitionEndPos.z = cameraZCoordinate;
        
        transitionTimer = 0f;
        isTransitioningRooms = true;
    }

    /// <summary>
    /// Instantly snap camera to room center (no transition)
    /// </summary>
    public void SnapToRoomCenter()
    {
        Vector3 newPos = currentRoomBounds.center;
        newPos.z = cameraZCoordinate;
        transform.position = newPos;
        isTransitioningRooms = false;
    }

    /// <summary>
    /// Get current room bounds
    /// </summary>
    public Rect GetRoomBounds()
    {
        return currentRoomBounds;
    }

    /// <summary>
    /// Check if camera is currently transitioning between rooms
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioningRooms;
    }
}
