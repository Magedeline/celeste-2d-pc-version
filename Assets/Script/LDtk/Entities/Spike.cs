using UnityEngine;
using LDtkUnity;

/// <summary>
/// Spike hazard entity - kills player on contact.
/// Direction determines the rotation/orientation of the spike.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Spike : MonoBehaviour, ILDtkImportedFields
{
    [Header("Spike Settings")]
    [SerializeField] private Direction spikeDirection = Direction.Up;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Collider2D spikeCollider;

    private void Awake()
    {
        spikeCollider = GetComponent<Collider2D>();
        if (spikeCollider != null)
        {
            spikeCollider.isTrigger = true;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void OnLDtkImportFields(LDtkFields fields)
    {
        if (fields.TryGetEnum<Direction>("Direction", out Direction dir))
        {
            SetDirection(dir);
        }
        else if (fields.TryGetString("Direction", out string dirStr))
        {
            SetDirection(ParseDirection(dirStr));
        }
    }

    /// <summary>
    /// Set the spike direction and update rotation
    /// </summary>
    public void SetDirection(Direction direction)
    {
        spikeDirection = direction;
        UpdateRotation();
    }

    private void UpdateRotation()
    {
        float angle = spikeDirection switch
        {
            Direction.Up => 0f,
            Direction.Down => 180f,
            Direction.Left => 90f,
            Direction.Right => -90f,
            _ => 0f
        };

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Use existing death system
            GameManager.GetInstance()?.OnPlayerDeath();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        
        // Draw spike direction indicator
        Vector3 dirVector = spikeDirection switch
        {
            Direction.Up => Vector3.up,
            Direction.Down => Vector3.down,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.up
        };

        Gizmos.DrawRay(transform.position, dirVector * 0.3f);
    }
}
