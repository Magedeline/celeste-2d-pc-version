using UnityEngine;
using LDtkUnity;
using System.Collections.Generic;

/// <summary>
/// Manages room transitions in a Celeste-like GridVania level structure.
/// Works with LDtk levels as rooms, handling player transitions between adjacent rooms.
/// </summary>
public class RoomManager : MonoBehaviour
{
    private static RoomManager __instance;

    [Header("Room Settings")]
    [Tooltip("The LDtk project component containing all rooms")]
    [SerializeField] private LDtkComponentProject ldtkProject;
    
    [Tooltip("Time for camera transition between rooms")]
    [SerializeField] private float roomTransitionDuration = 0.3f;
    
    [Tooltip("Freeze player during room transition")]
    [SerializeField] private bool freezePlayerDuringTransition = true;

    [Header("Transition Style")]
    [SerializeField] private RoomTransitionStyle transitionStyle = RoomTransitionStyle.InstantSnap;

    public enum RoomTransitionStyle
    {
        InstantSnap,      // Classic Celeste - instant camera snap
        SmoothPan,        // Smooth camera pan to new room
        FadeTransition    // Fade to black, then new room
    }

    [Header("Debug")]
    [SerializeField] private bool showRoomBounds = true;
    [SerializeField] private Color currentRoomColor = Color.green;
    [SerializeField] private Color neighborRoomColor = Color.yellow;

    // Current room tracking
    private LDtkComponentLevel currentRoom;
    private List<LDtkComponentLevel> allRooms = new List<LDtkComponentLevel>();
    private bool isTransitioning = false;

    // Events
    public System.Action<LDtkComponentLevel, LDtkComponentLevel> OnRoomChanged;
    public System.Action OnRoomTransitionStart;
    public System.Action OnRoomTransitionEnd;

    public static RoomManager Instance
    {
        get
        {
            if (__instance == null)
            {
                __instance = FindAnyObjectByType<RoomManager>();
            }
            return __instance;
        }
    }

    public LDtkComponentLevel CurrentRoom => currentRoom;
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (__instance == null)
        {
            __instance = this;
        }
        else if (__instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeRooms();
    }

    /// <summary>
    /// Initialize all rooms from the LDtk project
    /// </summary>
    public void InitializeRooms()
    {
        allRooms.Clear();
        
        if (ldtkProject == null)
        {
            // Try to find LDtk project in scene
            ldtkProject = FindAnyObjectByType<LDtkComponentProject>();
        }

        if (ldtkProject == null)
        {
            Debug.LogWarning("RoomManager: No LDtk project found in scene.");
            return;
        }

        // Find all LDtk levels (rooms) in the project hierarchy
        LDtkComponentLevel[] levels = ldtkProject.GetComponentsInChildren<LDtkComponentLevel>();
        allRooms.AddRange(levels);

        Debug.Log($"RoomManager: Found {allRooms.Count} rooms in LDtk project.");
    }

    private void Update()
    {
        if (isTransitioning) return;

        Transform player = GetPlayerTransform();
        if (player == null) return;

        // Check if player is still in current room
        if (currentRoom != null)
        {
            Rect roomBounds = GetRoomBounds(currentRoom);
            if (!roomBounds.Contains(player.position))
            {
                // Player left current room, find new room
                CheckRoomTransition(player.position);
            }
        }
        else
        {
            // No current room, find which room player is in
            FindCurrentRoom(player.position);
        }
    }

    /// <summary>
    /// Get player transform from GameManager
    /// </summary>
    private Transform GetPlayerTransform()
    {
        PlayerController pc = GameManager.GetInstance()?.GetPlayerController();
        return pc?.transform;
    }

    /// <summary>
    /// Find which room contains the given position
    /// </summary>
    public LDtkComponentLevel FindRoomAtPosition(Vector2 position)
    {
        foreach (var room in allRooms)
        {
            Rect bounds = GetRoomBounds(room);
            if (bounds.Contains(position))
            {
                return room;
            }
        }
        return null;
    }

    /// <summary>
    /// Find and set the current room based on player position
    /// </summary>
    private void FindCurrentRoom(Vector2 playerPosition)
    {
        LDtkComponentLevel room = FindRoomAtPosition(playerPosition);
        if (room != null && room != currentRoom)
        {
            SetCurrentRoom(room);
        }
    }

    /// <summary>
    /// Check if player transitioned to a neighbor room
    /// </summary>
    private void CheckRoomTransition(Vector2 playerPosition)
    {
        // First check neighbor rooms (more efficient)
        if (currentRoom != null)
        {
            LDtkNeighbour[] neighbors = currentRoom.Neighbours;
            if (neighbors != null)
            {
                foreach (var neighbor in neighbors)
                {
                    LDtkComponentLevel neighborRoom = FindRoomByIdentifier(neighbor.LevelIid);
                    if (neighborRoom != null)
                    {
                        Rect bounds = GetRoomBounds(neighborRoom);
                        if (bounds.Contains(playerPosition))
                        {
                            TransitionToRoom(neighborRoom);
                            return;
                        }
                    }
                }
            }
        }

        // Fallback: check all rooms
        LDtkComponentLevel newRoom = FindRoomAtPosition(playerPosition);
        if (newRoom != null && newRoom != currentRoom)
        {
            TransitionToRoom(newRoom);
        }
    }

    /// <summary>
    /// Find a room by its LDtk identifier
    /// </summary>
    private LDtkComponentLevel FindRoomByIdentifier(string iid)
    {
        foreach (var room in allRooms)
        {
            if (room.Iid == iid)
            {
                return room;
            }
        }
        return null;
    }

    /// <summary>
    /// Get the world bounds of a room
    /// </summary>
    public Rect GetRoomBounds(LDtkComponentLevel room)
    {
        if (room == null) return Rect.zero;

        // LDtk provides BorderRect which is the room's bounds
        return room.BorderRect;
    }

    /// <summary>
    /// Set the current room without transition
    /// </summary>
    public void SetCurrentRoom(LDtkComponentLevel room)
    {
        LDtkComponentLevel previousRoom = currentRoom;
        currentRoom = room;

        // Update camera bounds
        UpdateCameraBounds();

        OnRoomChanged?.Invoke(previousRoom, currentRoom);
        Debug.Log($"RoomManager: Current room set to {room?.name ?? "null"}");
    }

    /// <summary>
    /// Transition to a new room with animation
    /// </summary>
    public void TransitionToRoom(LDtkComponentLevel newRoom)
    {
        if (newRoom == null || newRoom == currentRoom || isTransitioning) return;

        StartCoroutine(PerformRoomTransition(newRoom));
    }

    private System.Collections.IEnumerator PerformRoomTransition(LDtkComponentLevel newRoom)
    {
        isTransitioning = true;
        OnRoomTransitionStart?.Invoke();

        LDtkComponentLevel previousRoom = currentRoom;
        PlayerController player = GameManager.GetInstance()?.GetPlayerController();

        // Freeze player if configured
        if (freezePlayerDuringTransition && player != null)
        {
            player.DisableInput();
        }

        switch (transitionStyle)
        {
            case RoomTransitionStyle.InstantSnap:
                yield return StartCoroutine(InstantSnapTransition(newRoom));
                break;
            case RoomTransitionStyle.SmoothPan:
                yield return StartCoroutine(SmoothPanTransition(newRoom));
                break;
            case RoomTransitionStyle.FadeTransition:
                yield return StartCoroutine(FadeTransition(newRoom));
                break;
        }

        currentRoom = newRoom;
        UpdateCameraBounds();

        // Unfreeze player
        if (freezePlayerDuringTransition && player != null)
        {
            player.EnableInput();
        }

        isTransitioning = false;
        OnRoomTransitionEnd?.Invoke();
        OnRoomChanged?.Invoke(previousRoom, currentRoom);

        Debug.Log($"RoomManager: Transitioned from {previousRoom?.name ?? "null"} to {newRoom.name}");
    }

    private System.Collections.IEnumerator InstantSnapTransition(LDtkComponentLevel newRoom)
    {
        // Instant camera snap to new room center
        CameraController cam = FindAnyObjectByType<CameraController>();
        if (cam != null)
        {
            Rect bounds = GetRoomBounds(newRoom);
            Vector3 newPos = bounds.center;
            newPos.z = cam.cameraZCoordinate;
            cam.transform.position = newPos;
        }
        yield return null;
    }

    private System.Collections.IEnumerator SmoothPanTransition(LDtkComponentLevel newRoom)
    {
        CameraController cam = FindAnyObjectByType<CameraController>();
        if (cam == null) yield break;

        Rect bounds = GetRoomBounds(newRoom);
        Vector3 startPos = cam.transform.position;
        Vector3 endPos = bounds.center;
        endPos.z = cam.cameraZCoordinate;

        float elapsed = 0f;
        while (elapsed < roomTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / roomTransitionDuration);
            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        cam.transform.position = endPos;
    }

    private System.Collections.IEnumerator FadeTransition(LDtkComponentLevel newRoom)
    {
        // TODO: Implement fade to black transition
        // For now, fallback to instant snap
        yield return StartCoroutine(InstantSnapTransition(newRoom));
    }

    /// <summary>
    /// Update camera controller with current room bounds
    /// </summary>
    private void UpdateCameraBounds()
    {
        CameraController cam = FindAnyObjectByType<CameraController>();
        if (cam != null && currentRoom != null)
        {
            // If camera controller has RoomBounds mode, use it
            if (cam.cameraMovementStyle == CameraController.CameraStyles.RoomBounds)
            {
                cam.SetRoomBounds(GetRoomBounds(currentRoom));
            }
        }
    }

    /// <summary>
    /// Get neighbor rooms of current room
    /// </summary>
    public List<LDtkComponentLevel> GetNeighborRooms()
    {
        List<LDtkComponentLevel> neighbors = new List<LDtkComponentLevel>();
        
        if (currentRoom == null) return neighbors;

        LDtkNeighbour[] neighbourData = currentRoom.Neighbours;
        if (neighbourData == null) return neighbors;

        foreach (var neighbor in neighbourData)
        {
            LDtkComponentLevel room = FindRoomByIdentifier(neighbor.LevelIid);
            if (room != null)
            {
                neighbors.Add(room);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Force player to specific room (for spawning/respawning)
    /// </summary>
    public void ForcePlayerToRoom(LDtkComponentLevel room, Vector2 spawnPosition)
    {
        if (room == null) return;

        currentRoom = room;
        UpdateCameraBounds();

        // Instant camera snap
        CameraController cam = FindAnyObjectByType<CameraController>();
        if (cam != null)
        {
            Rect bounds = GetRoomBounds(room);
            Vector3 camPos = bounds.center;
            camPos.z = cam.cameraZCoordinate;
            cam.transform.position = camPos;
        }

        // Move player
        PlayerController player = GameManager.GetInstance()?.GetPlayerController();
        if (player != null)
        {
            player.transform.position = spawnPosition;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showRoomBounds) return;

        // Draw current room bounds
        if (currentRoom != null)
        {
            Gizmos.color = currentRoomColor;
            Rect bounds = GetRoomBounds(currentRoom);
            DrawRectGizmo(bounds);
        }

        // Draw neighbor room bounds
        foreach (var neighbor in GetNeighborRooms())
        {
            Gizmos.color = neighborRoomColor;
            Rect bounds = GetRoomBounds(neighbor);
            DrawRectGizmo(bounds);
        }
    }

    private void DrawRectGizmo(Rect rect)
    {
        Vector3 topLeft = new Vector3(rect.xMin, rect.yMax, 0);
        Vector3 topRight = new Vector3(rect.xMax, rect.yMax, 0);
        Vector3 bottomLeft = new Vector3(rect.xMin, rect.yMin, 0);
        Vector3 bottomRight = new Vector3(rect.xMax, rect.yMin, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}
