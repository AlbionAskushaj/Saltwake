using UnityEngine;
using System.Collections;

public class DarkCloudEntrance : MonoBehaviour
{
    private Vector3 startPosition; // Added to keep track of the original position
    private bool isShaking = false; // Flag to control shaking

    public float shakeMagnitude = 0.1f; // The magnitude of the shake effect
    public float entryDelay = 5.0f; // Delay before the cloud starts moving or another action

    void Start()
    {
        startPosition = transform.position; // Save the original position
        StartCoroutine(ShakeEffect(entryDelay));
    }

    void Update()
    {
        if (isShaking)
        {
            // Continuously apply shaking effect
            float shakeX = startPosition.x + Random.Range(-shakeMagnitude, shakeMagnitude);
            float shakeY = startPosition.y + Random.Range(-shakeMagnitude, shakeMagnitude);
            transform.position = new Vector3(shakeX, shakeY, startPosition.z);
        }
    }

    IEnumerator ShakeEffect(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Start shaking after the delay
        isShaking = true;

        // Example: Stop shaking after a certain time (5 seconds here)
        yield return new WaitForSeconds(5f);
        isShaking = false;

        // Reset position to the original location if needed
        transform.position = startPosition;

        // Optionally, start moving or trigger another behavior here
    }
}
