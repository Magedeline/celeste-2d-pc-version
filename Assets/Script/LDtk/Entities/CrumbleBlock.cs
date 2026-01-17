using UnityEngine;
using LDtkUnity;

/// <summary>
/// Crumble Block entity - collapses when player stands on it.
/// Regenerates after a set time. Classic Celeste platforming hazard.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CrumbleBlock : MonoBehaviour, ILDtkImportedFields
{
    [Header("Crumble Settings")]
    [SerializeField] private float crumbleDelay = 0.3f;
    [SerializeField] private float respawnTime = 2f;
    [SerializeField] private bool respawns = true;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem crumbleParticles;
    [SerializeField] private float shakeIntensity = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioClip shakeSound;
    [SerializeField] private AudioClip crumbleSound;
    [SerializeField] private AudioClip respawnSound;

    private enum BlockState
    {
        Solid,
        Shaking,
        Crumbled,
        Respawning
    }

    private BlockState currentState = BlockState.Solid;
    private Vector3 startPosition;
    private Collider2D blockCollider;
    private AudioSource audioSource;
    private float shakeTimer;

    public bool IsSolid => currentState == BlockState.Solid || currentState == BlockState.Shaking;

    private void Awake()
    {
        blockCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        startPosition = transform.position;
    }

    public void OnLDtkImportFields(LDtkFields fields)
    {
        if (fields.TryGetFloat("CrumbleDelay", out float delay))
        {
            crumbleDelay = delay;
        }

        if (fields.TryGetFloat("RespawnTime", out float time))
        {
            respawnTime = time;
        }

        if (fields.TryGetBool("Respawns", out bool resp))
        {
            respawns = resp;
        }
    }

    private void Update()
    {
        if (currentState == BlockState.Shaking)
        {
            // Shake effect
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
            transform.position = startPosition + new Vector3(offsetX, offsetY, 0f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState != BlockState.Solid) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // Check if player is on top
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // Player landed on top
                {
                    StartCrumble();
                    break;
                }
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (currentState != BlockState.Solid) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    StartCrumble();
                    break;
                }
            }
        }
    }

    private void StartCrumble()
    {
        if (currentState != BlockState.Solid) return;

        currentState = BlockState.Shaking;

        // Play shake sound
        if (audioSource != null && shakeSound != null)
        {
            audioSource.PlayOneShot(shakeSound);
        }

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger("Shake");
        }

        StartCoroutine(CrumbleCoroutine());
    }

    private System.Collections.IEnumerator CrumbleCoroutine()
    {
        yield return new WaitForSeconds(crumbleDelay);

        // Crumble
        currentState = BlockState.Crumbled;
        transform.position = startPosition;

        // Disable collision
        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        // Hide visual
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        // Play effects
        if (crumbleParticles != null)
        {
            crumbleParticles.Play();
        }

        if (audioSource != null && crumbleSound != null)
        {
            audioSource.PlayOneShot(crumbleSound);
        }

        if (animator != null)
        {
            animator.SetTrigger("Crumble");
        }

        // Start respawn if enabled
        if (respawns)
        {
            yield return new WaitForSeconds(respawnTime);
            Respawn();
        }
    }

    private void Respawn()
    {
        currentState = BlockState.Respawning;

        // Re-enable collision
        if (blockCollider != null)
        {
            blockCollider.enabled = true;
        }

        // Show visual
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // Play respawn effects
        if (audioSource != null && respawnSound != null)
        {
            audioSource.PlayOneShot(respawnSound);
        }

        if (animator != null)
        {
            animator.SetTrigger("Respawn");
        }

        // Delay before becoming solid again (so player can move through)
        StartCoroutine(FinishRespawnCoroutine());
    }

    private System.Collections.IEnumerator FinishRespawnCoroutine()
    {
        yield return new WaitForSeconds(0.1f);
        currentState = BlockState.Solid;
    }

    /// <summary>
    /// Force reset the block (for room reset)
    /// </summary>
    public void ForceReset()
    {
        StopAllCoroutines();
        currentState = BlockState.Solid;
        transform.position = startPosition;

        if (blockCollider != null)
        {
            blockCollider.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = currentState switch
        {
            BlockState.Solid => Color.gray,
            BlockState.Shaking => Color.yellow,
            BlockState.Crumbled => new Color(0.5f, 0.5f, 0.5f, 0.3f),
            BlockState.Respawning => Color.cyan,
            _ => Color.gray
        };

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}
