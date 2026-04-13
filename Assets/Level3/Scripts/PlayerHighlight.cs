using UnityEngine;

// Place in the Level 3 scene on any GameObject.
// Finds the player at Start and adds a soft glow circle behind them
// so they stand out against the dark cave tiles.
public class PlayerHighlight : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(0.5f, 0.75f, 1f, 0.3f);
    [SerializeField] private float glowScale = 3f;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        GameObject glow = new GameObject("PlayerGlow");
        glow.transform.SetParent(p.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.3f, 0.1f);
        glow.transform.localScale = Vector3.one * glowScale;

        SpriteRenderer sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite = CreateGlowSprite();
        sr.color = glowColor;
        sr.sortingOrder = -1;
    }

    private Sprite CreateGlowSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = size * 0.5f;
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float a = 1f - Mathf.Clamp01(dist / radius);
                a *= a; // quadratic falloff for a soft edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
