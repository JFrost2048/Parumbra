using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class JsonDialogueSource : IDialogueSource
{
    private Dictionary<string, DialogueScript> _cache = new();

    // StreamingAssets/Resources 자유롭게
    // Unity Editor에서 테스트할 때는 Application.dataPath 사용
    private string BasePath => Path.Combine(Application.streamingAssetsPath, "Dialogue");

    
    //private string BasePath => Path.Combine(Application.dataPath, "Assets/script/UI/Dialogue/Data");

    public DialogueScript LoadScript(string scriptId)
    {
        if (!System.IO.Directory.Exists(BasePath))
            System.IO.Directory.CreateDirectory(BasePath); // 폴더 자동 생성

        if (_cache.TryGetValue(scriptId, out var c)) return c;
        string path = Path.Combine(BasePath, scriptId + ".json");
        string json = File.ReadAllText(path);
        var script = JsonUtility.FromJson<DialogueScript>(json);

        // 빠른 탐색을 위해 Block 맵 캐시(원하면 Dictionary로 따로 만듦)
        _cache[scriptId] = script;
        return script;
    }

    public void Reload(string scriptId)
    {
        if (_cache.ContainsKey(scriptId)) _cache.Remove(scriptId);
        LoadScript(scriptId);
    }
}
