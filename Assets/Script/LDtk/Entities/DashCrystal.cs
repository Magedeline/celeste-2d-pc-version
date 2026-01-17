using UnityEngine;
using LDtkUnity;

/// <summary>
/// Dash Crystal/Refill entity - restores player's dash ability.
/// Core Celeste mechanic for extended air traversal.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DashCrystal : MonoBehaviour, ILDtkImportedFields
{
    [Header("Crystal Settings")]
    [SerializeField] private bool isDoubleDash = false;
    [SerializeField] private float respawnTime = 2.5f;
    [SerializeField] private bool oneTimeUse = false;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color doubleDashColor = Color.magenta;
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private ParticleSystem respawnParticles;

    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatFrequency = 2f;
    [SerializeField] private float rotationSpeed = 30f;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip respawnSound;

    private bool isActive = true;
    private Vector3 startPosition;
    private float floatTimer;
    private Collider2D crystalCollider;
    private AudioSource audioSource;

    public bool IsDoubleDash => isDoubleDash;
    public bool IsActive => isActive;

    private void Awake()
    {
        crystalCollider = GetComponent<Collider2D>();
        if (crystalCollider != null)
        {
            crystalCollider.isTrigger = true;
        }

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

    private void Start()
    {
        UpdateVisual();
    }

    public void OnLDtkImportFields(LDtkFields fields)
    {
        if (fields.TryGetBool("DoubleDash", out bool doubleDash))
        {
            isDoubleDash = doubleDash;
        }

        if (fields.TryGetFloat("RespawnTime", out float time))
        {
            respawnTime = time;
        }

        if (fields.TryGetBool("OneTime", out bool oneTime))
        {
            oneTimeUse = oneTime;
        }

        UpdateVisual();
    }

    private void Update()
    {
        if (!isActive) return;

        // Floating animation
        floatTimer += Time.deltaTime;
        float yOffset = Mathf.Sin(floatTimer * floatFrequency) * floatAmplitude;
        transform.position = startPosition + Vector3.up * yOffset;

        // Rotation
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActive ? (isDoubleDash ? doubleDashColor : normalColor) : Color.gray;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        if (other.CompareTag("Player"))
        {
            CollectCrystal(other.gameObject);
        }
    }

    private void CollectCrystal(GameObject playerObj)
    {
        var player = playerObj.GetComponent<PlayerController>();
        if (player == null) return;

        // Check if player needs dash refill
        // TODO: Add check if player already has max dashes

        // Refill dash
        if (isDoubleDash)
        {
            player.SetMaxDashes(2);
        }
        player.ResetDash();

        // Deactivate crystal
        isActive = false;
        UpdateVisual();

        // Hide visual
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        // Play effects
        if (collectParticles != null)
        {
            collectParticles.Play();
        }

        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        if (animator != null)
        {
            animator.SetTrigger("Collect");
        }

        Debug.Log($"Dash Crystal collected! Double: {isDoubleDash}");

        // Start respawn timer if not one-time use
        if (!oneTimeUse)
        {
            StartCoroutine(RespawnCoroutine());
        }
    }

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);

        // Respawn crystal
        isActive = true;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        UpdateVisual();

        // Play respawn effects
        if (respawnParticles != null)
        {
            respawnParticles.Play();
        }

        if (audioSource != null && respawnSound != null)
        {
            audioSource.PlayOneShot(respawnSound);
        }

        if (animator != null)
        {
            animator.SetTrigger("Respawn");
        }

        // Reset position
        transform.position = startPosition;
        floatTimer = 0f;
    }

    /// <summary>
    /// Force respawn the crystal (for room reset)
    /// </summary>
    public void ForceRespawn()
    {
        StopAllCoroutines();
        isActive = true;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        UpdateVisual();
        transform.position = startPosition;
        floatTimer = 0f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isDoubleDash ? Color.magenta : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw diamond shape
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawLine(pos + Vector3.up * 0.3f, pos + Vector3.right * 0.2f);
        Gizmos.DrawLine(pos + Vector3.right * 0.2f, pos + Vector3.down * 0.3f);
        Gizmos.DrawLine(pos + Vector3.down * 0.3f, pos + Vector3.left * 0.2f);
        Gizmos.DrawLine(pos + Vector3.left * 0.2f, pos + Vector3.up * 0.3f);
    }
}
