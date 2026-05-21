using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MazeTaskLogic : MonoBehaviour
{
    [Header("Связь с 3D миром")]
    public TaskStation myStation;

    [Header("UI")]
    public RawImage mazeImage;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Slider timerSlider;

    [Header("Настройки")]
    public float timeLimit = 30f;
    public int cellSize = 20;

    [Header("Лабиринт (редактируй в MazeEditor)")]
    public int mazeWidth = 11;
    public int mazeHeight = 11;
    public int[] mazeData;        // 0=путь 1=стена — заполняется редактором
    public Vector2Int startPos = new Vector2Int(1, 1);
    public Vector2Int endPos = new Vector2Int(9, 9);

    Vector2Int playerPos;
    float timeRemaining;
    bool isActive;
    Texture2D tex;

    void OnEnable()
    {
        if (mazeData == null || mazeData.Length != mazeWidth * mazeHeight)
        {
            Debug.LogError("MazeTaskLogic: mazeData не задан! Открой MazeEditor в Inspector.");
            return;
        }

        playerPos = startPos;
        timeRemaining = timeLimit;
        isActive = true;

        tex = new Texture2D(mazeWidth * cellSize, mazeHeight * cellSize);
        tex.filterMode = FilterMode.Point;
        mazeImage.texture = tex;

        if (timerSlider != null) { timerSlider.maxValue = timeLimit; timerSlider.value = timeLimit; }
        if (statusText != null) { statusText.text = "Найди выход!"; statusText.color = Color.white; }

        DrawMaze();
    }

    void Update()
    {
        if (!isActive) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = "TIME: " + Mathf.Max(0, timeRemaining).ToString("F1") + "s";
            timerText.color = timeRemaining <= 5f ? Color.red : Color.white;
        }
        if (timerSlider != null)
            timerSlider.value = timeRemaining;

        if (timeRemaining <= 0f) { TimeOut(); return; }

        HandleInput();
    }

    void HandleInput()
    {
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = new Vector2Int(-1, 0);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = new Vector2Int(1, 0);
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = new Vector2Int(0, -1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = new Vector2Int(0, 1);
        if (dir == Vector2Int.zero) return;

        Vector2Int next = playerPos + dir;
        if (next.x < 0 || next.x >= mazeHeight || next.y < 0 || next.y >= mazeWidth) return;
        if (GetCell(next.x, next.y) == 1) return;

        playerPos = next;
        DrawMaze();

        if (playerPos == endPos)
        {
            isActive = false;
            if (statusText != null) { statusText.text = "ВЫХОД НАЙДЕН!"; statusText.color = Color.green; }
            Invoke(nameof(FinishGame), 0.8f);
        }
    }

    int GetCell(int row, int col) => mazeData[row * mazeWidth + col];

    void DrawMaze()
    {
        for (int r = 0; r < mazeHeight; r++)
        {
            for (int c = 0; c < mazeWidth; c++)
            {
                Color cell;
                if (r == playerPos.x && c == playerPos.y) cell = Color.white;
                else if (r == startPos.x && c == startPos.y) cell = Color.green;
                else if (r == endPos.x && c == endPos.y) cell = Color.red;
                else if (GetCell(r, c) == 1) cell = new Color(0.15f, 0.15f, 0.15f);
                else cell = new Color(0.85f, 0.85f, 0.85f);

                int texY = (mazeHeight - 1 - r) * cellSize;
                int texX = c * cellSize;
                for (int py = 0; py < cellSize; py++)
                    for (int px = 0; px < cellSize; px++)
                        tex.SetPixel(texX + px, texY + py, cell);
            }
        }
        tex.Apply();
    }

    void TimeOut()
    {
        isActive = false;
        if (statusText != null) { statusText.text = "TIME OUT"; statusText.color = Color.red; }
        Invoke(nameof(FailGame), 1f);
    }

    void FinishGame() { if (myStation != null) myStation.CompleteTask(); }
    void FailGame() { if (myStation != null) myStation.FailTask(); }
}