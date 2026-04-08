using UnityEngine;
using System.Collections;


public class Sky : MonoBehaviour
{
    public Sprite normalSkySprite;
    public Sprite flickerSkySprite;
    private SpriteRenderer spriteRenderer;

    public float flickerDuration = 1f; // Total time the cloud flickers at the start of a new wave
    public float flickerInterval = 0.2f; // Interval between sprite switches

    private Coroutine flickerCoroutine = null;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {


    }

    public void StartFlickering()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine); // Stop flickering if it's already happening
        }
        flickerCoroutine = StartCoroutine(FlickerEffect());
    }

    private IEnumerator FlickerEffect()
    {
        float endTime = Time.time + flickerDuration;
        while (Time.time < endTime)
        {
            // Switch to the flicker sprite
            spriteRenderer.sprite = flickerSkySprite;
            yield return new WaitForSeconds(flickerInterval);

            // Switch back to the normal sprite
            spriteRenderer.sprite = normalSkySprite;
            yield return new WaitForSeconds(flickerInterval);
        }

        // Ensure sprite is set back to normal after flickering
        spriteRenderer.sprite = normalSkySprite;
    }

}
