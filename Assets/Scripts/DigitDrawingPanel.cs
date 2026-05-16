using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class DigitDrawingPanel : MonoBehaviour
{
    [Header("Canvas Settings")]
    [Min(1)] public int textureWidth = 50;
    [Min(1)] public int textureHeight = 50;
    [Min(1)] public int brushSize = 3;
    public Color32 backgroundColor = new Color32(0, 0, 0, 255);
    public Color32 drawColor = new Color32(255, 255, 255, 255);

    [Header("Input Settings")]
    public Camera drawCamera;
    public float drawDistance = 8f;
    public LayerMask drawMask = ~0;
    public bool useScreenCenterWhenCursorLocked = true;

    private Renderer targetRenderer;
    private Collider targetCollider;
    private Texture2D drawingTexture;
    private Material runtimeMaterial;
    private Color32[] pixels;
    private bool hasAnyStroke;
    private bool textureDirty;

    public int PixelCount => textureWidth * textureHeight;
    public bool IsCanvasEmpty => !hasAnyStroke;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        targetCollider = GetComponent<Collider>();
        if (drawCamera == null) drawCamera = Camera.main;
        CreateTexture();
    }

    void Update()
    {
        if (!Input.GetMouseButton(0)) return;
        if (drawCamera == null) drawCamera = Camera.main;
        if (drawCamera == null) return;

        Vector3 screenPoint = Input.mousePosition;
        if (Cursor.lockState == CursorLockMode.Locked && useScreenCenterWhenCursorLocked)
            screenPoint = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        Ray ray = drawCamera.ScreenPointToRay(screenPoint);
        if (!Physics.Raycast(ray, out RaycastHit hit, drawDistance, drawMask, QueryTriggerInteraction.Ignore))
            return;
        if (hit.collider != targetCollider) return;

        Vector2 uv = hit.textureCoord;
        int px = Mathf.Clamp(Mathf.RoundToInt(uv.x * (textureWidth - 1)), 0, textureWidth - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(uv.y * (textureHeight - 1)), 0, textureHeight - 1);
        DrawBrush(px, py);
    }

    void LateUpdate()
    {
        if (!textureDirty || drawingTexture == null || pixels == null) return;
        drawingTexture.SetPixels32(pixels);
        drawingTexture.Apply(false, false);
        textureDirty = false;
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
                Object.Destroy(runtimeMaterial);
            else
                Object.DestroyImmediate(runtimeMaterial);
        }
    }

    public void ClearCanvas()
    {
        if (pixels == null || drawingTexture == null) return;
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = backgroundColor;
        drawingTexture.SetPixels32(pixels);
        drawingTexture.Apply(false, false);
        hasAnyStroke = false;
        textureDirty = false;
    }

    public float[] GetNormalizedPixelsTopLeft()
    {
        if (pixels == null) return null;
        float[] result = new float[pixels.Length];
        for (int y = 0; y < textureHeight; y++)
        {
            int sourceY = textureHeight - 1 - y;
            for (int x = 0; x < textureWidth; x++)
            {
                int srcIndex = sourceY * textureWidth + x;
                int dstIndex = y * textureWidth + x;
                result[dstIndex] = pixels[srcIndex].r / 255f;
            }
        }
        return result;
    }

    void CreateTexture()
    {
        drawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        drawingTexture.filterMode = FilterMode.Point;
        drawingTexture.wrapMode = TextureWrapMode.Clamp;

        pixels = new Color32[textureWidth * textureHeight];
        if (runtimeMaterial != null && runtimeMaterial != targetRenderer.sharedMaterial)
        {
            if (Application.isPlaying)
                Object.Destroy(runtimeMaterial);
            else
                Object.DestroyImmediate(runtimeMaterial);
        }
        runtimeMaterial = targetRenderer.material;
        runtimeMaterial.mainTexture = drawingTexture;
        ClearCanvas();
    }

    void DrawBrush(int centerX, int centerY)
    {
        int half = brushSize / 2;
        bool changed = false;

        for (int oy = -half; oy <= half; oy++)
        {
            for (int ox = -half; ox <= half; ox++)
            {
                int px = centerX + ox;
                int py = centerY + oy;
                if (px < 0 || py < 0 || px >= textureWidth || py >= textureHeight) continue;

                int index = py * textureWidth + px;
                if (!pixels[index].Equals(drawColor))
                {
                    pixels[index] = drawColor;
                    changed = true;
                }
            }
        }

        if (!changed) return;
        textureDirty = true;
        hasAnyStroke = true;
    }
}