using UnityEngine;

public enum AIProvider { GoogleGeminiV1, GoogleGeminiV1Beta }

[CreateAssetMenu(fileName="AIConfig", menuName="Koios/AI Config")]
public class AIConfig : ScriptableObject
{
    public AIProvider provider = AIProvider.GoogleGeminiV1Beta;
    public string model = "gemini-1.5-pro";
    [TextArea] public string systemPrompt;
    public string apiKey;
    [Range(0f,2f)] public float temperature = 0.2f;
    public int maxTokens = 2048;
}
