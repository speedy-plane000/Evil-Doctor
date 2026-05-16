/*using UnityEngine;

public static class DigitPuzzleBootstrap
{
    const string PuzzleRootName = "DigitPuzzle_Floor3";
    const string DoorName = "DigitDoor_Floor3";
    const string PanelName = "DigitPanel_Floor3";
    const string RecognizeButtonName = "DigitRecognizeButton";
    const string ClearButtonName = "DigitClearButton";

    static readonly Vector3 DoorLocalPosition = new Vector3(-2.8f, 3.25f, 5.8f);
    static readonly Vector3 PanelLocalPosition = new Vector3(-1.2f, 3.2f, 5.4f);
    static readonly Vector3 RecognizeButtonLocalPosition = new Vector3(-1.2f, 2.2f, 5.0f);
    static readonly Vector3 ClearButtonLocalPosition = new Vector3(-1.2f, 2.2f, 5.8f);
    static readonly Vector3 ButtonLocalScale = new Vector3(0.25f, 0.1f, 0.25f);
    static readonly Color RecognizeButtonColor = new Color(0.2f, 0.7f, 0.2f);
    static readonly Color ClearButtonColor = new Color(0.7f, 0.2f, 0.2f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupDigitPuzzle()
    {
        GameObject thirdFloor = GameObject.Find("third_floor");
        if (thirdFloor == null)
        {
            Debug.LogWarning("DigitPuzzleBootstrap: third_floor object not found.");
            return;
        }

        Transform puzzleRoot = FindOrCreateChild(thirdFloor.transform, PuzzleRootName);
        puzzleRoot.localPosition = Vector3.zero;
        puzzleRoot.localRotation = Quaternion.identity;
        puzzleRoot.localScale = Vector3.one;

        AN_DoorScript door = EnsureDoor(puzzleRoot);
        DigitDrawingPanel panel = EnsurePanel(puzzleRoot);
        DigitDoorPuzzleController controller = EnsureController(puzzleRoot, door, panel);
        EnsureButtons(puzzleRoot, controller);
    }

    static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null) return child;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject EnsurePrimitiveChild(Transform parent, string childName, PrimitiveType primitiveType)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing.gameObject;

        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = childName;
        primitive.transform.SetParent(parent, false);
        return primitive;
    }

    static AN_DoorScript EnsureDoor(Transform root)
    {
        GameObject doorGo = EnsurePrimitiveChild(root, DoorName, PrimitiveType.Cube);
        Transform doorTr = doorGo.transform;
        doorTr.localPosition = DoorLocalPosition;
        doorTr.localRotation = Quaternion.Euler(0f, 90f, 0f);
        doorTr.localScale = new Vector3(0.1f, 2.2f, 2f);

        if (doorGo.GetComponent<BoxCollider>() == null) doorGo.AddComponent<BoxCollider>();

        AN_DoorScript door = doorGo.GetComponent<AN_DoorScript>();
        if (door == null) door = doorGo.AddComponent<AN_DoorScript>();
        door.DestroyOnOpen = false;
        door.Remote = true;

        return door;
    }

    static DigitDrawingPanel EnsurePanel(Transform root)
    {
        GameObject panelGo = EnsurePrimitiveChild(root, PanelName, PrimitiveType.Quad);
        Transform panelTr = panelGo.transform;
        panelTr.localPosition = PanelLocalPosition;
        panelTr.localRotation = Quaternion.Euler(0f, 90f, 0f);
        panelTr.localScale = new Vector3(1.4f, 1.4f, 1f);

        if (panelGo.GetComponent<Collider>() == null)
            panelGo.AddComponent<MeshCollider>();

        DigitDrawingPanel panel = panelGo.GetComponent<DigitDrawingPanel>();
        if (panel == null) panel = panelGo.AddComponent<DigitDrawingPanel>();

        panel.drawCamera = Camera.main;
        panel.textureWidth = 50;
        panel.textureHeight = 50;
        panel.brushSize = 3;
        panel.drawDistance = 8f;
        panel.backgroundColor = new Color32(0, 0, 0, 255);
        panel.drawColor = new Color32(255, 255, 255, 255);
        return panel;
    }

    static DigitDoorPuzzleController EnsureController(Transform root, AN_DoorScript door, DigitDrawingPanel panel)
    {
        DigitDoorPuzzleController controller = root.GetComponent<DigitDoorPuzzleController>();
        if (controller == null) controller = root.gameObject.AddComponent<DigitDoorPuzzleController>();

        controller.targetDoor = door;
        controller.drawingPanel = panel;
        controller.modelRelativePath = "digit_model.bin";
        controller.correctDigit = 4;
        return controller;
    }

    static void EnsureButtons(Transform root, DigitDoorPuzzleController controller)
    {
        EnsureButton(root, controller, RecognizeButtonName, RecognizeButtonLocalPosition, RecognizeButtonColor, DigitPuzzleButton.DigitButtonAction.Recognize);
        EnsureButton(root, controller, ClearButtonName, ClearButtonLocalPosition, ClearButtonColor, DigitPuzzleButton.DigitButtonAction.Clear);
    }

    static void EnsureButton(Transform root, DigitDoorPuzzleController controller, string name, Vector3 localPosition, Color color, DigitPuzzleButton.DigitButtonAction action)
    {
        GameObject buttonGo = EnsurePrimitiveChild(root, name, PrimitiveType.Cube);
        Transform btnTr = buttonGo.transform;
        btnTr.localPosition = localPosition;
        btnTr.localRotation = Quaternion.identity;
        btnTr.localScale = ButtonLocalScale;

        if (buttonGo.GetComponent<BoxCollider>() == null) buttonGo.AddComponent<BoxCollider>();

        MeshRenderer renderer = buttonGo.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        DigitPuzzleButton button = buttonGo.GetComponent<DigitPuzzleButton>();
        if (button == null) button = buttonGo.AddComponent<DigitPuzzleButton>();
        button.puzzleController = controller;
        button.action = action;
        button.interactionDistance = 3f;
        button.interactionKey = KeyCode.E;
    }
}*/