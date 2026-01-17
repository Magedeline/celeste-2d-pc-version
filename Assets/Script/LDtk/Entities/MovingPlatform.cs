using UnityEngine;
using LDtkUnity;
using System.Collections.Generic;

/// <summary>
/// Moving platform entity - moves along a defined path.
/// Can carry the player along with it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour, ILDtkImportedFields
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 0.5f;
    [SerializeField] private bool loopPath = true;
    [SerializeField] private bool pingPong = true;
    [SerializeField] private bool startMoving = true;

    [Header("Path")]
    [SerializeField] private List<Vector2> pathPoints = new List<Vector2>();
    [SerializeField] private bool useLocalCoordinates = false;

    [Header("Player Interaction")]
    [SerializeField] private bool carryPlayer = true;
    [SerializeField] private LayerMask playerLayer;

    private int currentPointIndex = 0;
    private int direction = 1;
    private bool isWaiting = false;
    private bool isMoving = true;
    private float waitTimer = 0f;
    private Vector2 lastPosition;
    private Rigidbody2D rb;
    private List<Transform> ridingObjects = new List<Transform>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        lastPosition = transform.position;
    }

    private void Start()
    {
        // Add starting position to path if not included
        if (pathPoints.Count == 0)
        {
            pathPoints.Add(transform.position);
        }
        else if (useLocalCoordinates)
        {
            // Convert local coordinates to world coordinates
            Vector2 startPos = transform.position;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                pathPoints[i] = startPos + pathPoints[i];
            }
        }

        isMoving = startMoving;
    }

    public void OnLDtkImportFields(LDtkFields fields)
    {
        if (fields.TryGetFloat("Speed", out float speed))
        {
            moveSpeed = speed;
        }

        if (fields.TryGetFloat("WaitTime", out float wait))
        {
            waitTime = wait;
        }

        if (fields.TryGetBool("Loop", out bool loop))
        {
            loopPath = loop;
        }

        if (fields.TryGetBool("PingPong", out bool pp))
        {
            pingPong = pp;
        }

        if (fields.TryGetPointArray("Path", out Vector2[] points))
        {
            SetPath(points);
        }
    }

    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void SetPath(Vector2[] points)
    {
        pathPoints.Clear();
        pathPoints.Add(transform.position); // Start position
        pathPoints.AddRange(points);
    }

    public void SetPath(List<Vector2> points)
    {
        pathPoints = new List<Vector2>(points);
        if (pathPoints.Count > 0 && pathPoints[0] != (Vector2)transform.position)
        {
            pathPoints.Insert(0, transform.position);
        }
    }

    private void FixedUpdate()
    {
        if (!isMoving || pathPoints.Count < 2) return;

        if (isWaiting)
        {
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
            }
            return;
        }

        // Move towards current target point
        Vector2 targetPoint = pathPoints[currentPointIndex];
        Vector2 currentPos = rb.position;
        Vector2 moveDirection = (targetPoint - currentPos).normalized;
        float distanceToTarget = Vector2.Distance(currentPos, targetPoint);
        float moveDistance = moveSpeed * Time.fixedDeltaTime;

        if (moveDistance >= distanceToTarget)
        {
            // Reached target point
            rb.MovePosition(targetPoint);
            OnReachedPoint();
        }
        else
        {
            rb.MovePosition(currentPos + moveDirection * moveDistance);
        }

        // Move riding objects
        MoveRidingObjects();

        lastPosition = rb.position;
    }

    private void OnReachedPoint()
    {
        if (pingPong)
        {
            currentPointIndex += direction;

            if (currentPointIndex >= pathPoints.Count)
            {
                direction = -1;
                currentPointIndex = pathPoints.Count - 2;
            }
            else if (currentPointIndex < 0)
            {
                direction = 1;
                currentPointIndex = 1;
            }
        }
        else if (loopPath)
        {
            currentPointIndex = (currentPointIndex + 1) % pathPoints.Count;
        }
        else
        {
            currentPointIndex++;
            if (currentPointIndex >= pathPoints.Count)
            {
                isMoving = false;
                return;
            }
        }

        // Wait at point
        if (waitTime > 0f)
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    private void MoveRidingObjects()
    {
        if (!carryPlayer) return;

        Vector2 delta = rb.position - lastPosition;
        if (delta.magnitude < 0.001f) return;

        foreach (var obj in ridingObjects)
        {
            if (obj != null)
            {
                obj.position += (Vector3)delta;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!carryPlayer) return;

        // Check if player landed on top
        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // Player is on top
                {
                    if (!ridingObjects.Contains(collision.transform))
                    {
                        ridingObjects.Add(collision.transform);
                    }
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ridingObjects.Remove(collision.transform);
        }
    }

    /// <summary>
    /// Start/stop platform movement
    /// </summary>
    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    /// <summary>
    /// Reset platform to starting position
    /// </summary>
    public void ResetPosition()
    {
        if (pathPoints.Count > 0)
        {
            rb.position = pathPoints[0];
            currentPointIndex = 0;
            direction = 1;
            isWaiting = false;
            isMoving = startMoving;
        }
    }

    private void OnDrawGizmos()
    {
        if (pathPoints == null || pathPoints.Count == 0) return;

        Gizmos.color = Color.cyan;

        // Draw path
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
        }

        if (loopPath && pathPoints.Count > 2)
        {
            Gizmos.DrawLine(pathPoints[pathPoints.Count - 1], pathPoints[0]);
        }

        // Draw points
        Gizmos.color = Color.yellow;
        foreach (var point in pathPoints)
        {
            Gizmos.DrawWireSphere(point, 0.2f);
        }

        // Draw current target
        if (Application.isPlaying && currentPointIndex < pathPoints.Count)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pathPoints[currentPointIndex], 0.3f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw platform bounds
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}
