using UnityEngine;
using UnityEditor;

/// <summary>
/// Evil Doctor — Level Builder.
/// Меню: Evil Doctor ▶ Build Level
///
/// Создаёт:
///  • Здание 3 этажа, 25×25 блоков в плане, высота 37.5 блоков (1.5 × ширина)
///  • Все поверхности здания: почти чёрные
///  • Один выход (дверь) в южной стене 1-го этажа
///  • Уличная площадка (трава) 25×20 блоков к югу от выхода, огороженная забором
///  • Белая комната 4×4 в центре 3-го этажа — точка появления игрока
///  • Персонаж (Player) с FPS-камерой
/// </summary>
public class LevelBuilder : UnityEditor.Editor
{
    // ── Размеры ────────────────────────────────────────────────────────────────
    const float BLDG    = 25f;               // сторона этажа в блоках
    const float FLOOR_H = 12.5f;             // высота одного этажа (итого 37.5 = 1.5 × 25)
    const float SLAB_T  = 0.3f;              // толщина перекрытия / крыши
    const float WALL_T  = 0.3f;              // толщина стены
    const float ROOM_H  = FLOOR_H - SLAB_T; // свободная высота комнаты = 12.2

    const float WARD    = 4f;                // сторона белой комнаты (4×4 блока)
    const float YARD_D  = 20f;               // глубина уличной площадки (в блоках)
    const float FENCE_H = 2f;                // высота забора

    const float DOOR_W  = 2f;
    const float DOOR_H  = 2.4f;

    // ── Материалы ──────────────────────────────────────────────────────────────
    static Material s_black;
    static Material s_white;
    static Material s_grass;
    static Material s_fence;

    // ══════════════════════════════════════════════════════════════════════════
    // Точка входа
    // ══════════════════════════════════════════════════════════════════════════

    [MenuItem("Evil Doctor/Build Level")]
    public static void BuildLevel()
    {
        ClearLevel();
        EnsureMaterials();

        var outdoor  = new GameObject("── OUTDOOR ──").transform;
        var building = new GameObject("── BUILDING ──").transform;

        BuildOutdoor(outdoor);
        BuildBuilding(building);
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
        float yardCz = -h2 - YARD_D * 0.5f;      // центр газона по Z

        // Газон
        Box("Grass", root,
            new Vector3(0f, -SLAB_T * 0.5f, yardCz),
            new Vector3(BLDG, SLAB_T, YARD_D),
            s_grass);

        float fenceY = FENCE_H * 0.5f;

        // Восточный забор (совпадает с восточной стеной здания)
        Box("Fence_E", root,
            new Vector3(h2, fenceY, yardCz),
            new Vector3(WALL_T, FENCE_H, YARD_D),
            s_fence);

        // Западный забор (совпадает с западной стеной здания)
        Box("Fence_W", root,
            new Vector3(-h2, fenceY, yardCz),
            new Vector3(WALL_T, FENCE_H, YARD_D),
            s_fence);

        // Южный забор (передняя граница площадки)
        Box("Fence_S", root,
            new Vector3(0f, fenceY, -h2 - YARD_D),
            new Vector3(BLDG + WALL_T, FENCE_H, WALL_T),
            s_fence);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ЗДАНИЕ
    // ══════════════════════════════════════════════════════════════════════════

    static void BuildBuilding(Transform root)
    {
        // Этаж 1: поверхность Y = 0, выход на юге
        BuildFloor1(Child("Floor1", root), 0f);

        // Этаж 2: поверхность Y = FLOOR_H, глухой
        BuildGenericFloor(Child("Floor2", root), FLOOR_H);

        // Этаж 3: поверхность Y = 2*FLOOR_H, белая комната в центре
        BuildFloor3(Child("Floor3", root), 2f * FLOOR_H);

        // Крыша
        Box("Roof", root,
            new Vector3(0f, 3f * FLOOR_H - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG),
            s_black);
    }

    // ── Этаж 1 (с дверью на юге) ──────────────────────────────────────────────

    static void BuildFloor1(Transform parent, float fy)
    {
        float h2     = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        Box("Slab", parent,
            new Vector3(0f, fy - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG),
            s_black);

        Box("Wall_N", parent, new Vector3(0f,              roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f),              new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
        Box("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f),              new Vector3(WALL_T, ROOM_H, BLDG),   s_black);

        // Южная стена с единственным выходом
        WallWithDoor("Wall_S", parent,
            new Vector3(0f, fy, -h2 + WALL_T * 0.5f),
            BLDG, spanAlongX: true, s_black);
    }

    // ── Этаж 2 (глухой) ───────────────────────────────────────────────────────

    static void BuildGenericFloor(Transform parent, float fy)
    {
        float h2     = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        Box("Slab", parent,
            new Vector3(0f, fy - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG),
            s_black);

        Box("Wall_N", parent, new Vector3(0f,              roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_S", parent, new Vector3(0f,              roomCy, -h2 + WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f),              new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
        Box("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f),              new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
    }

    // ── Этаж 3 (с белой комнатой) ─────────────────────────────────────────────

    static void BuildFloor3(Transform parent, float fy)
    {
        float h2     = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        Box("Slab", parent,
            new Vector3(0f, fy - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG),
            s_black);

        Box("Wall_N", parent, new Vector3(0f,              roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_S", parent, new Vector3(0f,              roomCy, -h2 + WALL_T * 0.5f), new Vector3(BLDG,   ROOM_H, WALL_T), s_black);
        Box("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f),              new Vector3(WALL_T, ROOM_H, BLDG),   s_black);
        Box("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f),              new Vector3(WALL_T, ROOM_H, BLDG),   s_black);

        // Белая комната 4×4 в центре этажа
        BuildWhiteWard(Child("WhiteWard", parent), fy);
    }

    // ── Белая комната 4×4 ─────────────────────────────────────────────────────

    static void BuildWhiteWard(Transform ward, float fy)
    {
        float w2     = WARD * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        // Белый пол
        Box("Floor", ward,
            new Vector3(0f, fy + 0.01f, 0f),
            new Vector3(WARD, 0.02f, WARD),
            s_white);

        // Белый потолок
        Box("Ceiling", ward,
            new Vector3(0f, fy + ROOM_H - 0.01f, 0f),
            new Vector3(WARD, 0.02f, WARD),
            s_white);

        // Белые стены: С, В, З — глухие; Ю — с дверью (выход из комнаты)
        Box("Wall_N", ward, new Vector3(0f,              roomCy,  w2 - WALL_T * 0.5f), new Vector3(WARD,   ROOM_H, WALL_T), s_white);
        Box("Wall_E", ward, new Vector3( w2 - WALL_T * 0.5f, roomCy, 0f),              new Vector3(WALL_T, ROOM_H, WARD),   s_white);
        Box("Wall_W", ward, new Vector3(-w2 + WALL_T * 0.5f, roomCy, 0f),              new Vector3(WALL_T, ROOM_H, WARD),   s_white);

        WallWithDoor("Wall_S", ward,
            new Vector3(0f, fy, -w2 + WALL_T * 0.5f),
            WARD, spanAlongX: true, s_white);

        // Яркое белое освещение комнаты
        PointLight("WardLight", ward,
            new Vector3(0f, fy + ROOM_H * 0.75f, 0f),
            Color.white, range: 10f, intensity: 8f);

        // Точка появления игрока
        var sp = new GameObject("SpawnPoint");
        sp.transform.SetParent(ward);
        sp.transform.position = new Vector3(0f, fy + 0.05f, 0f);
        sp.tag = "Respawn";
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ИГРОК
    // ══════════════════════════════════════════════════════════════════════════

    static void SetupPlayer()
    {
        var existing = GameObject.FindWithTag("Player");
        if (existing != null) DestroyImmediate(existing);

        var spawnGO  = GameObject.FindWithTag("Respawn");
        Vector3 spawnPos = spawnGO != null
            ? spawnGO.transform.position
            : new Vector3(0f, 2f * FLOOR_H + 0.1f, 0f);

        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawnPos;

        var cc    = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        var pc = player.AddComponent<PlayerController>();

        var camHolder = new GameObject("CameraHolder").transform;
        camHolder.SetParent(player.transform);
        camHolder.localPosition = new Vector3(0f, 1.6f, 0f);

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
    /// </summary>
    static void WallWithDoor(string name, Transform parent,
        Vector3 wallBottom, float wallWidth, bool spanAlongX, Material mat)
    {
        var group = Child(name, parent);

        float sideW    = (wallWidth - DOOR_W) * 0.5f;
        float sideCOff = (DOOR_W + sideW) * 0.5f;
        float aboveH   = ROOM_H - DOOR_H;

        Vector3 axis      = spanAlongX ? Vector3.right : Vector3.forward;
        Vector3 sideScale = spanAlongX
            ? new Vector3(sideW,   ROOM_H, WALL_T)
            : new Vector3(WALL_T,  ROOM_H, sideW);
        Vector3 aboveScale = spanAlongX
            ? new Vector3(DOOR_W,  aboveH, WALL_T)
            : new Vector3(WALL_T,  aboveH, DOOR_W);

        Vector3 centre = wallBottom + Vector3.up * (ROOM_H * 0.5f);

        Box("Left",  group, centre - axis * sideCOff, sideScale, mat);
        Box("Right", group, centre + axis * sideCOff, sideScale, mat);

        if (aboveH > 0.01f)
        {
            float aboveY = wallBottom.y + DOOR_H + aboveH * 0.5f;
            Box("Above", group,
                new Vector3(centre.x, aboveY, centre.z),
                aboveScale, mat);
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
