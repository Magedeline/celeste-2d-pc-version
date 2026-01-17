using UnityEngine;
using LDtkUnity;

/// <summary>
/// Room transition trigger - placed at room edges to handle player transitions.
/// Can be used for manual room transitions or special room connections.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomTransition : MonoBehaviour, ILDtkImportedFields
{
    [Header("Transition Settings")]
    [SerializeField] private string targetRoomId = "";
    [SerializeField] private Direction exitDirection = Direction.Right;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private bool preserveVelocity = true;

    [Header("Transition Type")]
    [SerializeField] private TransitionType transitionType = TransitionType.Automatic;

    public enum TransitionType
    {
        Automatic,      // Triggers when player enters
        OnInput,        // Requires player input (like doors)
        OneWay          // Only triggers from one direction
    }

    [Header("Visual")]
    [SerializeField] private bool showInGame = false;
    [SerializeField] private SpriteRenderer visualIndicator;

    private Collider2D transitionCollider;
    private bool playerInZone = false;

    public string TargetRoomId => targetRoomId;
    public Direction ExitDirection => exitDirection;

    private void Awake()
    {
        transitionCollider = GetComponent<Collider2D>();
        if (transitionCollider != null)
        {
            transitionCollider.isTrigger = true;
        }

        // Hide visual indicator in game if configured
        if (!showInGame && visualIndicator != null)
        {
            visualIndicator.enabled = false;
        }
    }

    public void OnLDtkImportFields(LDtkFields fields)
    {
        if (fields.TryGetString("TargetRoom", out string target))
        {
            targetRoomId = target;
        }

        if (fields.TryGetEnum<Direction>("Direction", out Direction dir))
        {
            exitDirection = dir;
        }
        else if (fields.TryGetString("Direction", out string dirStr))
        {
            exitDirection = ParseDirection(dirStr);
        }

        if (fields.TryGetPoint("SpawnOffset", out Vector2 offset))
        {
            spawnOffset = offset;
        }

        if (fields.TryGetBool("PreserveVelocity", out bool preserve))
        {
            preserveVelocity = preserve;
        }

        if (fields.TryGetEnum<TransitionType>("Type", out TransitionType type))
        {
            transitionType = type;
        }
    }

    private Direction ParseDirection(string dir)
    {
        if (string.IsNullOrEmpty(dir)) return Direction.Right;

        return dir.ToLower() switch
        {
            "up" => Direction.Up,
            "down" => Direction.Down,
            "left" => Direction.Left,
            "right" => Direction.Right,
            _ => Direction.Right
        };
    }

    private void Update()
    {
        // Handle input-based transitions
        if (playerInZone && transitionType == TransitionType.OnInput)
        {
            // Check for interact input
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                TriggerTransition();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInZone = true;

        if (transitionType == TransitionType.Automatic)
        {
            TriggerTransition();
        }
        else if (transitionType == TransitionType.OneWay)
        {
            // Check if player is moving in the correct direction
            if (IsPlayerMovingInExitDirection(other.gameObject))
            {
                TriggerTransition();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }

    private bool IsPlayerMovingInExitDirection(GameObject player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return true;

        Vector2 velocity = rb.linearVelocity;
        
        return exitDirection switch
        {
            Direction.Right => velocity.x > 0.1f,
            Direction.Left => velocity.x < -0.1f,
            Direction.Up => velocity.y > 0.1f,
            Direction.Down => velocity.y < -0.1f,
            _ => true
        };
    }

    private void TriggerTransition()
    {
        if (RoomManager.Instance == null)
        {
            Debug.LogWarning("RoomTransition: RoomManager not found!");
            return;
        }

        if (RoomManager.Instance.IsTransitioning)
        {
            return; // Already transitioning
        }

        PlayerController player = GameManager.GetInstance()?.GetPlayerController();
        if (player == null) return;

        // Store player velocity if needed
        Vector2 playerVelocity = Vector2.zero;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && preserveVelocity)
        {
            playerVelocity = rb.linearVelocity;
        }

        // Find target room
        LDtkComponentLevel targetRoom = FindTargetRoom();
        if (targetRoom == null)
        {
            Debug.LogWarning($"RoomTransition: Target room '{targetRoomId}' not found!");
            return;
        }

        // Calculate spawn position in new room
        Vector2 spawnPosition = CalculateSpawnPosition(targetRoom);

        // Perform transition
        RoomManager.Instance.TransitionToRoom(targetRoom);

        // Move player to spawn position
        player.transform.position = spawnPosition;

        // Restore velocity if configured
        if (rb != null && preserveVelocity)
        {
            rb.linearVelocity = playerVelocity;
        }

        Debug.Log($"Room transition to '{targetRoomId}' at position {spawnPosition}");
    }

    private LDtkComponentLevel FindTargetRoom()
    {
        if (string.IsNullOrEmpty(targetRoomId)) return null;

        // Search through all rooms in RoomManager
        LDtkComponentLevel[] allRooms = FindObjectsByType<LDtkComponentLevel>(FindObjectsSortMode.None);
        foreach (var room in allRooms)
        {
            if (room.name == targetRoomId || room.Identifier == targetRoomId || room.Iid == targetRoomId)
            {
                return room;
            }
        }

        return null;
    }

    private Vector2 CalculateSpawnPosition(LDtkComponentLevel targetRoom)
    {
        Rect targetBounds = targetRoom.BorderRect;
        Vector2 basePosition;

        // Position player at the opposite edge of the target room
        switch (exitDirection)
        {
            case Direction.Right:
                basePosition = new Vector2(targetBounds.xMin + 1f, transform.position.y);
                break;
            case Direction.Left:
                basePosition = new Vector2(targetBounds.xMax - 1f, transform.position.y);
                break;
            case Direction.Up:
                basePosition = new Vector2(transform.position.x, targetBounds.yMin + 1f);
                break;
            case Direction.Down:
                basePosition = new Vector2(transform.position.x, targetBounds.yMax - 1f);
                break;
            default:
                basePosition = targetBounds.center;
                break;
        }

        // Clamp to room bounds
        basePosition.x = Mathf.Clamp(basePosition.x, targetBounds.xMin + 0.5f, targetBounds.xMax - 0.5f);
        basePosition.y = Mathf.Clamp(basePosition.y, targetBounds.yMin + 0.5f, targetBounds.yMax - 0.5f);

        return basePosition + spawnOffset;
    }

    private void OnDrawGizmos()
    {
        // Draw transition zone
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }

        // Draw direction arrow
        Gizmos.color = Color.blue;
        Vector3 dirVector = exitDirection switch
        {
            Direction.Up => Vector3.up,
            Direction.Down => Vector3.down,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.right
        };
        Gizmos.DrawRay(transform.position, dirVector * 0.5f);

        // Draw target room label
        #if UNITY_EDITOR
        if (!string.IsNullOrEmpty(targetRoomId))
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"→ {targetRoomId}");
        }
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
