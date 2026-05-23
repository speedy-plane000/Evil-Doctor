using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MazeTaskLogic))]
public class MazeEditor : Editor
{
    
    int drawMode = 1;

    static readonly Color ColorWall = new Color(0.2f, 0.2f, 0.2f);
    static readonly Color ColorPath = new Color(0.85f, 0.85f, 0.85f);
    static readonly Color ColorStart = new Color(0.2f, 0.8f, 0.2f);
    static readonly Color ColorEnd = new Color(0.9f, 0.2f, 0.2f);
    static readonly Color ColorPlayer = new Color(1f, 1f, 0.2f);

    public override void OnInspectorGUI()
    {
        MazeTaskLogic maze = (MazeTaskLogic)target;

        
        DrawPropertiesExcluding(serializedObject, "mazeData");
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("— Редактор лабиринта —", EditorStyles.boldLabel);

        
        if (GUILayout.Button("Создать / сбросить сетку"))
        {
            maze.mazeData = new int[maze.mazeWidth * maze.mazeHeight];
            for (int r = 0; r < maze.mazeHeight; r++)
                for (int c = 0; c < maze.mazeWidth; c++)
                    maze.mazeData[r * maze.mazeWidth + c] =
                        (r == 0 || r == maze.mazeHeight - 1 || c == 0 || c == maze.mazeWidth - 1) ? 1 : 0;

            EditorUtility.SetDirty(maze);
        }

        if (maze.mazeData == null || maze.mazeData.Length != maze.mazeWidth * maze.mazeHeight)
        {
            EditorGUILayout.HelpBox("Нажми «Создать / сбросить сетку» чтобы начать рисовать.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Режим рисования:");
        EditorGUILayout.BeginHorizontal();
        DrawModeButton("■ Стена", 0, ColorWall);
        DrawModeButton("□ Путь", 1, ColorPath);
        DrawModeButton("S Старт", 2, ColorStart);
        DrawModeButton("E Финиш", 3, ColorEnd);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        float cellPx = Mathf.Min(28f, (EditorGUIUtility.currentViewWidth - 20f) / maze.mazeWidth);

        for (int r = 0; r < maze.mazeHeight; r++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < maze.mazeWidth; c++)
            {
                int idx = r * maze.mazeWidth + c;
                Color bg;

                if (r == maze.startPos.x && c == maze.startPos.y) bg = ColorStart;
                else if (r == maze.endPos.x && c == maze.endPos.y) bg = ColorEnd;
                else if (maze.mazeData[idx] == 1) bg = ColorWall;
                else bg = ColorPath;

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = bg;

                string label = (r == maze.startPos.x && c == maze.startPos.y) ? "S"
                             : (r == maze.endPos.x && c == maze.endPos.y) ? "E"
                             : "";

                if (GUILayout.Button(label, GUILayout.Width(cellPx), GUILayout.Height(cellPx)))
                {
                    ApplyDraw(maze, r, c, idx);
                    EditorUtility.SetDirty(maze);
                }

                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "S = старт (зелёный)   E = финиш (красный)\n■ = стена   □ = путь",
            MessageType.None);
    }

    void ApplyDraw(MazeTaskLogic maze, int r, int c, int idx)
    {
        switch (drawMode)
        {
            case 0: 
                if (!(r == maze.startPos.x && c == maze.startPos.y) &&
                    !(r == maze.endPos.x && c == maze.endPos.y))
                    maze.mazeData[idx] = 1;
                break;
            case 1: 
                maze.mazeData[idx] = 0;
                break;
            case 2: 
                maze.mazeData[maze.startPos.x * maze.mazeWidth + maze.startPos.y] = 0;
                maze.startPos = new Vector2Int(r, c);
                maze.mazeData[idx] = 0;
                break;
            case 3: 
                maze.mazeData[maze.endPos.x * maze.mazeWidth + maze.endPos.y] = 0;
                maze.endPos = new Vector2Int(r, c);
                maze.mazeData[idx] = 0;
                break;
        }
    }

    void DrawModeButton(string label, int mode, Color col)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = drawMode == mode ? col : col * 0.5f;
        if (GUILayout.Button(label))
            drawMode = mode;
        GUI.backgroundColor = prev;
    }
}