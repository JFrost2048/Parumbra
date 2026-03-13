
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ChoiceCsvImporter : EditorWindow
{
    [MenuItem("Tools/Import Choices from CSV")]
    public static void ImportChoicesFromCSV()
    {
        string path = EditorUtility.OpenFilePanel("CSV 파일 선택", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2) return;

        string[] headers = lines[0].Split(',');

        string outputPath = "Assets/Choices/";
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');

            ChoiceData choice = ScriptableObject.CreateInstance<ChoiceData>();
            choice.group = GetCol(headers, cols, "group");
            choice.title = GetCol(headers, cols, "title");
            choice.description = GetCol(headers, cols, "description");
            choice.image = Resources.Load<Sprite>("images/choices/" + GetCol(headers, cols, "image"));
            choice.pointCost = int.Parse(GetCol(headers, cols, "pointCost"));
            choice.internalTag = GetCol(headers, cols, "internalTag");

            choice.events = new List<ChoiceEvent>();

            for (int e = 1; e <= 5; e++)
            {
                string type = GetCol(headers, cols, $"event{e}_type");
                if (string.IsNullOrEmpty(type)) continue;

                ChoiceEvent evt = new ChoiceEvent();
                evt.type = type;
                evt.target = GetCol(headers, cols, $"event{e}_target");
                evt.value = GetCol(headers, cols, $"event{e}_value");
                evt.hidden = bool.Parse(GetCol(headers, cols, $"event{e}_hidden"));
                choice.events.Add(evt);
            }

            string assetName = $"{choice.group}_{choice.title}".Replace(" ", "_");
            AssetDatabase.CreateAsset(choice, outputPath + assetName + ".asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static string GetCol(string[] headers, string[] cols, string colName)
    {
        for (int i = 0; i < headers.Length; i++)
            if (headers[i] == colName && i < cols.Length)
                return cols[i];
        return "";
    }
}
