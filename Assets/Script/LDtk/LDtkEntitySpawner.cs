using UnityEngine;
using LDtkUnity;

/// <summary>
/// Handles spawning of entities from LDtk data.
/// Attach this to the LDtk project importer to automatically spawn prefabs.
/// </summary>
public class LDtkEntitySpawner : MonoBehaviour, ILDtkImportedFields
{
    [Header("Entity Prefabs")]
    [SerializeField] private GameObject playerSpawnPrefab;
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private GameObject springPrefab;
    [SerializeField] private GameObject strawberryPrefab;
    [SerializeField] private GameObject checkpointPrefab;
    [SerializeField] private GameObject movingPlatformPrefab;
    [SerializeField] private GameObject crumbleBlockPrefab;
    [SerializeField] private GameObject dashCrystalPrefab;

    // LDtk field data
    private string entityType;
    private string direction;
    private int entityId;
    private bool isOneWay;
    private float moveSpeed;
    private Vector2[] pathPoints;

    public void OnLDtkImportFields(LDtkFields fields)
    {
        // Read common fields from LDtk entity
        if (fields.TryGetString("Direction", out string dir))
        {
            direction = dir;
        }

        if (fields.TryGetInt("ID", out int id))
        {
            entityId = id;
        }

        if (fields.TryGetBool("OneWay", out bool oneWay))
        {
            isOneWay = oneWay;
        }

        if (fields.TryGetFloat("Speed", out float speed))
        {
            moveSpeed = speed;
        }

        if (fields.TryGetPointArray("Path", out Vector2[] points))
        {
            pathPoints = points;
        }
    }

    /// <summary>
    /// Configure the spawned entity based on LDtk data
    /// </summary>
    public void ConfigureEntity(GameObject entity)
    {
        if (entity == null) return;

        // Configure direction-based rotation for spikes/springs
        if (entity.TryGetComponent<Spike>(out Spike spike))
        {
            spike.SetDirection(ParseDirection(direction));
        }
        else if (entity.TryGetComponent<Spring>(out Spring spring))
        {
            spring.SetDirection(ParseDirection(direction));
        }
        else if (entity.TryGetComponent<Strawberry>(out Strawberry strawberry))
        {
            strawberry.SetStrawberryId(entityId);
        }
        else if (entity.TryGetComponent<LDtkCheckpoint>(out LDtkCheckpoint checkpoint))
        {
            checkpoint.SetCheckpointId(entityId);
        }
        else if (entity.TryGetComponent<MovingPlatform>(out MovingPlatform platform))
        {
            platform.SetSpeed(moveSpeed);
            if (pathPoints != null && pathPoints.Length > 0)
            {
                platform.SetPath(pathPoints);
            }
        }
    }

    private Direction ParseDirection(string dir)
    {
        if (string.IsNullOrEmpty(dir)) return Direction.Up;

        return dir.ToLower() switch
        {
            "up" => Direction.Up,
            "down" => Direction.Down,
            "left" => Direction.Left,
            "right" => Direction.Right,
            _ => Direction.Up
        };
    }
}

/// <summary>
/// Direction enum used by multiple entities
/// </summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}
