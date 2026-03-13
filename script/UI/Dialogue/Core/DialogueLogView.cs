using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueLogView : MonoBehaviour
{
    [Header("UI")]
    public RectTransform contentRoot;
    public ScrollRect scrollRect;

    [Header("Prefabs")]
    [Tooltip("과거 호환: 비워두면 아래 lineItemPrefab을 사용")]
    public GameObject logItemPrefab;          // (legacy)
    public GameObject lineItemPrefab;         // 일반 대사
    public GameObject choiceItemPrefab;       // ▶ 선택지
    public GameObject narrationItemPrefab;    // 나레이션(옵션)
    public GameObject systemItemPrefab;       // 시스템(옵션)

    [Header("Profiles")]
    public List<CharacterProfile> profiles = new();

    [Header("Styling")]
    public Sprite narrationAvatar;    // (옵션)
    public bool autoScrollToBottom = true;
    [Tooltip("선택지 뒤에는 화자 단락을 끊어서 다음 줄을 첫 줄로 간주")]
    public bool breakSpeakerAfterChoice = true;

    DialogueLog _log;

    // 타입별 풀 (line/choice/narration/system)
    readonly Dictionary<string, List<GameObject>> _poolByType = new();

    public void Bind(DialogueLog log)
    {
        _log = log;
        Refresh();
        // 이벤트 방식이면: _log.OnChanged += Refresh;
    }

    public void Refresh()
    {
        if (_log == null || contentRoot == null) return;

        var items = _log.Items; // IReadOnlyList<DialogueLogEntry>

        // 1) 전 타입 풀을 잠시 전부 비활성화
        foreach (var kv in _poolByType)
        {
            foreach (var go in kv.Value)
                if (go) go.SetActive(false);
        }

        string prevSpeaker = null;
        // 타입별 사용 카운터
        var typeIndex = new Dictionary<string, int>();

        for (int i = 0; i < items.Count; i++)
        {
            var e = items[i];
            string t = NormalizeType(e.type); // "line"/"choice"/"narration"/"system"
            if (!typeIndex.ContainsKey(t)) typeIndex[t] = 0;

            var go = GetOrCreateItem(t, typeIndex[t]++);
            go.SetActive(true);

            // 공통 바인딩(프리팁 경로 동일 가정)
            var avatarImg = go.transform.Find("Row/Left/Avatar")?.GetComponent<Image>();
            var nameText  = go.transform.Find("Row/Right/Name")?.GetComponent<TextMeshProUGUI>();
            var bodyText  = go.transform.Find("Row/Right/Bubble/BodyText")?.GetComponent<TextMeshProUGUI>();

            if (bodyText) bodyText.text = e.text ?? "";

            bool isNarration = (t == "narration") || string.IsNullOrEmpty(e.speaker);
            bool isChoice    = (t == "choice");

            // 선택지: 이름/아바타 숨김(프리팹에 있어도 숨김)
            if (isChoice)
            {
                if (nameText)  nameText.gameObject.SetActive(false);
                if (avatarImg) avatarImg.enabled = false;

                // 선택지 뒤에는 화자 단락을 끊어 다음 줄이 '첫 줄'로 보이게
                if (breakSpeakerAfterChoice) prevSpeaker = null;
                continue;
            }

            // 나레이션: 이름/아바타 숨기고(아이콘 원하면 사용), 화자 연속성에는 참여하지 않음
            if (isNarration)
            {
                if (nameText)  nameText.gameObject.SetActive(false);
                if (avatarImg)
                {
                    if (narrationAvatar != null)
                    {
                        avatarImg.enabled = true;
                        avatarImg.sprite = narrationAvatar;
                    }
                    else avatarImg.enabled = false;
                }
                // prevSpeaker 변경 없음
                continue;
            }

            // 일반 대사: 같은 화자면 이름/아바타 숨김
            string curSpeaker = e.speaker ?? "";
            bool isFirstOfSpeaker = (i == 0) || curSpeaker != prevSpeaker;

            if (nameText)
            {
                nameText.gameObject.SetActive(isFirstOfSpeaker);
                if (isFirstOfSpeaker) nameText.text = curSpeaker;
            }

            if (avatarImg)
            {
                avatarImg.enabled = isFirstOfSpeaker;

                // 스피커별 아바타 매핑
                var prof = FindProfile(e);
                if (isFirstOfSpeaker && prof != null && prof.avatar != null)
                    avatarImg.sprite = prof.avatar;
            }

            prevSpeaker = curSpeaker;
        }

        // 맨 아래로 스크롤
        if (autoScrollToBottom && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // ===== Helpers =====
    GameObject GetOrCreateItem(string typeKey, int indexOfType)
    {
        if (!_poolByType.TryGetValue(typeKey, out var list))
        {
            list = new List<GameObject>();
            _poolByType[typeKey] = list;
        }

        // 필요 개수만큼 생성
        while (list.Count <= indexOfType)
        {
            var prefab = GetPrefab(typeKey);
            var go = Instantiate(prefab, contentRoot);
            go.name = $"{typeKey}_Item_{list.Count}";
            list.Add(go);
        }
        return list[indexOfType];
    }

    GameObject GetPrefab(string typeKey)
    {
        switch (typeKey)
        {
            case "choice":    return choiceItemPrefab != null    ? choiceItemPrefab    : (lineItemPrefab ?? logItemPrefab);
            case "narration": return narrationItemPrefab != null ? narrationItemPrefab : (lineItemPrefab ?? logItemPrefab);
            case "system":    return systemItemPrefab != null    ? systemItemPrefab    : (lineItemPrefab ?? logItemPrefab);
            default:          return lineItemPrefab != null       ? lineItemPrefab      : (logItemPrefab ?? choiceItemPrefab ?? narrationItemPrefab ?? systemItemPrefab);
        }
    }

    string NormalizeType(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "line";
        raw = raw.ToLowerInvariant();
        if (raw == "choice" || raw == "narration" || raw == "system" || raw == "line")
            return raw;
        return "line";
    }

    CharacterProfile FindProfile(DialogueLogEntry e)
    {
        if (!string.IsNullOrEmpty(e.speakerId))
        {
            var p = profiles.Find(x => x.speakerId == e.speakerId);
            if (p != null) return p;
        }
        if (!string.IsNullOrEmpty(e.speaker))
        {
            var p = profiles.Find(x => x.displayName == e.speaker);
            if (p != null) return p;
        }
        return null;
    }
}
