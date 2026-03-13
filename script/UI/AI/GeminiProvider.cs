using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiProvider : IAIChatProvider
{
    private string BuildApiUrl(AIConfig cfg)
    {
        string baseUrl = (cfg.provider == AIProvider.GoogleGeminiV1Beta)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : "https://generativelanguage.googleapis.com/v1";
        return $"{baseUrl}/models/{cfg.model}:generateContent?key={cfg.apiKey}";
    }

    [System.Serializable] class Part { public string text; }
    [System.Serializable] class Content { public string role; public Part[] parts; }
    [System.Serializable] class GenConfig { public float temperature; public int max_output_tokens; public string response_mime_type; }
    [System.Serializable] class Request { public Content[] contents; public GenConfig generationConfig; }
    [System.Serializable] class CandidatePart { public string text; }
    [System.Serializable] class CandidateContent { public CandidatePart[] parts; }
    [System.Serializable] class Response { public CandidateContent[] candidates; }

    public async Task<string> GenerateAsync(List<(string role, string content)> messages, AIConfig cfg)
    {
        var req = new Request {
            contents = BuildContents(cfg, messages),
            generationConfig = BuildGenConfig(cfg)
        };

        string url = BuildApiUrl(cfg);
        string body = JsonUtility.ToJson(req);
        using var www = new UnityWebRequest(url, "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        var op = www.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (www.result != UnityWebRequest.Result.Success)
            throw new System.Exception($"Gemini error: {www.error}\n{www.downloadHandler.text}");

        var res = JsonUtility.FromJson<Response>(www.downloadHandler.text);
        return res?.candidates != null && res.candidates.Length > 0 && res.candidates[0].parts.Length > 0
            ? res.candidates[0].parts[0].text
            : "";
    }

    private Content[] BuildContents(AIConfig cfg, List<(string role, string content)> messages)
    {
        var list = new List<Content>();
        if (!string.IsNullOrEmpty(cfg.systemPrompt))
            list.Add(new Content { role = "user", parts = new[]{ new Part{ text = cfg.systemPrompt } }});
        foreach (var m in messages)
            list.Add(new Content { role = m.role, parts = new[]{ new Part{ text = m.content } }});
        return list.ToArray();
    }

    private GenConfig BuildGenConfig(AIConfig cfg)
    {
        var g = new GenConfig {
            temperature = cfg.temperature,
            max_output_tokens = cfg.maxTokens,
            response_mime_type = null
        };
        if (cfg.provider == AIProvider.GoogleGeminiV1Beta)
            g.response_mime_type = "application/json"; // v1beta만 JSON 모드
        return g;
    }
}
