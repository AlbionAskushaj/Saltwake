using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetRespawnPoint(Transform point)
    {
        if (point != null)
            respawnPoint = point;
    }

    public Transform GetRespawnPoint()
    {
        return respawnPoint;
    }

    public void Respawn()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (respawnPoint != null)
            transform.position = respawnPoint.position;
    }
}