using System.Text.RegularExpressions;

public static class PostProcessing
{
    // 모델 답변에서 JSON만 뽑거나, 없으면 원문
    public static string ExtractJsonOrText(string raw)
    {
        // ```json ... ``` 포맷, { ... } 포맷 등 처리
        var m = Regex.Match(raw, "\\{[\\s\\S]*\\}");
        return m.Success ? m.Value : raw;
    }
}
