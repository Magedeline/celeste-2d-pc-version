using UnityEngine;
using LDtkUnity;

/// <summary>
/// Collectible strawberry entity - core Celeste collectible.
/// Follows player after collection, saved when player lands safely.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Strawberry : MonoBehaviour, ILDtkImportedFields
{
    [Header("Strawberry Settings")]
    [SerializeField] private int strawberryId = 0;
    [SerializeField] private bool isGolden = false;
    [SerializeField] private bool isWinged = false;

    [Header("Collection Behavior")]
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float followDistance = 0.5f;
    [SerializeField] private float collectRadius = 0.5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private Sprite goldenSprite;
    [SerializeField] private Sprite wingedSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip saveSound;

    [Header("Floating Animation")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatFrequency = 2f;

    private enum StrawberryState
    {
        Idle,
        Following,
        Collected
    }

    private StrawberryState currentState = StrawberryState.Idle;
    private Transform playerTransform;
    private Vector3 startPosition;
    private float floatTimer;
    private AudioSource audioSource;
    private Collider2D strawberryCollider;

    public int StrawberryId => strawberryId;
    public bool IsGolden => isGolden;
    public bool IsWinged => isWinged;
    public bool IsCollected => currentState == StrawberryState.Collected;

    private void Awake()
    {
        strawberryCollider = GetComponent<Collider2D>();
        if (strawberryCollider != null)
        {
            strawberryCollider.isTrigger = true;
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

    public void OnLDtkImportFields(LDtkFields fields)
    {
        if (fields.TryGetInt("ID", out int id))
        {
            strawberryId = id;
        }

        if (fields.TryGetBool("Golden", out bool golden))
        {
            isGolden = golden;
        }

        if (fields.TryGetBool("Winged", out bool winged))
        {
            isWinged = winged;
        }

        UpdateVisual();
    }

    public void SetStrawberryId(int id)
    {
        strawberryId = id;
    }

    private void Start()
    {
        UpdateVisual();
        
        // Check if already collected from save data
        // TODO: Integrate with save system
    }

    private void Update()
    {
        switch (currentState)
        {
            case StrawberryState.Idle:
                UpdateIdleAnimation();
                break;
            case StrawberryState.Following:
                UpdateFollowing();
                break;
        }
    }

    private void UpdateIdleAnimation()
    {
        // Floating animation
        floatTimer += Time.deltaTime * floatFrequency;
        float yOffset = Mathf.Sin(floatTimer) * floatAmplitude;
        transform.position = startPosition + Vector3.up * yOffset;

        // Winged strawberry flies away if player dashes
        if (isWinged)
        {
            CheckWingedBehavior();
        }
    }

    private void CheckWingedBehavior()
    {
        PlayerController player = GameManager.GetInstance()?.GetPlayerController();
        if (player == null) return;

        // TODO: Check if player is dashing and fly away
        // This would require accessing the player's current state
    }

    private void UpdateFollowing()
    {
        if (playerTransform == null)
        {
            PlayerController player = GameManager.GetInstance()?.GetPlayerController();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            return;
        }

        // Smoothly follow player
        Vector3 targetPosition = playerTransform.position + Vector3.up * followDistance;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // Check if player landed safely (grounded)
        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null && pc.GetIsGrounded())
        {
            SaveStrawberry();
        }
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        if (isGolden && goldenSprite != null)
        {
            spriteRenderer.sprite = goldenSprite;
            spriteRenderer.color = Color.yellow;
        }
        else if (isWinged && wingedSprite != null)
        {
            spriteRenderer.sprite = wingedSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState != StrawberryState.Idle) return;

        if (other.CompareTag("Player"))
        {
            CollectStrawberry(other.transform);
        }
    }

    private void CollectStrawberry(Transform player)
    {
        currentState = StrawberryState.Following;
        playerTransform = player;

        // Disable collider
        if (strawberryCollider != null)
        {
            strawberryCollider.enabled = false;
        }

        // Play collect sound
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger("Collect");
        }

        Debug.Log($"Strawberry {strawberryId} collected, following player...");
    }

    private void SaveStrawberry()
    {
        currentState = StrawberryState.Collected;

        // Play particles
        if (collectParticles != null)
        {
            collectParticles.Play();
        }

        // Play save sound
        if (audioSource != null && saveSound != null)
        {
            audioSource.PlayOneShot(saveSound);
        }

        // TODO: Save to game data through GameManager
        Debug.Log($"Strawberry {strawberryId} saved!");

        // Destroy after effect
        Destroy(gameObject, 0.5f);
    }

    /// <summary>
    /// Called when player dies while strawberry is following
    /// </summary>
    public void OnPlayerDeath()
    {
        if (currentState == StrawberryState.Following)
        {
            // Reset to idle state
            currentState = StrawberryState.Idle;
            transform.position = startPosition;
            playerTransform = null;

            if (strawberryCollider != null)
            {
                strawberryCollider.enabled = true;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isGolden ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, collectRadius);

        if (isWinged)
        {
            // Draw wing indicators
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position + Vector3.left * 0.2f, transform.position + Vector3.left * 0.4f + Vector3.up * 0.2f);
            Gizmos.DrawLine(transform.position + Vector3.right * 0.2f, transform.position + Vector3.right * 0.4f + Vector3.up * 0.2f);
        }
    }
}
