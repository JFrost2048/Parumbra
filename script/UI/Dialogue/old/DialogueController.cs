// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using TMPro;
// using System.Text.RegularExpressions;
// using UnityEngine.Networking;
// using System.Text;

// [System.Serializable]
// public class DialogueLine { public string speaker; public string text; public string next; } // next는 선택
// [System.Serializable]
// public class DialogueData { public List<DialogueLine> lines; } // ← 소문자 lines

// // safety
// [System.Serializable] class GL_SafetySetting { public string category; public string threshold; }


// // 전송 페이로드/응답용 구조체
// [System.Serializable] class ClientPayload { public string input; public ClientState state; }
// [System.Serializable] class ClientState { public int index; public string lastSpeaker; public string lastText; }
// [System.Serializable] class SingleReply { public string reply; public string speaker; public string text; public string next; }

// // --- Gemini 응답 DTO ---
// [System.Serializable] class GeminiPart { public string text; }
// [System.Serializable] class GeminiContent { public List<GeminiPart> parts; }
// [System.Serializable] class GeminiCandidate { public GeminiContent content; }
// [System.Serializable] class GeminiResponse { public List<GeminiCandidate> candidates; }

// [System.Serializable] class GL_Part { public string text; }
// [System.Serializable] class GL_Content { public string role; public List<GL_Part> parts; }
// [System.Serializable]
// class GL_GenConfig
// {
//     public string response_mime_type;   // "application/json"
//     public float temperature;           // 0.2
//     public int max_output_tokens;       // 512~1024
// }
// // request


// [System.Serializable]
// class GL_Request
// {
//     public List<GL_Content> contents;
//     public GL_GenConfig generationConfig;
//     public List<GL_SafetySetting> safetySettings;
// }


// // response
// [System.Serializable] class GL_PartResp { public string text; }
// [System.Serializable] class GL_ContentResp { public List<GL_PartResp> parts; }
// [System.Serializable] class GL_Candidate { public GL_ContentResp content; }
// [System.Serializable] class GL_Response { public List<GL_Candidate> candidates; }

// //
// [System.Serializable] class _StringBox { public string v; }



// public class DialogueController : MonoBehaviour
// {
//     // 추가: 필드
//     [Header("Input Lock")]
//     public bool lockInputWhileSending = true;
//     public CanvasGroup inputGroup;       // 선택(있으면 흐리게 + 클릭막기)
//     public GameObject inputContainer;    // 선택(있으면 아예 숨기기)
//     public UnityEngine.Events.UnityEvent onSendStart, onSendEnd, onSendFail; // 후킹용


//     [Header("UI refs")]
//     public TextMeshProUGUI nameText;   // Name 텍스트
//     public TextMeshProUGUI bodyText;   // Script/Body 텍스트
//     public GameObject nextIndicator;   // "▶" 같은 다음표시 (선택)
//     [Header("User Input")]
//     public TMP_InputField userInput;   // 사용자가 입력할 필드
//     public UnityEngine.UI.Button sendButton; // 전송 버튼(선택)
//     bool isSending = false;
//     [Header("Data")]
//     public List<DialogueLine> lines = new List<DialogueLine>();

//     [Header("Typing")]
//     public float charsPerSecond = 40f; // 초당 글자 수
//     public bool autoFocusOnStart = true;

//     int index = -1;
//     Coroutine typingRoutine;
//     bool isTyping;

//     [Header("Load From JSON")]
//     [Tooltip("Resources 기준 경로 (예: Dialogue/intro)")]
//     public string resourcesPath;              // 예: "Dialogue/intro"
//     public TextAsset jsonSource;              // 인스펙터에 직접 넣어도 됨
//     public bool loadOnStart = true;
//     bool clickedThisFrame;
//     string currentFullText = "";
//     [Header("Load From API")]
//     public bool useApi = false;         // ★ API 사용 여부
//     public string apiUrl;               // ★ ex) https://example.com/dialogues/road_trip
//     public string bearerToken = "";     // ★ 필요 시 Authorization: Bearer 토큰
//     public float apiTimeout = 10f;      // ★ 초


//     bool loggedThisLine = false;   // 현재 라인을 로그에 기록했는지 플래그

//     [Header("Behaviour")]
//     public bool stopAtLastLine = true; // 마지막 줄에서 멈출지 여부

//     void Start()
//     {
//         if (loadOnStart)
//         {
//             if (useApi && !string.IsNullOrEmpty(apiUrl))
//                 StartCoroutine(LoadFromApi());
//             else
//                 LoadFromAssignedSource();
//         }

//         if (autoFocusOnStart) Next();
//     }
//     GL_Request BuildReq(List<GL_Content> contents)
//     {
//         // max_output_tokens는 반드시 1 이상
//         int maxTokens = (int)Mathf.Max(256, apiTimeout * 50f); // 대략값, 원하면 512~1024 고정 권장

//         return new GL_Request
//         {
//             contents = contents,
//             generationConfig = new GL_GenConfig
//             {
//                 response_mime_type = "application/json",
//                 temperature = 0.2f,
//                 max_output_tokens = maxTokens   // ★ 0 방지
//             },
//             safetySettings = new List<GL_SafetySetting> {
//             new GL_SafetySetting{ category="HARM_CATEGORY_HARASSMENT",          threshold="BLOCK_NONE" },
//             new GL_SafetySetting{ category="HARM_CATEGORY_HATE_SPEECH",         threshold="BLOCK_NONE" },
//             new GL_SafetySetting{ category="HARM_CATEGORY_SEXUALLY_EXPLICIT",   threshold="BLOCK_NONE" }, // ★ 정확한 이름
//             new GL_SafetySetting{ category="HARM_CATEGORY_DANGEROUS_CONTENT",   threshold="BLOCK_NONE" }, // ★ 정확한 이름
//             new GL_SafetySetting{ category="HARM_CATEGORY_CIVIC_INTEGRITY",     threshold="BLOCK_NONE" }
//         }
//         };
//     }

//     public void LoadFromAssignedSource()
//     {
//         string json = null;

//         if (jsonSource != null)
//         {
//             json = jsonSource.text;
//         }
//         else if (!string.IsNullOrEmpty(resourcesPath))
//         {
//             var ta = Resources.Load<TextAsset>(resourcesPath);
//             if (ta != null) json = ta.text;
//             else Debug.LogWarning($"Resources '{resourcesPath}' not found.");
//         }

//         if (!string.IsNullOrEmpty(json))
//         {
//             ApplyJson(json);
//             if(index < 0)
//                 Next(); // 새 줄이 추가됐으면 바로 표시
//         } 
//     }

//     public void ApplyJson(string json)
//     {
//         json = PrepareForJsonUtility(json);
//         var data = JsonUtility.FromJson<DialogueData>(json);
//         if (data != null && data.lines != null && data.lines.Count > 0)
//         {
//             lines = data.lines;
//             index = -1;
//             nameText.text = "";
//             bodyText.text = "";
//             Debug.Log($"[Dialogue] loaded lines={lines.Count}");
//         }
//         else
//         {
//             Debug.LogWarning("JSON 파싱 실패 또는 lines 비어있음");
//         }
//     }


//     string BuildGeminiBody_ForInitialLoad()
//     {
//         var instruction = new GL_Content
//         {
//             role = "user",
//             parts = new List<GL_Part> {
//             new GL_Part { text =
// @"반드시 JSON 객체 '한 개'만 반환:
// { ""lines"": [ { ""speaker"": string, ""text"": string, ""next"": string(optional) } ] }
// JSON 밖 텍스트 금지. 한국어." }
//         }
//         };

//         var req = BuildReq(new List<GL_Content> { instruction });
//         return JsonUtility.ToJson(req);
//     }


//     string StripJsExport(string raw)
//     {
//         // "export default { ... }" 형태를 순수 JSON 객체로 변환
//         var trimmed = raw.Trim();
//         if (trimmed.StartsWith("export default"))
//         {
//             int firstBrace = trimmed.IndexOf('{');
//             int lastBrace = trimmed.LastIndexOf('}');
//             if (firstBrace >= 0 && lastBrace > firstBrace)
//                 return trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
//         }
//         return raw;
//     }
//     void Update()
//     {
//         // 키로도 진행
//         clickedThisFrame = false;
//         if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
//         {
//             if (!clickedThisFrame) { Next(); clickedThisFrame = true; }
//         }
//     }

//     public void Next()
//     {

//         // 타이핑 중이면 스킵
//         if (isTyping)
//         {
//             FinishTypingInstant();
//             return;
//         }

//         // 끝에서 멈추기
//         if (index + 1 >= lines.Count)
//         {
//             if (stopAtLastLine)
//             {
//                 if (nextIndicator) nextIndicator.SetActive(false); // ▶ 끄기만
//                 return; // 텍스트/이름 지우지 않음
//             }
//             else
//             {
//                 // 기존 동작 유지하고 싶다면 이 분기 사용
//                 nameText.text = "";
//                 bodyText.text = "";
//                 if (nextIndicator) nextIndicator.SetActive(false);
//                 return;
//             }
//         }

//         // 다음 줄 표시
//         index++;
//         ShowLine(lines[index]);
//     }

//     void ShowLine(DialogueLine line)
//     {
//         nameText.text = line.speaker;
//         currentFullText = line.text;               // ★ 추가

//          // 새 줄 시작 → 로그에 한 번 기록
//     if (DialogueLog.Instance != null) 
//         DialogueLog.Instance.AddLine(line.speaker, line.text);

//         if (typingRoutine != null) StopCoroutine(typingRoutine);
//         typingRoutine = StartCoroutine(Typeout(currentFullText));
//     }

//     IEnumerator Typeout(string full)
//     {
//         isTyping = true;
//         if (nextIndicator) nextIndicator.SetActive(false);

//         bodyText.text = "";
//         float t = 0f;
//         int shown = 0;

//         // 리치텍스트 지원(TMP는 내부적으로 안전함)
//         while (shown < full.Length)
//         {
//             t += Time.deltaTime * charsPerSecond;
//             int target = Mathf.Clamp(Mathf.FloorToInt(t), 0, full.Length);

//             if (target != shown)
//             {
//                 // 리치텍스트 태그는 한 번에 붙도록 처리(간단 버전)
//                 bodyText.text = full.Substring(0, target);
//                 shown = target;
//             }
//             yield return null;
//         }

//         bodyText.text = full;
//         isTyping = false;
//         if (nextIndicator) nextIndicator.SetActive(true);
//     }

//     void FinishTypingInstant()
// {
//     if (typingRoutine != null)
//     {
//         StopCoroutine(typingRoutine);
//         typingRoutine = null;
//     }
//     bodyText.text = currentFullText;
//     bodyText.maxVisibleCharacters = int.MaxValue;
//     isTyping = false;
//     if (nextIndicator) nextIndicator.SetActive(true);

//     // 스킵 시에도 로그 반영 (중복 방지하려면 플래그 추가 가능)
//     if (DialogueLog.Instance != null) {
//         DialogueLog.Instance.AddLine(nameText.text, currentFullText);
//     }
// }

//     string PrepareForJsonUtility(string raw)
//     {
//         if (string.IsNullOrEmpty(raw)) return raw;

//         if (raw[0] == '\uFEFF') raw = raw.Substring(1); // BOM
//         raw = Regex.Replace(raw, @"//.*?$", "", RegexOptions.Multiline);
//         raw = Regex.Replace(raw, @"/\*.*?\*/", "", RegexOptions.Singleline);
//         raw = raw.Trim();
//         if (raw.StartsWith("[")) raw = "{\"lines\":" + raw + "}"; // 최상위 배열 허용

//         return raw;
//     }
//     string ExtractJsonFromGemini(string raw)
//     {
//         // 1) candidates 경로 우선
//         var resp = JsonUtility.FromJson<GL_Response>(raw);
//         if (resp != null && resp.candidates != null && resp.candidates.Count > 0 &&
//             resp.candidates[0].content != null &&
//             resp.candidates[0].content.parts != null &&
//             resp.candidates[0].content.parts.Count > 0)
//         {
//             raw = resp.candidates[0].content.parts[0].text;
//         }

//         if (string.IsNullOrWhiteSpace(raw)) return raw;
//         raw = raw.Trim();

//         // 2) 따옴표로 감싼 JSON이면 한 번 더 벗김
//         TryUnwrapQuotedJson(ref raw);

//         // 3) 순수 JSON 블럭만 추출(안전망)
//         var just = ExtractJsonStrict(raw);
//         return string.IsNullOrEmpty(just) ? raw : just;
//     }

//     IEnumerator SendUserInput(string inputText)
//     {
//         if (string.IsNullOrEmpty(apiUrl)) { Debug.LogWarning("[Dialogue] apiUrl 비어있음"); yield break; }
//         if (isSending) yield break;
//         isSending = true;
//         if (sendButton) sendButton.interactable = false;

//         if (lockInputWhileSending && userInput) userInput.interactable = false;
//         if (inputGroup) { inputGroup.interactable = false; inputGroup.blocksRaycasts = false; inputGroup.alpha = 0.6f; }
//         if (inputContainer) inputContainer.SetActive(false);

//         onSendStart?.Invoke();

//         var bodyJson = BuildGeminiBody_ForUserInput(inputText); // 또는 초기 로드용
//         bodyJson = EnsureTokensPositive(bodyJson);
//         var bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

//         Debug.Log($"[Dialogue] POST body: {bodyJson}"); // 바디 확인용




//         UnityWebRequest BuildGeminiRequest(string url, byte[] bodyBytes, float timeout)
//         {
//             var r = new UnityWebRequest(url, "POST");
//             r.timeout = Mathf.RoundToInt(timeout);
//             r.uploadHandler = new UploadHandlerRaw(bodyBytes) { contentType = "application/json; charset=utf-8" };
//             r.downloadHandler = new DownloadHandlerBuffer();
//             r.SetRequestHeader("Content-Type", "application/json");
//             r.SetRequestHeader("Accept", "application/json");
//             return r;
//         }

//         float? GetRetryAfter(UnityWebRequest r)
//         {
//             var ra = r.GetResponseHeader("Retry-After");
//             if (!string.IsNullOrEmpty(ra) && float.TryParse(ra, out var sec)) return sec;
//             return null;
//         }

//         int maxRetries = 4;
//         for (int attempt = 0; attempt <= maxRetries; attempt++)
//         {
//             using (var req = BuildGeminiRequest(apiUrl, bodyBytes, apiTimeout))
//             {
//                 yield return req.SendWebRequest();

// #if UNITY_2020_2_OR_NEWER
//                 bool ok = (req.result == UnityWebRequest.Result.Success) && req.responseCode < 400;
// #else
//                 bool ok = !(req.isNetworkError || req.isHttpError);
// #endif
//                 if (ok)
//                 {
//                     var raw = req.downloadHandler.text;
//                     Debug.Log($"[Dialogue] RAW response:\n{raw}");

//                     string jsonFromModel = ExtractJsonFromGemini(raw);
//                     if (string.IsNullOrWhiteSpace(jsonFromModel))
//                     {
//                         Debug.LogWarning("[Dialogue] 서버 응답에서 JSON을 찾지 못함. raw 출력:\n" + raw);
//                     }
//                     else
//                     {
//                         if (!TryApplyJsonOrFallback(jsonFromModel))
//                             Debug.LogWarning("[Dialogue] 파싱 실패 → 폴백 표시됨");
//                         else if (index < 0)
//                             Next(); // 새 줄이 추가됐으면 바로 표시

//                     }
//                     break;
//                 }

//                 long code = req.responseCode;
//                 string body = req.downloadHandler.text;

//                 if (code == 429 || code == 503)
//                 {
//                     float delay = GetRetryAfter(req) ?? Mathf.Min(20f, Mathf.Pow(2f, attempt) + Random.Range(0f, 0.5f));
//                     Debug.LogWarning($"Rate limited ({code}). Retry in {delay:F2}s (attempt {attempt + 1}/{maxRetries + 1})");
//                     yield return new WaitForSecondsRealtime(delay);
//                     continue;
//                 }

//                 Debug.LogWarning($"[Dialogue] API load failed: {req.error}\nStatus: {code}\nBody: {body}\nURL: {apiUrl}");
//                 isSending = false;
//                 if (sendButton) sendButton.interactable = true;
//                 onSendFail?.Invoke();
//                 yield break;
//             }
//         }

//         isSending = false;
//         if (sendButton) sendButton.interactable = true;
//         if (inputContainer) inputContainer.SetActive(true);
//         if (userInput) { userInput.interactable = true; userInput.ActivateInputField(); } // 캐럿 복원
//         onSendEnd?.Invoke();
//     }  // --- LoadFromApi: GET → POST + 본문 전송 + 응답 파싱 ---
//     IEnumerator LoadFromApi()
//     {
//         if (string.IsNullOrEmpty(apiUrl))
//         {
//             Debug.LogWarning("[Dialogue] apiUrl 비어있음");
//             yield break;
//         }

//         var bodyJson = BuildGeminiBody_ForInitialLoad();
//         var bodyBytes = System.Text.Encoding.UTF8.GetBytes(bodyJson);


//         // --- BuildGeminiRequest & GetRetryAfter ---
//         UnityWebRequest BuildGeminiRequest(string url, byte[] bodyBytes, float timeout)
//         {
//             var r = new UnityWebRequest(url, "POST");
//             r.timeout = Mathf.RoundToInt(timeout);
//             r.uploadHandler = new UploadHandlerRaw(bodyBytes) { contentType = "application/json; charset=utf-8" };
//             r.downloadHandler = new DownloadHandlerBuffer();
//             r.SetRequestHeader("Accept", "application/json");
//             return r;
//         }

//         float? GetRetryAfter(UnityWebRequest r)
//         {
//             var ra = r.GetResponseHeader("Retry-After");
//             if (!string.IsNullOrEmpty(ra) && float.TryParse(ra, out var sec)) return sec;
//             return null;
//         }

//         int maxRetries = 4;
//         for (int attempt = 0; attempt <= maxRetries; attempt++)
//         {
//             using (var req = BuildGeminiRequest(apiUrl, bodyBytes, apiTimeout))
//             {
//                 yield return req.SendWebRequest();

// #if UNITY_2020_2_OR_NEWER
//                 bool ok = (req.result == UnityWebRequest.Result.Success) && req.responseCode < 400;
// #else
//                 bool ok = !(req.isNetworkError || req.isHttpError);
// #endif
//                 if (ok)
//                 {
//                     var raw = req.downloadHandler.text;
//                     Debug.Log($"[Dialogue] RAW response:\n{raw}");

//                     // 통일된 파서 사용
//                     string jsonFromModel = ExtractJsonFromGemini(raw);

//                     if (!TryApplyJsonOrFallback(jsonFromModel))
//                         Debug.LogWarning("[Dialogue] 초기 로드 파싱 실패 → 폴백 표시됨");

//                     yield break; // 성공
//                 }


//                 // 실패 처리
//                 long code = req.responseCode;
//                 string body = req.downloadHandler.text;

//                 if (code == 429 || code == 503)
//                 {
//                     float delay = GetRetryAfter(req) ?? Mathf.Min(20f, Mathf.Pow(2f, attempt) + Random.Range(0f, 0.5f));
//                     Debug.LogWarning($"Rate limited ({code}). Retry in {delay:F2}s (attempt {attempt + 1}/{maxRetries + 1})");
//                     yield return new WaitForSecondsRealtime(delay);
//                     continue; // 새 req로 재시도
//                 }

//                 Debug.LogWarning($"[Dialogue] API load failed: {req.error}\nStatus: {code}\nBody: {body}\nURL: {apiUrl}");
//                 yield break;
//             }
//         }
//     }
//     // DTO는 이전에 정의한 GL_Request/GL_Content/GL_Part/GL_GenConfig 그대로 사용
//     // JSON 블록만 추출(가장 먼저 나오는 {...} 또는 [...] )
//     string ExtractJsonStrict(string raw)
//     {
//         if (string.IsNullOrEmpty(raw)) return null;
//         raw = raw.Trim();

//         // 1) 정식 candidates 경로 우선
//         var resp = JsonUtility.FromJson<GL_Response>(raw);
//         if (resp != null && resp.candidates != null && resp.candidates.Count > 0 &&
//             resp.candidates[0].content != null && resp.candidates[0].content.parts != null &&
//             resp.candidates[0].content.parts.Count > 0)
//         {
//             var t = resp.candidates[0].content.parts[0].text;
//             if (!string.IsNullOrWhiteSpace(t)) raw = t.Trim();
//         }

//         // 2) JSON만 발췌
//         int i = raw.IndexOf('{'); int j = raw.LastIndexOf('}');
//         if (i >= 0 && j > i) return raw.Substring(i, j - i + 1);

//         // 배열 케이스도 허용
//         i = raw.IndexOf('['); j = raw.LastIndexOf(']');
//         if (i >= 0 && j > i) return "{\"lines\":" + raw.Substring(i, j - i + 1) + "}";

//         return null;
//     }

//     // 최종 파싱 함수(폴백 포함)
//     bool TryApplyJsonOrFallback(string raw)
//     {
//         var json = ExtractJsonStrict(raw);
//         if (!string.IsNullOrEmpty(json))
//         {
//             ApplyJson(json); // 너의 기존 보정/파서 사용
//             return true;
//         }

//         // 폴백: 받은 전체 텍스트를 한 줄로 감싸서라도 표시
//         var safe = new DialogueData
//         {
//             lines = new List<DialogueLine>{
//             new DialogueLine{ speaker="", text=raw }
//         }
//         };
//         lines = safe.lines; index = lines.Count - 1;
//         ShowLine(lines[index]);
//         Debug.LogWarning("[Dialogue] JSON 추출 실패 → 폴백 사용");
//         return false;
//     }

//     string BuildGeminiBody_ForUserInput(string userText)
//     {
//         if (string.IsNullOrWhiteSpace(userText)) userText = "(empty)";

//         var instruction = new GL_Content
//         {
//             role = "user",
//             parts = new List<GL_Part> {
//             new GL_Part { text =
// @"아래 규칙을 따르세요.
// 반드시 JSON 객체 '한 개'만 반환:
// { ""lines"": [ { ""speaker"": string, ""text"": string, ""next"": string(optional) } ] }
// JSON 밖 텍스트 금지. 한국어." }
//         }
//         };
//         var user = new GL_Content
//         {
//             role = "user",
//             parts = new List<GL_Part> { new GL_Part { text = userText } }
//         };

//         var req = BuildReq(new List<GL_Content> { instruction, user });
//         return JsonUtility.ToJson(req);
//     }

//     string EnsureTokensPositive(string bodyJson)
//     {
//         // 아주 단순한 방어: "max_output_tokens":0 → 512 로 치환
//         if (bodyJson.Contains("\"max_output_tokens\":0"))
//             bodyJson = bodyJson.Replace("\"max_output_tokens\":0", "\"max_output_tokens\":512");
//         return bodyJson;
//     }

//     void HandleServerResponse(string json)
//     {
//         if (string.IsNullOrWhiteSpace(json)) return;

//         json = PrepareForJsonUtility(json); // 네가 이미 쓰는 보정

//         // 1) 스크립트 전체 교체: {"lines":[...]}
//         var data = JsonUtility.FromJson<DialogueData>(json);
//         if (data != null && data.lines != null && data.lines.Count > 0)
//         {
//             lines = data.lines;
//             index = -1;
//             nameText.text = "";
//             bodyText.text = "";
//             Debug.Log($"[Dialogue] server lines={lines.Count}");
//             // 바로 다음 줄부터 출력
//             Next();
//             return;
//         }

//         // 2) 한 줄만 내려줄 때: {"reply":"..."} 또는 {"speaker":"...","text":"..."}
//         var single = JsonUtility.FromJson<SingleReply>(json);
//         if (single != null && (!string.IsNullOrEmpty(single.reply) || !string.IsNullOrEmpty(single.text)))
//         {
//             var newLine = new DialogueLine
//             {
//                 speaker = string.IsNullOrEmpty(single.speaker) ? "" : single.speaker,
//                 text = !string.IsNullOrEmpty(single.reply) ? single.reply : single.text,
//                 next = single.next
//             };
//             // 현재 리스트 뒤에 붙이고 그 줄을 보여줌
//             lines.Add(newLine);
//             index = lines.Count - 1;
//             ShowLine(newLine);
//             return;
//         }

//         Debug.LogWarning("[Dialogue] 서버 응답 파싱 실패");
//     }
//     public void OnSendClicked()
//     {
//         if (isSending) return;
//         var text = userInput != null ? userInput.text : "";
//         StartCoroutine(SendUserInput(text));
//     }

//     public static class AIHttp
//     {
//         // 전역 스로틀(옵션) – 분당 허용치에 맞춰 조정: 60RPM이면 1.0f, 30RPM이면 2.0f 등
//         public static float MinInterval = 1.0f;
//         static float _lastSendTime = -999f;

//         public static IEnumerator SendWithBackoff(UnityWebRequest req, int maxRetries = 4)
//         {
//             // ---- 전역 스로틀 ----
//             float wait = (_lastSendTime < 0f) ? 0f
//                          : Mathf.Max(0f, MinInterval - (Time.realtimeSinceStartup - _lastSendTime));
//             if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

//             for (int attempt = 0; attempt <= maxRetries; attempt++)
//             {
//                 _lastSendTime = Time.realtimeSinceStartup;

//                 yield return req.SendWebRequest();

//                 bool success = req.result == UnityWebRequest.Result.Success && req.responseCode < 400;
//                 if (success) yield break; // 성공

//                 long code = req.responseCode;
//                 // 429 / 503은 일시적 – 백오프 후 재시도
//                 if (code == 429 || code == 503)
//                 {
//                     // 서버가 주는 Retry-After 헤더 우선
//                     float delay = 0f;
//                     var ra = req.GetResponseHeader("Retry-After");
//                     if (!string.IsNullOrEmpty(ra) && float.TryParse(ra, out var sec)) delay = Mathf.Max(0.5f, sec);

//                     // 없으면 지수 백오프 + 지터
//                     if (delay <= 0f)
//                         delay = Mathf.Min(20f, Mathf.Pow(2f, attempt) + Random.Range(0f, 0.5f));

//                     Debug.LogWarning($"Rate limited ({code}). Retry in {delay:F2}s (attempt {attempt + 1}/{maxRetries + 1})");
//                     yield return new WaitForSecondsRealtime(delay);
//                     continue; // 재시도
//                 }

//                 // 기타 에러는 루프 종료
//                 break;
//             }
//         }
//     }
//     static bool TryUnwrapQuotedJson(ref string s)
//     {
//         if (string.IsNullOrWhiteSpace(s)) return false;
//         s = s.Trim();

//         // 따옴표로 시작하면 JSON 문자열 리터럴로 가정
//         if (s.Length > 0 && s[0] == '\"')
//         {
//             // JsonUtility는 "그 자체" 문자열을 못 파싱하므로, 임시 객체로 감싸서 파싱
//             // ex) {"v":"{\"lines\":[...]}"}  →  box.v == {"lines":[...]}
//             try
//             {
//                 var wrapped = "{\"v\":" + s + "}";
//                 var box = JsonUtility.FromJson<_StringBox>(wrapped);
//                 if (box != null && !string.IsNullOrEmpty(box.v))
//                 {
//                     s = box.v.Trim();
//                     return true;
//                 }
//             }
//             catch { /* ignore */ }
//         }
//         return false;
//     }

// }
