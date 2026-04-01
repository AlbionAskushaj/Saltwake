using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomGate : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D gateCollider;
    [SerializeField] private float fadeSpeed = 2f;

    private bool isOpen = false;
    private Color baseColor;

    private void Awake()
    {
        if (gateCollider == null)
            gateCollider = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        gateCollider.isTrigger = false;
        baseColor = spriteRenderer.color;
    }

    public void Open()
    {
        isOpen = true;
        gateCollider.enabled = false;
    }

    public void Close()
    {
        isOpen = false;
        gateCollider.enabled = true;
        spriteRenderer.color = baseColor;
    }

    private void Update()
    {
        if (isOpen && spriteRenderer.color.a > 0f)
        {
            Color c = spriteRenderer.color;
            c.a -= fadeSpeed * Time.deltaTime;
            if (c.a < 0f) c.a = 0f;
            spriteRenderer.color = c;
        }
    }
}
