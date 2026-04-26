using UnityEngine;
using UnityEditor;

/// <summary>
/// Редакторский инструмент генерации уровня.
/// Меню: Evil Doctor ▶ Build Level
///
/// Создаёт:
///  • Белую палату на 3-м этаже (место появления игрока, яркое белое освещение)
///  • 3 этажа здания с пустым пространством для лабиринта
///  • Чекпоинты в начале каждого этажа
///  • Уличную площадку (трава) с забором
///  • Триггер победы у выхода
///  • Персонажа (Player) с FPS-камерой в точке появления
/// </summary>
public class LevelBuilder : UnityEditor.Editor
{
    // ── Размеры уровня ─────────────────────────────────────────────────────────
    const float FLOOR_H   = 4f;      // высота этажа (между поверхностями полов)
    const float SLAB_T    = 0.3f;    // толщина перекрытия
    const float WALL_T    = 0.3f;    // толщина стены
    const float ROOM_H    = FLOOR_H - SLAB_T; // свободная высота комнаты = 3.7 м

    const float BLDG      = 30f;     // сторона квадратного здания
    const float WARD_Z    = 10f;     // глубина белой палаты (северная часть 3-го этажа)

    const float OUTDOOR   = 100f;    // размер газона
    const float FENCE_R   = 45f;     // расстояние от центра до забора
    const float FENCE_H   = 2.5f;    // высота забора

    const float DOOR_W    = 2f;      // ширина двери
    const float DOOR_H    = 2.4f;    // высота двери

    // ── Материалы ─────────────────────────────────────────────────────────────
    static Material s_white;
    static Material s_dark;
    static Material s_mazeFloor;
    static Material s_grass;
    static Material s_fence;

    // ══════════════════════════════════════════════════════════════════════════
    // Точка входа — пункт меню
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
        SetupManagers();
        SetupPlayer();

        Debug.Log("<color=lime>[LevelBuilder] Уровень успешно создан!</color>");
    }

    [MenuItem("Evil Doctor/Clear Level")]
    public static void ClearLevel()
    {
        foreach (var name in new[] { "── OUTDOOR ──", "── BUILDING ──",
                                     "Player", "GameManagers" })
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

        s_white    = GetMat(dir, "WhiteWard",  Color.white,                     emissive: true);
        s_dark     = GetMat(dir, "MazeWall",   new Color(0.05f, 0.05f, 0.06f));
        s_mazeFloor= GetMat(dir, "MazeFloor",  new Color(0.07f, 0.07f, 0.08f));
        s_grass    = GetMat(dir, "Grass",       new Color(0.18f, 0.48f, 0.10f));
        s_fence    = GetMat(dir, "Fence",       new Color(0.22f, 0.22f, 0.22f));
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
            m.SetColor("_EmissionColor", col * 0.6f);
        }

        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // УЛИЦА
    // ══════════════════════════════════════════════════════════════════════════

    static void BuildOutdoor(Transform root)
    {
        // Газон (центр перекрытия ниже нулевой отметки, поверхность = Y 0)
        Box("Grass", root,
            new Vector3(0f, -SLAB_T * 0.5f, 0f),
            new Vector3(OUTDOOR, SLAB_T, OUTDOOR),
            s_grass);

        // Забор — 4 стены
        float fy  = FENCE_H * 0.5f;
        float len = FENCE_R * 2f + WALL_T;
        float r   = FENCE_R + WALL_T * 0.5f;
        Box("Fence_N", root, new Vector3(0f,  fy,  r), new Vector3(len,  FENCE_H, WALL_T), s_fence);
        Box("Fence_S", root, new Vector3(0f,  fy, -r), new Vector3(len,  FENCE_H, WALL_T), s_fence);
        Box("Fence_E", root, new Vector3( r,  fy, 0f), new Vector3(WALL_T, FENCE_H, len), s_fence);
        Box("Fence_W", root, new Vector3(-r,  fy, 0f), new Vector3(WALL_T, FENCE_H, len), s_fence);

        // Триггер победы — невидимый объём сразу за южной стеной здания
        var exit = Trigger("ExitZone", root,
            new Vector3(0f, 1.5f, -BLDG * 0.5f - 4f),
            new Vector3(6f, 3f, 3f));
        exit.AddComponent<ExitTrigger>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ЗДАНИЕ
    // ══════════════════════════════════════════════════════════════════════════

    static void BuildBuilding(Transform root)
    {
        // Этаж 1 — поверхность Y = 0
        var f1 = Child("Floor1", root);
        BuildFloor1(f1, 0f);

        // Этаж 2 — поверхность Y = 4
        var f2 = Child("Floor2", root);
        BuildGenericFloor(f2, 4f, floorNum: 2);

        // Этаж 3 — поверхность Y = 8 (содержит белую палату)
        var f3 = Child("Floor3", root);
        BuildFloor3(f3, 8f);

        // Крыша
        Box("Roof", root,
            new Vector3(0f, 3f * FLOOR_H - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG),
            s_dark);
    }

    // ── Этаж 1 (с дверью на юге для выхода на улицу) ────────────────────────

    static void BuildFloor1(Transform parent, float fy)
    {
        float h2    = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        // Перекрытие (пол)
        Box("Slab", parent,
            new Vector3(0f, fy - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG),
            s_mazeFloor);

        // Стены: С, В, З — сплошные; Ю — с дверью наружу
        SolidWall("Wall_N", parent, new Vector3(0f, roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG, ROOM_H, WALL_T), s_dark);
        SolidWall("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f), new Vector3(WALL_T, ROOM_H, BLDG), s_dark);
        SolidWall("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f), new Vector3(WALL_T, ROOM_H, BLDG), s_dark);

        WallWithDoor("Wall_S", parent,
            new Vector3(0f, fy, -h2 + WALL_T * 0.5f),
            BLDG, spanAlongX: true, s_dark);

        // Место для лабиринта
        Marker("[MAZE_FLOOR1 — разместите лабиринт здесь]", parent,
            new Vector3(0f, fy + 1f, 0f));

        // Чекпоинт у входа (возле южной стены — игрок придёт снизу)
        MakeCheckpoint("Checkpoint_F1", parent,
            new Vector3(0f, fy + 0.05f, -h2 + 3f), floorNum: 1);

        // Тусклый синеватый свет лабиринта
        PointLight("AmbientLight", parent,
            new Vector3(0f, fy + ROOM_H * 0.7f, 0f),
            new Color(0.15f, 0.15f, 0.22f), range: 40f, intensity: 0.5f);
    }

    // ── Этаж 2 (типовой) ────────────────────────────────────────────────────

    static void BuildGenericFloor(Transform parent, float fy, int floorNum)
    {
        float h2     = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        Box("Slab", parent,
            new Vector3(0f, fy - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG),
            s_mazeFloor);

        SolidWall("Wall_N", parent, new Vector3(0f, roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG, ROOM_H, WALL_T), s_dark);
        SolidWall("Wall_S", parent, new Vector3(0f, roomCy, -h2 + WALL_T * 0.5f), new Vector3(BLDG, ROOM_H, WALL_T), s_dark);
        SolidWall("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f), new Vector3(WALL_T, ROOM_H, BLDG), s_dark);
        SolidWall("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f), new Vector3(WALL_T, ROOM_H, BLDG), s_dark);

        Marker($"[MAZE_FLOOR{floorNum} — разместите лабиринт здесь]", parent,
            new Vector3(0f, fy + 1f, 0f));

        // Чекпоинт в центре этажа
        MakeCheckpoint($"Checkpoint_F{floorNum}", parent,
            new Vector3(0f, fy + 0.05f, 0f), floorNum);

        Marker($"[STAIRWELL_F{floorNum} — разместите лестницу здесь]", parent,
            new Vector3(h2 - 4f, fy + 0.1f, h2 - 4f));

        PointLight("AmbientLight", parent,
            new Vector3(0f, fy + ROOM_H * 0.7f, 0f),
            new Color(0.15f, 0.15f, 0.22f), range: 40f, intensity: 0.5f);
    }

    // ── Этаж 3 (с белой палатой на севере) ──────────────────────────────────

    static void BuildFloor3(Transform parent, float fy)
    {
        float h2     = BLDG * 0.5f;
        float roomCy = fy + ROOM_H * 0.5f;

        Box("Slab", parent,
            new Vector3(0f, fy - SLAB_T * 0.5f, 0f),
            new Vector3(BLDG, SLAB_T, BLDG),
            s_mazeFloor);

        // Внешние стены (сплошные)
        SolidWall("Wall_N", parent, new Vector3(0f, roomCy,  h2 - WALL_T * 0.5f), new Vector3(BLDG, ROOM_H, WALL_T), s_dark);
        SolidWall("Wall_S", parent, new Vector3(0f, roomCy, -h2 + WALL_T * 0.5f), new Vector3(BLDG, ROOM_H, WALL_T), s_dark);
        SolidWall("Wall_E", parent, new Vector3( h2 - WALL_T * 0.5f, roomCy, 0f), new Vector3(WALL_T, ROOM_H, BLDG), s_dark);
        SolidWall("Wall_W", parent, new Vector3(-h2 + WALL_T * 0.5f, roomCy, 0f), new Vector3(WALL_T, ROOM_H, BLDG), s_dark);

        // Белая палата (северная часть этажа)
        var ward = Child("WhiteWard", parent);
        BuildWhiteWard(ward, fy);

        // Место лабиринта (южная часть)
        Marker("[MAZE_FLOOR3 — разместите лабиринт здесь]", parent,
            new Vector3(0f, fy + 1f, -WARD_Z * 0.5f));

        // Чекпоинт у входа в зону лабиринта
        float wardEdgeZ = h2 - WARD_Z;        // Z южной стены палаты ≈ 5
        MakeCheckpoint("Checkpoint_F3", parent,
            new Vector3(0f, fy + 0.05f, wardEdgeZ - 2f), floorNum: 3);

        Marker("[STAIRWELL_F3 — разместите лестницу здесь]", parent,
            new Vector3(h2 - 4f, fy + 0.1f, -h2 + 4f));

        PointLight("MazeLight", parent,
            new Vector3(0f, fy + ROOM_H * 0.7f, -(WARD_Z * 0.5f)),
            new Color(0.15f, 0.15f, 0.22f), range: 30f, intensity: 0.5f);
    }

    // ── Белая палата ─────────────────────────────────────────────────────────

    static void BuildWhiteWard(Transform ward, float fy)
    {
        float h2       = BLDG * 0.5f;
        float wardZ0   = h2 - WARD_Z;          // Z южной стены палаты (= ~5)
        float wardZc   = h2 - WARD_Z * 0.5f;   // Z центра палаты       (= ~10)
        float roomCy   = fy + ROOM_H * 0.5f;

        // Белое покрытие пола (тонкий слой поверх перекрытия)
        Box("WardFloor", ward,
            new Vector3(0f, fy + 0.01f, wardZc),
            new Vector3(BLDG, 0.02f, WARD_Z),
            s_white);

        // Белый потолок палаты
        Box("WardCeiling", ward,
            new Vector3(0f, fy + ROOM_H - 0.01f, wardZc),
            new Vector3(BLDG, 0.02f, WARD_Z),
            s_white);

        // Белые стены палаты (восток и запад — внутри зоны палаты)
        // (север = внешняя стена здания; юг = разделяющая стена с дверью)
        SolidWall("WardWall_E", ward,
            new Vector3( h2 - WALL_T * 0.5f, roomCy, wardZc),
            new Vector3(WALL_T, ROOM_H, WARD_Z), s_white);
        SolidWall("WardWall_W", ward,
            new Vector3(-h2 + WALL_T * 0.5f, roomCy, wardZc),
            new Vector3(WALL_T, ROOM_H, WARD_Z), s_white);
        SolidWall("WardWall_N", ward,
            new Vector3(0f, roomCy, h2 - WALL_T * 0.5f),
            new Vector3(BLDG, ROOM_H, WALL_T), s_white);

        // Южная разделяющая стена с дверью (выход из палаты в лабиринт)
        WallWithDoor("WardWall_S", ward,
            new Vector3(0f, fy, wardZ0),
            BLDG, spanAlongX: true, s_white);

        // Яркое белое освещение палаты (два источника для равномерности)
        PointLight("WardLight_A", ward,
            new Vector3(-5f, fy + ROOM_H * 0.75f, wardZc),
            Color.white, range: 20f, intensity: 5f);
        PointLight("WardLight_B", ward,
            new Vector3( 5f, fy + ROOM_H * 0.75f, wardZc),
            Color.white, range: 20f, intensity: 5f);

        // Точка появления игрока (тег Respawn — встроенный Unity-тег)
        var sp = new GameObject("SpawnPoint");
        sp.transform.SetParent(ward);
        sp.transform.position = new Vector3(0f, fy + 0.05f, wardZc);
        sp.tag = "Respawn";
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Вспомогательные строители геометрии
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Стена с дверным проёмом по центру.
    /// wallBottom — нижняя точка стены (Y = уровень пола).
    /// spanAlongX — стена идёт вдоль оси X (иначе вдоль Z).
    /// </summary>
    static void WallWithDoor(string name, Transform parent,
        Vector3 wallBottom, float wallWidth, bool spanAlongX, Material mat)
    {
        var group = Child(name, parent);

        float sideW   = (wallWidth - DOOR_W) * 0.5f;
        float sideCOff = (DOOR_W + sideW) * 0.5f;   // смещение центра панели от оси
        float aboveH  = ROOM_H - DOOR_H;
        Vector3 centre = wallBottom + Vector3.up * (ROOM_H * 0.5f);

        // Ось, вдоль которой простирается стена
        Vector3 axis = spanAlongX ? Vector3.right : Vector3.forward;

        Vector3 sideScale  = spanAlongX
            ? new Vector3(sideW,   ROOM_H, WALL_T)
            : new Vector3(WALL_T,  ROOM_H, sideW);
        Vector3 aboveScale = spanAlongX
            ? new Vector3(DOOR_W,  aboveH, WALL_T)
            : new Vector3(WALL_T,  aboveH, DOOR_W);

        // Левая панель
        Box("Left",  group, centre - axis * sideCOff, sideScale,  mat);
        // Правая панель
        Box("Right", group, centre + axis * sideCOff, sideScale,  mat);
        // Над дверью
        if (aboveH > 0.01f)
        {
            Vector3 abovePos = new Vector3(centre.x,
                wallBottom.y + DOOR_H + aboveH * 0.5f,
                centre.z);
            Box("Above", group, abovePos, aboveScale, mat);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GameManagers и Player
    // ══════════════════════════════════════════════════════════════════════════

    static void SetupManagers()
    {
        if (GameObject.Find("GameManagers") != null) return;

        var root = new GameObject("GameManagers");
        root.AddComponent<GameManager>();
        root.AddComponent<CheckpointManager>();
    }

    static void SetupPlayer()
    {
        var existing = GameObject.FindWithTag("Player");
        if (existing != null) DestroyImmediate(existing);

        // Точка появления
        var spawnGO  = GameObject.FindWithTag("Respawn");
        Vector3 spawnPos = spawnGO != null
            ? spawnGO.transform.position
            : new Vector3(0f, 8.1f, 10f);

        // Корень персонажа
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawnPos;

        var cc      = player.AddComponent<CharacterController>();
        cc.height   = 1.8f;
        cc.radius   = 0.35f;
        cc.center   = new Vector3(0f, 0.9f, 0f);

        var pc = player.AddComponent<PlayerController>();

        // Держатель камеры (отвечает за вертикальный поворот)
        var camHolder = new GameObject("CameraHolder").transform;
        camHolder.SetParent(player.transform);
        camHolder.localPosition = new Vector3(0f, 1.6f, 0f);

        // Привязываем cameraHolder к PlayerController через SerializedObject
        var so   = new SerializedObject(pc);
        var prop = so.FindProperty("cameraHolder");
        if (prop != null)
        {
            prop.objectReferenceValue = camHolder;
            so.ApplyModifiedProperties();
        }

        // Используем существующую Main Camera или создаём новую
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
    // Примитивы
    // ══════════════════════════════════════════════════════════════════════════

    static void SolidWall(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        => Box(name, parent, pos, scale, mat);

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

    static GameObject Trigger(string name, Transform parent, Vector3 pos, Vector3 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var col  = go.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = true;
        return go;
    }

    static void MakeCheckpoint(string name, Transform parent, Vector3 pos, int floorNum)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var col       = go.AddComponent<CapsuleCollider>();
        col.radius    = 1.5f;
        col.height    = 0.3f;
        col.direction = 1;        // Y-axis
        col.isTrigger = true;

        var cp    = go.AddComponent<Checkpoint>();
        var cpSo  = new SerializedObject(cp);
        var cpProp = cpSo.FindProperty("floorNumber");
        if (cpProp != null)
        {
            cpProp.intValue = floorNum;
            cpSo.ApplyModifiedProperties();
        }
    }

    static void PointLight(string name, Transform parent, Vector3 pos,
        Color col, float range, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        var l         = go.AddComponent<Light>();
        l.type        = LightType.Point;
        l.color       = col;
        l.range       = range;
        l.intensity   = intensity;
    }

    static Transform Child(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }

    static void Marker(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
    }
}
