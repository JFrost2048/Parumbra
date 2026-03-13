#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ChoiceImporter : MonoBehaviour
{
    [MenuItem("Tools/Import Choice JSON")]
    public static void ImportChoices()
    {
        string path = EditorUtility.OpenFilePanel("Select JSON File", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        var parsed = JsonUtility.FromJson<ChoiceWrapper>(WrapJson(json));

        string savePath = "Assets/Choices/";
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        foreach (var kv in parsed.custom)
        {
            string groupKey = kv.Key;
            var group = kv.Value;

            foreach (var element in group.elements)
            {
                var asset = ScriptableObject.CreateInstance<ChoiceData>();
                asset.group = group.name;
                asset.title = element.title;
                asset.description = element.text;
                asset.pointCost = ExtractPointCost(element.events);
                asset.internalTag = ExtractMainTag(element.events);
                asset.events = element.events;

                // 이미지 경로 기반 로드
                if (!string.IsNullOrEmpty(element.image))
                {
                    string assetPath = "Assets" + element.image;
                    asset.image = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                }

                AssetDatabase.CreateAsset(asset, $"{savePath}{groupKey}_{element.title}.asset");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("선택지 데이터 변환 완료!");
    }

    static int ExtractPointCost(List<ChoiceEvent> events)
    {
        foreach (var ev in events)
            if (ev.type == "setValue" && ev.target == "point")
                return int.Parse(ev.value);
        return 0;
    }

    static string ExtractMainTag(List<ChoiceEvent> events)
    {
        foreach (var ev in events)
            if (ev.type == "setValue" && ev.target == "tags" && !ev.hidden)
                return ev.value;
        return "";
    }

    // JsonUtility는 루트에 딕셔너리를 못읽어서 래핑함
    static string WrapJson(string original)
    {
        return "{ \"custom\": " + original + "}";
    }

    [System.Serializable]
    public class ChoiceWrapper
    {
        public Dictionary<string, ChoiceGroup> custom;
    }

    [System.Serializable]
    public class ChoiceGroup
    {
        public string name;
        public List<ChoiceElement> elements;
    }

    [System.Serializable]
    public class ChoiceElement
    {
        public string title;
        public string text;
        public string image;
        public List<ChoiceEvent> events;
    }
}
#endif
