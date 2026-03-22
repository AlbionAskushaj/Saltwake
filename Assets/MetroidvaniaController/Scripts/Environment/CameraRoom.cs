using UnityEngine;

// Place this on a GameObject with a BoxCollider2D (set as Trigger).
// Size the collider to match the room. When the player enters, the camera
// locks to these bounds and will not show anything outside them.
[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoom : MonoBehaviour
{
    private BoxCollider2D roomCollider;
    private CameraFollow cameraFollow;

    void Awake()
    {
        roomCollider = GetComponent<BoxCollider2D>();
        roomCollider.isTrigger = true;
        cameraFollow = Camera.main.GetComponent<CameraFollow>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            cameraFollow.SetRoomBounds(roomCollider.bounds);
        }
    }

#if UNITY_EDITOR
    // Draw the room bounds in the Scene view so you can see them while editing
    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
    }
#endif
}
