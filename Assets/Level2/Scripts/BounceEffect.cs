using UnityEngine;

public class BounceEffect : MonoBehaviour
{
    public float bounceHeight = 0.5f; // Maximum height the object bounces
    public float bounceSpeed = 2f; // Speed of the bounce

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position; // Store the starting position
    }

    void Update()
    {
        // Calculate the new Y position using a sine wave
        float newY = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight + startPosition.y;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}
