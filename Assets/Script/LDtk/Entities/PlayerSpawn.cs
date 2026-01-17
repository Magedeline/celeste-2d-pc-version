using UnityEngine;
using LDtkUnity;

/// <summary>
/// Marks a player spawn point imported from LDtk.
/// Used by GameManager to determine where to spawn the player in a room.
/// </summary>
public class PlayerSpawn : MonoBehaviour, ILDtkImportedFields
{
    [Header("Spawn Settings")]
    [SerializeField] private bool isDefaultSpawn = false;
    [SerializeField] private Direction facingDirection = Direction.Right;
    [SerializeField] private string spawnId = "";

    [Header("Visual (Editor Only)")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private float gizmoSize = 0.5f;

    public bool IsDefaultSpawn => isDefaultSpawn;
    public Direction FacingDirection => facingDirection;
    public string SpawnId => spawnId;
    public Vector2 SpawnPosition => transform.position;

    public void OnLDtkImportFields(LDtkFields fields)
    {
        if (fields.TryGetBool("IsDefault", out bool defaultSpawn))
        {
            isDefaultSpawn = defaultSpawn;
        }

        if (fields.TryGetString("Direction", out string dir))
        {
            facingDirection = ParseDirection(dir);
        }

        if (fields.TryGetString("SpawnID", out string id))
        {
            spawnId = id;
        }
    }

    private Direction ParseDirection(string dir)
    {
        if (string.IsNullOrEmpty(dir)) return Direction.Right;

        return dir.ToLower() switch
        {
            "left" => Direction.Left,
            "right" => Direction.Right,
            _ => Direction.Right
        };
    }

    /// <summary>
    /// Get the spawn position with slight offset to avoid collision issues
    /// </summary>
    public Vector2 GetAdjustedSpawnPosition()
    {
        return (Vector2)transform.position + Vector2.up * 0.1f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
        
        // Draw facing direction arrow
        Gizmos.color = Color.blue;
        Vector3 dirVector = facingDirection == Direction.Right ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(transform.position, dirVector * 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, gizmoSize);

        // Draw spawn ID label in scene view
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, 
            string.IsNullOrEmpty(spawnId) ? "Player Spawn" : $"Spawn: {spawnId}");
        #endif
    }
}
