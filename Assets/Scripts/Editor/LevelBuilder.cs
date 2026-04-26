using UnityEngine;
using UnityEditor;

/// <summary>
/// Evil Doctor — Level Builder.
/// Меню: Evil Doctor ▶ Build Level
///
/// Создаёт:
///  • 3 этажа, каждый 12×12 блоков
///  • Высота этажа = 1.5 × рост персонажа (2.7 м)
///  • Белая комната 2×2 в СЗ углу 1-го этажа — точка появления игрока
///  • Лестница на 2-й этаж в середине восточной стены (напротив СЗ угла)
///  • Все поверхности здания: почти чёрные
///  • Один выход (дверь) в южной стене 1-го этажа
///  • Уличная площадка (трава) к югу от выхода, огороженная забором
///  • Персонаж (Player) с FPS-камерой, ходьба без прыжка
/// </summary>
public class LevelBuilder : UnityEditor.Editor
{
    // ── Размеры ────────────────────────────────────────────────────────────────
    const float BLDG      = 12f;
    const float PLAYER_H  = 1.8f;
    const float FLOOR_H   = PLAYER_H * 1.5f;    // = 2.7 м
    const float SLAB_T    = 0.3f;
    const float WALL_T    = 0.3f;
    const float ROOM_H    = FLOOR_H - SLAB_T;   // = 2.4 м

    const float WARD      = 2f;                  // сторона белой комнаты (2×2)
    const float YARD_D    = 20f;                 // глубина уличной площадки
    const float FENCE_H   = 2f;

    // Двери
    const float WARD_DOOR_W = 1.0f;              // дверь в белой комнате (в 2 м стене)
    const float EXIT_DOOR_W = 2.0f;              // выход из здания
    const float DOOR_H      = 2.0f;              // высота двери (< ROOM_H = 2.4)

    // Лестница — у восточной стены, центрирована по Z = 0, ступени идут на запад
    const int   STAIR_STEPS = 10;
    const float STAIR_TREAD = 0.4f;
    const float STAIR_W     = 1.5f;
    const float STAIR_RISER = FLOOR_H / 10f;    // = 0.27 м

    // ── Материалы ──────────────────────────────────────────────────────────────
    static Material s_black, s_white, s_grass, s_fence;

    // ══════════════════════════════════════════════════════════════════════════
    // Точка входа
    // ══════════════════════════════════════════════════════════════════════════

    [MenuItem("Evil Doctor/Build Level")]
    public static void BuildLevel()
    {
        ClearLevel();
        EnsureMaterials();

        BuildOutdoor(new GameObject("── OUTDOOR ──").transform);
        BuildBuilding(new GameObject("── BUILDING ──").transform);
        SetupPlayer();

        Debug.Log("<color=lime>[LevelBuilder] Уровень успешно создан!</color>");
    }

    [MenuItem("Evil Doctor/Clear Level")]
    public static void ClearLevel()
    {
        foreach (var name in new[] { "── OUTDOOR ──", "── BUILDING ──", "Player" })
        {
            var go = GameObject.Find(name);
            if (go != null) DestroyImmediate(go);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Материалы
    // ══════════════════════════════════════════════════════════════════════════

    static void EnsureMaterials()
    {
        const string dir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        s_black = GetMat(dir, "BuildingBlack", new Color(0.02f, 0.02f, 0.02f));
        s_white = GetMat(dir, "WardWhite",     Color.white, emissive: true);
        s_grass = GetMat(dir, "Grass",         new Color(0.18f, 0.48f, 0.10f));
        s_fence = GetMat(dir, "Fence",         new Color(0.15f, 0.15f, 0.15f));
    }

    static Material GetMat(string folder, string assetName, Color col, bool emissive = false)
    {
        string path = $"{folder}/{assetName}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;

        m = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            { name = assetName, color = col };

        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", col * 0.8f);
        }

        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // УЛИЦА
    // ══════════════════════════════════════════════════════════════════════════

    static void BuildOutdoor(Transform root)
    {
        float h2     = BLDG * 0.5f;
        float yardCz = -h2 - YARD_D * 0.5f;

        Box("Grass", root,
            new Vector3(0f, -SLAB_T * 0.5f, yardCz),
            new Vector3(BLDG, SLAB_T, YARD_D), s_grass);

        float fy = FENCE_H * 0.5f;
        Box("Fence_E", root, new Vector3( h2, fy, yardCz), new Vector3(WALL_T, FENCE_H, YARD_D), s_fence);
        Box("Fence_W", root, new Vector3(-h2, fy, yardCz), new Vector3(WALL_T, FENCE_H, YARD_D), s_fence);
        Box("Fence_S", root, new Vector3(0f, fy, -h2 - YARD_D),
            new Vector3(BLDG + WALL_T, FENCE_H, WALL_T), s_fence);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ЗДАНИЕ
    // ══════════════════════════════════════════════════════════════════════════

    static void BuildBuilding(Transform root)
    {
        // Этаж 1: белая комната СЗ угол + выход Ю + лестница В
        BuildFloor1(Child("Floor1", root), 0f);

        // Этаж 2: перекрытие с отверстием под лестницу + глухие стены
        BuildFloor2(Child("Floor2", root), FLOOR_H);

        // Этаж 3: обычный тёмный этаж
        BuildGenericFloor(Child("Floor3", root), 2f * FLOOR_H);

        // Крыша
        Box("Roof", root,
            new Vector3(0f, 3f * FLOOR_H - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG), s_black);
    }

    // ── Этаж 1 ────────────────────────────────────────────────────────────────

    static void BuildFloor1(Transform parent, float fy)
    {
        float h2     = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        Box("Slab", parent,
            new Vector3(0f, fy - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG), s_black);

        // Внешние стены
        Box("Wall_N", parent, new Vector3(0f,               roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f),               new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
        Box("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f),               new Vector3(WALL_T, ROOM_H, BLDG),   s_black);

        // Южная стена с единственным выходом
        WallWithDoor("Wall_S", parent,
            new Vector3(0f, fy, -h2 + WALL_T * 0.5f),
            BLDG, spanAlongX: true, s_black, EXIT_DOOR_W);

        // Белая комната в СЗ углу
        BuildWhiteWard(Child("WhiteWard", parent), fy);

        // Лестница вдоль восточной стены, центр Z = 0, ступени идут на запад
        BuildStaircase(Child("Staircase", parent), fy);
    }

    // ── Этаж 2 (перекрытие с отверстием под лестницу) ────────────────────────

    static void BuildFloor2(Transform parent, float fy)
    {
        float h2     = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;
        float slabY  = fy - SLAB_T * 0.5f;

        // Отверстие в перекрытии совпадает с проекцией лестницы на XZ
        float openX1 = h2 - WALL_T - STAIR_STEPS * STAIR_TREAD; // = 1.7  (западный край)
        float openZH = STAIR_W * 0.5f;                            // = 0.75

        // Перекрытие тремя кусками вокруг отверстия
        Box("Slab_N", parent,
            new Vector3(0f, slabY, (openZH + h2) * 0.5f),
            new Vector3(BLDG, SLAB_T, h2 - openZH), s_black);
        Box("Slab_S", parent,
            new Vector3(0f, slabY, -(openZH + h2) * 0.5f),
            new Vector3(BLDG, SLAB_T, h2 - openZH), s_black);
        Box("Slab_W", parent,
            new Vector3((-h2 + openX1) * 0.5f, slabY, 0f),
            new Vector3(openX1 + h2, SLAB_T, STAIR_W), s_black);

        // Внешние стены (глухие)
        Box("Wall_N", parent, new Vector3(0f,               roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_S", parent, new Vector3(0f,               roomCy, -h2 + WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f),               new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
        Box("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f),               new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
    }

    // ── Этаж 3 (обычный тёмный) ───────────────────────────────────────────────

    static void BuildGenericFloor(Transform parent, float fy)
    {
        float h2     = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        Box("Slab", parent,
            new Vector3(0f, fy - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG), s_black);

        Box("Wall_N", parent, new Vector3(0f,               roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_S", parent, new Vector3(0f,               roomCy, -h2 + WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f),               new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
        Box("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f),               new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
    }

    // ── Белая комната 2×2 в СЗ углу ──────────────────────────────────────────

    static void BuildWhiteWard(Transform ward, float fy)
    {
        float h2    = BLDG * 0.5f;
        // Внутренние грани внешних стен, образующие угол
        float wardX = -h2 + WALL_T;          // = -5.7 (внутренняя грань западной стены)
        float wardZ =  h2 - WALL_T;          // =  5.7 (внутренняя грань северной стены)

        // Центр интерьера белой комнаты: X=[-5.7,-3.7], Z=[3.7,5.7]
        float wardCx = wardX + WARD * 0.5f;  // = -4.7
        float wardCz = wardZ - WARD * 0.5f;  // =  4.7
        float roomCy = fy + ROOM_H * 0.5f;

        // Белый пол и потолок
        Box("Floor",   ward, new Vector3(wardCx, fy + 0.01f,          wardCz), new Vector3(WARD, 0.02f, WARD), s_white);
        Box("Ceiling", ward, new Vector3(wardCx, fy + ROOM_H - 0.01f, wardCz), new Vector3(WARD, 0.02f, WARD), s_white);

        // Тонкие белые панели на внутренних гранях внешних стен (С и З)
        Box("Wall_N_inner", ward,
            new Vector3(wardCx, roomCy, wardZ - 0.01f),
            new Vector3(WARD, ROOM_H, 0.02f), s_white);
        Box("Wall_W_inner", ward,
            new Vector3(wardX + 0.01f, roomCy, wardCz),
            new Vector3(0.02f, ROOM_H, WARD), s_white);

        // Южная перегородка (сплошная белая): внутренняя грань на Z = wardZ - WARD = 3.7
        Box("Wall_S", ward,
            new Vector3(wardCx, roomCy, wardZ - WARD - WALL_T * 0.5f),
            new Vector3(WARD, ROOM_H, WALL_T), s_white);

        // Восточная перегородка с дверью (выход на восток в основной зал)
        // Тело стены центрировано по X = wardX + WARD + WALL_T * 0.5f = -3.55
        WallWithDoor("Wall_E", ward,
            new Vector3(wardX + WARD + WALL_T * 0.5f, fy, wardCz),
            WARD, spanAlongX: false, s_white, WARD_DOOR_W);

        // Освещение
        PointLight("WardLight", ward,
            new Vector3(wardCx, fy + ROOM_H * 0.75f, wardCz),
            Color.white, range: 6f, intensity: 8f);

        // Точка появления игрока
        var sp = new GameObject("SpawnPoint");
        sp.transform.SetParent(ward);
        sp.transform.position = new Vector3(wardCx, fy + 0.1f, wardCz);
        sp.tag = "Respawn";
    }

    // ── Лестница (у восточной стены, ступени идут на запад) ──────────────────

    static void BuildStaircase(Transform parent, float fy)
    {
        float stairStartX = BLDG * 0.5f - WALL_T; // inner face of east wall = 5.7

        for (int i = 0; i < STAIR_STEPS; i++)
        {
            // Каждая ступень — сплошной блок от пола до высоты (i+1)*RISER
            float stepX = stairStartX - (i + 0.5f) * STAIR_TREAD;
            float stepH = (i + 1) * STAIR_RISER;
            float stepY = fy + stepH * 0.5f;

            Box($"Step{i + 1}", parent,
                new Vector3(stepX, stepY, 0f),
                new Vector3(STAIR_TREAD, stepH, STAIR_W), s_black);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ИГРОК
    // ══════════════════════════════════════════════════════════════════════════

    static void SetupPlayer()
    {
        var existing = GameObject.FindWithTag("Player");
        if (existing != null) DestroyImmediate(existing);

        var spawnGO = GameObject.FindWithTag("Respawn");
        Vector3 spawnPos = spawnGO != null
            ? spawnGO.transform.position
            : new Vector3(0f, 0.1f, 0f);

        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawnPos;

        var cc        = player.AddComponent<CharacterController>();
        cc.height     = PLAYER_H;
        cc.radius     = 0.35f;
        cc.center     = new Vector3(0f, PLAYER_H * 0.5f, 0f);
        cc.stepOffset = STAIR_RISER + 0.05f;   // немного выше высоты одной ступени

        var pc = player.AddComponent<PlayerController>();

        var camHolder = new GameObject("CameraHolder").transform;
        camHolder.SetParent(player.transform);
        camHolder.localPosition = new Vector3(0f, PLAYER_H * 0.9f, 0f); // высота глаз

        var so   = new SerializedObject(pc);
        var prop = so.FindProperty("cameraHolder");
        if (prop != null)
        {
            prop.objectReferenceValue = camHolder;
            so.ApplyModifiedProperties();
        }

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.SetParent(camHolder);
            mainCam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        else
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(camHolder);
            camGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }

        Debug.Log($"[LevelBuilder] Игрок создан в позиции {player.transform.position}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Вспомогательные строители
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Стена с дверным проёмом по центру.
    /// wallBottom — нижняя грань стены (Y = уровень пола).
    /// spanAlongX — стена идёт вдоль оси X (иначе вдоль Z).
    /// doorW      — ширина проёма.
    /// </summary>
    static void WallWithDoor(string name, Transform parent,
        Vector3 wallBottom, float wallWidth, bool spanAlongX, Material mat,
        float doorW = EXIT_DOOR_W)
    {
        var group = Child(name, parent);

        float sideW    = (wallWidth - doorW) * 0.5f;
        float sideCOff = (doorW + sideW) * 0.5f;
        float aboveH   = ROOM_H - DOOR_H;

        Vector3 axis      = spanAlongX ? Vector3.right : Vector3.forward;
        Vector3 sideScale = spanAlongX
            ? new Vector3(sideW,  ROOM_H, WALL_T)
            : new Vector3(WALL_T, ROOM_H, sideW);
        Vector3 aboveScale = spanAlongX
            ? new Vector3(doorW,  aboveH, WALL_T)
            : new Vector3(WALL_T, aboveH, doorW);

        Vector3 centre = wallBottom + Vector3.up * (ROOM_H * 0.5f);

        Box("Left",  group, centre - axis * sideCOff, sideScale, mat);
        Box("Right", group, centre + axis * sideCOff, sideScale, mat);

        if (aboveH > 0.01f)
        {
            float aboveY = wallBottom.y + DOOR_H + aboveH * 0.5f;
            Box("Above", group,
                new Vector3(centre.x, aboveY, centre.z), aboveScale, mat);
        }
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        if (mat != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Transform Child(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }

    static void PointLight(string name, Transform parent, Vector3 pos,
        Color col, float range, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var l       = go.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = col;
        l.range     = range;
        l.intensity = intensity;
    }
}
