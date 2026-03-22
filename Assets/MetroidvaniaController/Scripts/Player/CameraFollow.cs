using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform Target;
    public float FollowSpeed = 8f;
    public float RoomTransitionSpeed = 5f; // Slower pan when switching rooms

    [Header("Shake")]
    public float shakeAmount = 0.1f;
    public float decreaseFactor = 1.0f;

    private Camera cam;
    private Vector3 targetPos;
    private bool hasBounds = false;
    private Bounds roomBounds;
    private bool isTransitioning = false;

    // Shake state
    private float shakeDuration = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (Target == null) return;

        Vector3 desired = new Vector3(Target.position.x, Target.position.y, -10f);

        if (hasBounds)
            desired = ClampToBounds(desired);

        float speed = isTransitioning ? RoomTransitionSpeed : FollowSpeed;
        targetPos = Vector3.Lerp(transform.position, desired, speed * Time.deltaTime);
        targetPos.z = -10f;

        // Check if we've arrived at the new room position
        if (isTransitioning && Vector3.Distance(targetPos, desired) < 0.05f)
            isTransitioning = false;

        // Apply shake on top
        UpdateShake();
        transform.position = targetPos + shakeOffset;
    }

    // Called by CameraRoom when the player enters a new room
    public void SetRoomBounds(Bounds bounds)
    {
        roomBounds = bounds;
        hasBounds = true;
        isTransitioning = true;
    }

    // Clamp the camera position so it never shows outside the room
    private Vector3 ClampToBounds(Vector3 pos)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // If the room is smaller than the camera view, center on the room axis
        float minX = roomBounds.min.x + halfW;
        float maxX = roomBounds.max.x - halfW;
        float minY = roomBounds.min.y + halfH;
        float maxY = roomBounds.max.y - halfH;

        pos.x = (minX > maxX) ? roomBounds.center.x : Mathf.Clamp(pos.x, minX, maxX);
        pos.y = (minY > maxY) ? roomBounds.center.y : Mathf.Clamp(pos.y, minY, maxY);

        return pos;
    }

    private void UpdateShake()
    {
        if (shakeDuration > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeAmount;
            shakeOffset.z = 0f;
            shakeDuration -= Time.deltaTime * decreaseFactor;
        }
        else
        {
            shakeOffset = Vector3.zero;
            shakeDuration = 0f;
        }
    }

    public void ShakeCamera()
    {
        shakeDuration = 0.2f;
    }
}
