using UnityEditor;
using UnityEngine;
using TacticsGrid; 

[CustomEditor(typeof(GridMapGenerator))]
public class GridMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var gen = (GridMapGenerator)target;

        GUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate / Resize"))
            {
                gen.GenerateOrResize();
                EditorUtility.SetDirty(gen);
            }

            if (GUILayout.Button("Clear"))
            {
                gen.ClearAll();
                EditorUtility.SetDirty(gen);
            }
        }

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Blender 타일 프리팹 1개로 맵을 자동 생성합니다.\n" +
            "Config의 columns/rows/tileSize/spacing/origin/scale 변경 후 Generate/Resize 누르면 반영됩니다.",
            MessageType.Info
        );
    }
}
