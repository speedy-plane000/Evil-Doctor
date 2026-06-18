using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class RemotePathfinderHelper : MonoBehaviour
{
    const float PlayerLookupInterval = 1f;
    const float AbilityCooldownSeconds = 30f;
    const float LeverHighlightSeconds = 5f;
    const float MessageVisibleSeconds = 5f;
    const float LeverMarkerVerticalOffset = 1.2f;
    const string LeverMarkerText = "★ СТОЛБ РЫЧАГА ★";
    const float GridCellSizeWorldUnits = 2.75f;
    const float GridXWorldOffset = 0.3f;
    const float GridZWorldOffset = 7.25f;
    const int MazeOffset = 6;
    const int MazeScale = 2;
    const int LeverCellSearchRadius = 2;
    const float LeverParticipantsRefreshInterval = 2f;

    [Header("Interaction")]
    public float interactionDistance = 2.5f;
    public KeyCode pickupKey = KeyCode.E;

    [Header("Abilities")]
    public KeyCode showExitDistanceKey = KeyCode.F;
    public KeyCode showNearestLeverKey = KeyCode.G;

    [Header("Maze Data")]
    public string gridResourceName = "AStarGrid";

    [Header("Highlight Material")]
    public Material highlightMaterial;
    
    PlayerMovement cachedPlayer;
    float nextPlayerLookupTime;
    bool isPickedUp;
    float nextAbilityAllowedTime;

    Renderer[] pickupRenderers;
    Collider[] pickupColliders;

    int[][] grid;
    int gridWidth;
    int gridHeight;
    Vector2Int exitCell = new Vector2Int(-1, -1);

    string statusMessage = string.Empty;
    float statusMessageUntilTime;
    GUIStyle statusMessageStyle;
    GUIStyle leverMarkerStyle;
    Vector3 highlightedLeverMarkerWorldPosition;
    float highlightedLeverMarkerUntilTime;

    Coroutine leverHighlightCoroutine;
    AN_Button[] cachedParticipantLevers;
    float nextParticipantLeversRefreshTime;
    readonly List<AN_Button> leverBuffer = new List<AN_Button>(8);
    readonly HashSet<AN_Button> uniqueLeverBuffer = new HashSet<AN_Button>();

    sealed class HighlightState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] highlightMaterials;
    }

    static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };
    static readonly AN_Button[] EmptyButtons = Array.Empty<AN_Button>();

    void Awake()
    {
        pickupRenderers = GetComponentsInChildren<Renderer>(true);
        pickupColliders = GetComponentsInChildren<Collider>(true);
        cachedPlayer = ResolvePlayer();
        TryLoadGrid();
    }

    public bool IsPickedUp => isPickedUp;
    
    void Update()
    {
        if (!TryGetPlayer(out PlayerMovement player))
            return;

        if (!isPickedUp && Input.GetKeyDown(pickupKey))
        {
            float distanceToPickup = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPickup <= interactionDistance)
                PickUpRemote();
        }

        if (!isPickedUp)
            return;

        if (Input.GetKeyDown(showExitDistanceKey))
            UseExitAbility();

        if (Input.GetKeyDown(showNearestLeverKey))
            UseNearestLeverAbility();
    }

    void OnGUI()
    {
        if (Time.time > statusMessageUntilTime || string.IsNullOrEmpty(statusMessage))
        {
            DrawLeverMarkerOnScreen();
            return;
        }

        if (statusMessageStyle == null)
        {
            statusMessageStyle = new GUIStyle(GUI.skin.label);
            statusMessageStyle.alignment = TextAnchor.MiddleCenter;
            statusMessageStyle.fontSize = 32;
            statusMessageStyle.fontStyle = FontStyle.Bold;
            statusMessageStyle.normal.textColor = Color.white;
        }

        Rect messageRect = new Rect(0f, Screen.height - 50f, Screen.width, 40f);
        GUI.Label(messageRect, statusMessage, statusMessageStyle);
        DrawLeverMarkerOnScreen();
    }

    bool TryGetPlayer(out PlayerMovement player)
    {
        if (cachedPlayer == null && Time.time >= nextPlayerLookupTime)
        {
            cachedPlayer = ResolvePlayer();
            nextPlayerLookupTime = Time.time + PlayerLookupInterval;
        }

        player = cachedPlayer;
        return player != null;
    }

    PlayerMovement ResolvePlayer()
    {
        GameObject namedPlayer = GameObject.Find("player");
        if (namedPlayer != null)
        {
            PlayerMovement namedPlayerMovement = namedPlayer.GetComponent<PlayerMovement>();
            if (namedPlayerMovement != null)
                return namedPlayerMovement;
        }

        return FindObjectOfType<PlayerMovement>();
    }

    void PickUpRemote()
    {
        isPickedUp = true;
        HidePickupModel();
    }

    void HidePickupModel()
    {
        if (pickupRenderers != null)
        {
            for (int i = 0; i < pickupRenderers.Length; i++)
            {
                if (pickupRenderers[i] != null)
                    pickupRenderers[i].enabled = false;
            }
        }

        if (pickupColliders != null)
        {
            for (int i = 0; i < pickupColliders.Length; i++)
            {
                if (pickupColliders[i] != null)
                    pickupColliders[i].enabled = false;
            }
        }
    }

    void UseExitAbility()
    {
        if (!TryConsumeAbilityUse())
            return;

        if (grid == null || exitCell.x < 0)
        {
            ShowStatusMessage("Сетка AStar не найдена");
            return;
        }

        if (!TryGetPlayerGridCell(out Vector2Int playerCell))
        {
            ShowStatusMessage("Не удалось определить клетку игрока");
            return;
        }

        if (!TryFindPathLength(playerCell, exitCell, out int pathLength))
        {
            ShowStatusMessage("До выхода нет пути");
            return;
        }

        ShowStatusMessage($"До выхода: {FormatMazeCells(pathLength)}");
    }

    void UseNearestLeverAbility()
    {
        if (!TryConsumeAbilityUse())
            return;

        if (grid == null)
        {
            ShowStatusMessage("Сетка AStar не найдена");
            return;
        }

        if (!TryGetPlayerGridCell(out Vector2Int playerCell))
        {
            ShowStatusMessage("Не удалось определить клетку игрока");
            return;
        }

        AN_Button nearestLever = null;
        int nearestPathLength = int.MaxValue;
        AN_Button[] firstFloorLevers = GetLeversWithSearchParticipant();
        EvaluateNearestLeverFromButtons(
            firstFloorLevers,
            playerCell,
            ref nearestLever,
            ref nearestPathLength);

        if (nearestLever == null)
        {
            ShowStatusMessage("Не найден доступный рычаг");
            return;
        }

        ShowStatusMessage($"До рычага: {FormatMazeCells(nearestPathLength)}");

        if (leverHighlightCoroutine != null)
            StopCoroutine(leverHighlightCoroutine);

        leverHighlightCoroutine = StartCoroutine(HighlightLever(nearestLever.gameObject));
    }

    void EvaluateNearestLeverFromButtons(
        AN_Button[] buttons,
        Vector2Int playerCell,
        ref AN_Button nearestLever,
        ref int nearestPathLength)
    {
        if (buttons == null || buttons.Length == 0)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            AN_Button button = buttons[i];
            if (button == null || !button.isLever || button.WasAlreadyPressed)
                continue;

            if (!TryGetLeverGridCell(button, out Vector2Int leverCell))
                continue;

            if (!TryFindPathLength(playerCell, leverCell, out int pathLength))
                continue;

            if (pathLength < nearestPathLength)
            {
                nearestPathLength = pathLength;
                nearestLever = button;
            }
        }
    }

    bool TryConsumeAbilityUse()
    {
        if (Time.time < nextAbilityAllowedTime)
        {
            float cooldownLeft = Mathf.Ceil(nextAbilityAllowedTime - Time.time);
            ShowStatusMessage($"Перезарядка: {cooldownLeft:0} c");
            return false;
        }

        nextAbilityAllowedTime = Time.time + AbilityCooldownSeconds;
        return true;
    }

    void ShowStatusMessage(string message)
    {
        statusMessage = message;
        statusMessageUntilTime = Time.time + MessageVisibleSeconds;
    }

    string FormatMazeCells(int pathLength)
    {
        float cells = pathLength / 2f;
        float rounded = Mathf.Round(cells);
        if (Mathf.Abs(cells - rounded) < 0.001f)
        {
            int wholeCells = Mathf.Max(0, (int)rounded);
            return $"{wholeCells} {GetRussianCellWord(wholeCells)}";
        }

        return $"{cells:0.0} клетки";
    }

    string GetRussianCellWord(int value)
    {
        int absValue = Mathf.Abs(value);
        int lastTwo = absValue % 100;
        int lastOne = absValue % 10;

        if (lastTwo >= 11 && lastTwo <= 14)
            return "клеток";

        if (lastOne == 1)
            return "клетка";

        if (lastOne >= 2 && lastOne <= 4)
            return "клетки";

        return "клеток";
    }

    bool TryLoadGrid()
    {
        TextAsset gridAsset = Resources.Load<TextAsset>(gridResourceName);
        if (gridAsset == null || string.IsNullOrWhiteSpace(gridAsset.text))
            return false;

        if (!TryParseGrid(gridAsset.text, out int[][] parsedGrid))
            return false;

        grid = parsedGrid;
        gridHeight = grid.Length;
        gridWidth = gridHeight > 0 ? grid[0].Length : 0;
        exitCell = FindCellByValue(3);

        return true;
    }

    bool TryParseGrid(string json, out int[][] parsedGrid)
    {
        parsedGrid = null;
        List<int[]> rows = new List<int[]>();
        List<int> currentRow = null;
        int depth = 0;
        int number = 0;
        bool hasNumber = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (c == '[')
            {
                depth++;
                if (depth == 2)
                    currentRow = new List<int>();
                continue;
            }

            if (char.IsDigit(c))
            {
                number = number * 10 + (c - '0');
                hasNumber = true;
                continue;
            }

            if (hasNumber)
            {
                if (currentRow != null)
                    currentRow.Add(number);
                number = 0;
                hasNumber = false;
            }

            if (c == ']')
            {
                if (depth <= 0)
                    return false;

                if (depth == 2 && currentRow != null && currentRow.Count > 0)
                    rows.Add(currentRow.ToArray());
                depth--;
            }
        }

        if (depth != 0 || rows.Count == 0)
            return false;

        int width = rows[0].Length;
        for (int i = 1; i < rows.Count; i++)
        {
            if (rows[i].Length != width)
                return false;
        }

        parsedGrid = rows.ToArray();
        return true;
    }

    Vector2Int FindCellByValue(int value)
    {
        if (grid == null)
            return new Vector2Int(-1, -1);

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (grid[y][x] == value)
                    return new Vector2Int(x, y);
            }
        }

        return new Vector2Int(-1, -1);
    }

    bool TryWorldToGridCell(Vector3 worldPosition, out Vector2Int cell)
    {
        int gridX = (Mathf.FloorToInt((worldPosition.x - GridXWorldOffset) / GridCellSizeWorldUnits) + MazeOffset) * MazeScale;
        int gridZ = (-Mathf.FloorToInt((worldPosition.z - GridZWorldOffset) / GridCellSizeWorldUnits) + MazeOffset) * MazeScale - 2;
        cell = new Vector2Int(gridX, gridZ);

        return IsInBounds(cell);
    }

    bool TryGetPlayerGridCell(out Vector2Int playerCell)
    {
        playerCell = default;

        if (!TryGetPlayer(out PlayerMovement player))
            return false;

        if (!TryWorldToGridCell(player.transform.position, out Vector2Int rawCell))
            return false;

        if (!TryFindNearestPassableCell(rawCell, out playerCell))
            return false;

        Debug.Log($"[RemotePathfinderHelper] JSON grid cell used for player: x={playerCell.x}, y={playerCell.y} (y->world z, raw: x={rawCell.x}, y={rawCell.y})");
        return true;
    }

    bool TryFindNearestPassableCell(Vector2Int start, out Vector2Int passableCell)
    {
        passableCell = default;

        if (!IsInBounds(start))
            return false;

        if (IsPassable(start))
        {
            passableCell = start;
            return true;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int next = current + Directions[i];
                if (!IsInBounds(next) || visited.Contains(next))
                    continue;

                if (IsPassable(next))
                {
                    passableCell = next;
                    return true;
                }

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }

    bool TryFindPathLength(Vector2Int start, Vector2Int goal, out int pathLength)
    {
        pathLength = -1;

        if (!IsInBounds(start) || !IsInBounds(goal) || !IsPassable(goal))
            return false;

        if (!IsPassable(start))
        {
            if (!TryFindNearestPassableCell(start, out start))
                return false;
        }

        List<Vector2Int> openSet = new List<Vector2Int> { start };
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        Dictionary<Vector2Int, int> fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            int bestIndex = 0;
            Vector2Int current = openSet[0];
            int currentBestF = fScore[current];
            int currentBestH = Heuristic(current, goal);

            for (int i = 1; i < openSet.Count; i++)
            {
                Vector2Int candidate = openSet[i];
                int candidateF = fScore[candidate];
                if (candidateF > currentBestF)
                    continue;

                int candidateH = Heuristic(candidate, goal);
                if (candidateF < currentBestF || candidateH < currentBestH)
                {
                    bestIndex = i;
                    current = candidate;
                    currentBestF = candidateF;
                    currentBestH = candidateH;
                }
            }

            if (current == goal)
            {
                pathLength = gScore[current];
                return true;
            }

            openSet.RemoveAt(bestIndex);
            closedSet.Add(current);

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int neighbor = current + Directions[i];
                if (!IsInBounds(neighbor) || !IsPassable(neighbor) || closedSet.Contains(neighbor))
                    continue;

                int tentativeG = gScore[current] + 1;
                if (!gScore.TryGetValue(neighbor, out int knownG) || tentativeG < knownG)
                {
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return false;
    }

    int Heuristic(Vector2Int from, Vector2Int to)
    {
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }

    bool IsInBounds(Vector2Int cell)
    {
        return grid != null && cell.x >= 0 && cell.y >= 0 && cell.x < gridWidth && cell.y < gridHeight;
    }

    bool IsPassable(Vector2Int cell)
    {
        if (!IsInBounds(cell))
            return false;

        return grid[cell.y][cell.x] != 0;
    }

    bool IsLeverCell(Vector2Int cell)
    {
        return IsInBounds(cell) && grid[cell.y][cell.x] == 2;
    }

    bool TryGetLeverGridCell(AN_Button button, out Vector2Int leverCell)
    {
        leverCell = default;

        if (button == null || !TryWorldToGridCell(button.transform.position, out Vector2Int rawCell))
            return false;

        if (IsLeverCell(rawCell))
        {
            leverCell = rawCell;
            return true;
        }

        return TryFindNearestLeverCell(rawCell, LeverCellSearchRadius, out leverCell);
    }

    bool TryFindNearestLeverCell(Vector2Int startCell, int maxRadius, out Vector2Int leverCell)
    {
        leverCell = default;

        if (!IsInBounds(startCell) || maxRadius <= 0)
            return false;

        int bestDistance = int.MaxValue;
        bool found = false;

        for (int dy = -maxRadius; dy <= maxRadius; dy++)
        {
            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            {
                Vector2Int candidate = new Vector2Int(startCell.x + dx, startCell.y + dy);
                if (!IsLeverCell(candidate))
                    continue;

                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                leverCell = candidate;
                found = true;
            }
        }

        return found;
    }

    AN_Button[] GetLeversWithSearchParticipant()
    {
        if (cachedParticipantLevers != null && Time.time < nextParticipantLeversRefreshTime)
            return cachedParticipantLevers;

        RemoteLeverSearchParticipant[] participants = FindObjectsByType<RemoteLeverSearchParticipant>(FindObjectsSortMode.None);
        if (participants == null || participants.Length == 0)
        {
            cachedParticipantLevers = GetAllLeverButtonsFallback();
            nextParticipantLeversRefreshTime = Time.time + LeverParticipantsRefreshInterval;
            return cachedParticipantLevers;
        }

        leverBuffer.Clear();
        uniqueLeverBuffer.Clear();

        for (int i = 0; i < participants.Length; i++)
        {
            RemoteLeverSearchParticipant participant = participants[i];
            if (participant == null)
                continue;

            if (!participant.TryGetLeverButton(out AN_Button button))
                continue;

            if (uniqueLeverBuffer.Add(button))
                leverBuffer.Add(button);
        }

        cachedParticipantLevers = leverBuffer.Count > 0 ? leverBuffer.ToArray() : GetAllLeverButtonsFallback();
        nextParticipantLeversRefreshTime = Time.time + LeverParticipantsRefreshInterval;
        return cachedParticipantLevers;
    }

    AN_Button[] GetAllLeverButtonsFallback()
    {
        AN_Button[] allButtons = FindObjectsByType<AN_Button>(FindObjectsSortMode.None);
        if (allButtons == null || allButtons.Length == 0)
            return EmptyButtons;

        leverBuffer.Clear();
        for (int i = 0; i < allButtons.Length; i++)
        {
            AN_Button button = allButtons[i];
            if (button != null && button.isLever)
                leverBuffer.Add(button);
        }

        return leverBuffer.Count > 0 ? leverBuffer.ToArray() : EmptyButtons;
    }

    IEnumerator HighlightLever(GameObject leverObject)
    {
        if (leverObject == null)
            yield break;

        if (!TryGetLeverSupportRenderers(leverObject, out Renderer[] renderers))
            yield break;

        highlightedLeverMarkerWorldPosition = GetRenderersCenter(renderers, leverObject.transform.position);
        highlightedLeverMarkerUntilTime = Time.time + LeverHighlightSeconds;

        List<HighlightState> states = new List<HighlightState>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                continue;

            HighlightState state = new HighlightState
            {
                renderer = renderer,
                originalMaterials = renderer.materials
            };

            if (highlightMaterial != null)
            {
                state.highlightMaterials = new Material[state.originalMaterials.Length];
                for (int j = 0; j < state.highlightMaterials.Length; j++)
                {
                    state.highlightMaterials[j] = new Material(highlightMaterial);
                    if (state.originalMaterials[j] != null && state.originalMaterials[j].HasProperty("_MainTex"))
                    {
                        Texture mainTex = state.originalMaterials[j].GetTexture("_MainTex");
                        if (mainTex != null)
                            state.highlightMaterials[j].SetTexture("_MainTex", mainTex);
                    }
                }
                renderer.materials = state.highlightMaterials;
            }
            else
            {
                Shader glowShader = Shader.Find("Legacy Shaders/Self-Illumin/Diffuse");
                state.highlightMaterials = new Material[state.originalMaterials.Length];
                for (int j = 0; j < state.highlightMaterials.Length; j++)
                {
                    state.highlightMaterials[j] = new Material(glowShader);
                    state.highlightMaterials[j].color = Color.yellow;
                    if (state.originalMaterials[j] != null && state.originalMaterials[j].HasProperty("_MainTex"))
                    {
                        Texture mainTex = state.originalMaterials[j].GetTexture("_MainTex");
                        if (mainTex != null)
                        {
                            state.highlightMaterials[j].SetTexture("_MainTex", mainTex);
                        }
                    }
                }
                renderer.materials = state.highlightMaterials;
            }

            states.Add(state);
        }

        yield return new WaitForSeconds(LeverHighlightSeconds);

        for (int i = 0; i < states.Count; i++)
        {
            HighlightState state = states[i];
            if (state.renderer == null)
                continue;

            state.renderer.materials = state.originalMaterials;

            for (int j = 0; j < state.highlightMaterials.Length; j++)
            {
                if (state.highlightMaterials[j] != null)
                    Destroy(state.highlightMaterials[j]);
            }
        }

        leverHighlightCoroutine = null;
    }

    bool TryGetLeverSupportRenderers(GameObject leverObject, out Renderer[] supportRenderers)
    {
        supportRenderers = Array.Empty<Renderer>();
        Transform leverTransform = leverObject.transform;

        for (Transform parent = leverTransform.parent; parent != null; parent = parent.parent)
        {
            Renderer[] parentRenderers = parent.GetComponentsInChildren<Renderer>(true);
            if (parentRenderers == null || parentRenderers.Length == 0)
                continue;

            List<Renderer> filtered = new List<Renderer>();
            for (int i = 0; i < parentRenderers.Length; i++)
            {
                Renderer renderer = parentRenderers[i];
                if (renderer == null)
                    continue;

                if (renderer.transform == leverTransform || renderer.transform.IsChildOf(leverTransform))
                    continue;

                filtered.Add(renderer);
            }

            if (filtered.Count > 0)
            {
                supportRenderers = filtered.ToArray();
                return true;
            }
        }

        supportRenderers = leverObject.GetComponentsInChildren<Renderer>(true);
        return supportRenderers != null && supportRenderers.Length > 0;
    }

    Vector3 GetRenderersCenter(Renderer[] renderers, Vector3 fallbackPosition)
    {
        if (renderers == null || renderers.Length == 0)
            return fallbackPosition;

        bool hasBounds = false;
        Bounds combinedBounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? combinedBounds.center : fallbackPosition;
    }

    void DrawLeverMarkerOnScreen()
    {
        if (Time.time > highlightedLeverMarkerUntilTime)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        if (leverMarkerStyle == null)
        {
            leverMarkerStyle = new GUIStyle(GUI.skin.label);
            leverMarkerStyle.alignment = TextAnchor.MiddleCenter;
            leverMarkerStyle.fontSize = 34;
            leverMarkerStyle.fontStyle = FontStyle.Bold;
            leverMarkerStyle.normal.textColor = Color.yellow;
        }

        Vector3 screenPoint = camera.WorldToScreenPoint(highlightedLeverMarkerWorldPosition + Vector3.up * LeverMarkerVerticalOffset);
        if (screenPoint.z <= 0f)
            return;

        float markerWidth = 320f;
        float markerHeight = 40f;
        Rect markerRect = new Rect(
            screenPoint.x - markerWidth * 0.5f,
            Screen.height - screenPoint.y - markerHeight * 0.5f,
            markerWidth,
            markerHeight);
        GUI.Label(markerRect, LeverMarkerText, leverMarkerStyle);
    }
}

public static class RemotePathfinderHelperBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupRemotePathfinderHelper()
    {
        GameObject remoteObject = GameObject.Find("Remote (1)");
        if (remoteObject == null)
            return;

        RemoteCameraDisruptor disruptor = remoteObject.GetComponent<RemoteCameraDisruptor>();
        if (disruptor != null)
            UnityEngine.Object.Destroy(disruptor);

        if (remoteObject.GetComponent<RemotePathfinderHelper>() == null)
            remoteObject.AddComponent<RemotePathfinderHelper>();
    }
}