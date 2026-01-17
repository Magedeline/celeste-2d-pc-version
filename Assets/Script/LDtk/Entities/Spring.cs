using UnityEngine;
using LDtkUnity;

/// <summary>
/// Spring/Bouncer entity - launches player in a direction when touched.
/// Classic Celeste mechanic for vertical/horizontal traversal.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Spring : MonoBehaviour, ILDtkImportedFields
{
    [Header("Spring Settings")]
    [SerializeField] private Direction springDirection = Direction.Up;
    [SerializeField] private float launchForce = 15f;
    [SerializeField] private bool resetDash = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerAnimationName = "Trigger";
    [SerializeField] private float cooldownTime = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip springSound;

    private Collider2D springCollider;
    private bool isOnCooldown = false;
    private AudioSource audioSource;

    private void Awake()
    {
        springCollider = GetComponent<Collider2D>();
        if (springCollider != null)
        {
            springCollider.isTrigger = true;
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && springSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
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

        if (fields.TryGetFloat("Force", out float force))
        {
            launchForce = force;
        }

        if (fields.TryGetBool("ResetDash", out bool reset))
        {
            resetDash = reset;
        }
    }

    public void SetDirection(Direction direction)
    {
        springDirection = direction;
        UpdateRotation();
    }

    private void UpdateRotation()
    {
        float angle = springDirection switch
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
        if (isOnCooldown) return;

        if (other.CompareTag("Player"))
        {
            LaunchPlayer(other.gameObject);
        }
    }

    private void LaunchPlayer(GameObject playerObj)
    {
        PlayerController player = playerObj.GetComponent<PlayerController>();
        if (player == null) return;

        // Calculate launch direction
        Vector2 launchDirection = springDirection switch
        {
            Direction.Up => Vector2.up,
            Direction.Down => Vector2.down,
            Direction.Left => Vector2.left,
            Direction.Right => Vector2.right,
            _ => Vector2.up
        };

        // Apply launch velocity
        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Zero out velocity in launch direction first for consistent launches
            if (springDirection == Direction.Up || springDirection == Direction.Down)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            rb.linearVelocity += launchDirection * launchForce;
        }

        // Reset player dash if configured
        if (resetDash)
        {
            player.ResetDash();
        }

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger(triggerAnimationName);
        }

        // Play sound
        if (audioSource != null && springSound != null)
        {
            audioSource.PlayOneShot(springSound);
        }

        // Start cooldown
        StartCoroutine(CooldownCoroutine());
    }

    private System.Collections.IEnumerator CooldownCoroutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Vector3 dirVector = springDirection switch
        {
            Direction.Up => Vector3.up,
            Direction.Down => Vector3.down,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.up
        };

        // Draw spring direction
        Gizmos.DrawRay(transform.position, dirVector * 0.5f);
        
        // Draw launch arc indicator
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + dirVector * 0.5f, 0.2f);
    }
}
