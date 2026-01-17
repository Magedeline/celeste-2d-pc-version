using UnityEngine;
using LDtkUnity;

/// <summary>
/// Checkpoint entity imported from LDtk.
/// Extends the existing checkpoint system with LDtk integration.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LDtkCheckpoint : MonoBehaviour, ILDtkImportedFields
{
    [Header("Checkpoint Settings")]
    [SerializeField] private int checkpointId = 0;
    [SerializeField] private bool isRoomSpawn = false;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private ParticleSystem activateParticles;

    [Header("Audio")]
    [SerializeField] private AudioClip activateSound;

    private bool isActivated = false;
    private Collider2D checkpointCollider;
    private AudioSource audioSource;

    public int CheckpointId => checkpointId;
    public bool IsActivated => isActivated;
    public bool IsRoomSpawn => isRoomSpawn;
    public Vector2 SpawnPosition => transform.position;

    private void Awake()
    {
        checkpointCollider = GetComponent<Collider2D>();
        if (checkpointCollider != null)
        {
            checkpointCollider.isTrigger = true;
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
        if (audioSource == null && activateSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        UpdateVisual();
    }

    public void OnLDtkImportFields(LDtkFields fields)
    {
        if (fields.TryGetInt("ID", out int id))
        {
            checkpointId = id;
        }

        if (fields.TryGetBool("IsRoomSpawn", out bool roomSpawn))
        {
            isRoomSpawn = roomSpawn;
        }

        // Also set the gameObject name to match ID for compatibility with existing system
        gameObject.name = checkpointId.ToString();
    }

    public void SetCheckpointId(int id)
    {
        checkpointId = id;
        gameObject.name = id.ToString();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        isActivated = true;

        // Save checkpoint through existing system
        PlayerController player = GameManager.GetInstance()?.GetPlayerController();
        if (player != null)
        {
            player.GetPlayerData()?.SetCheckpoint(gameObject.name);
            GameManager.GetInstance()?.SaveSlot(GameManager.GetInstance().GetCurrentSaveSlot().SlotID);
        }

        // Update visual
        UpdateVisual();

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger("Activate");
        }

        // Play sound
        if (audioSource != null && activateSound != null)
        {
            audioSource.PlayOneShot(activateSound);
        }

        // Play particles
        if (activateParticles != null)
        {
            activateParticles.Play();
        }

        Debug.Log($"Checkpoint {checkpointId} activated!");
    }

    /// <summary>
    /// Activate checkpoint without triggering effects (for loading)
    /// </summary>
    public void SetActivated(bool activated)
    {
        isActivated = activated;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActivated ? activeColor : inactiveColor;
        }
    }

    /// <summary>
    /// Get the spawn position with slight offset
    /// </summary>
    public Vector2 GetSpawnPosition()
    {
        return (Vector2)transform.position + Vector2.up * 0.1f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.gray;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 1f, 0f));

        // Draw checkpoint ID
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, $"CP: {checkpointId}");
        #endif
    }
}
