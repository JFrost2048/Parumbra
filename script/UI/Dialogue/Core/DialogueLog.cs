using System;
using System.Collections.Generic;

[Serializable]
public class DialogueLogEntry
{
    public string speakerId;   // 동일 인물 판정용 (이름이 바뀌어도 동일 인물로 묶고 싶을 때)
    public string speaker; //화자
    public string text;  //대사
    //    public DateTime time; //기록 시간
    public string type; // "line" | "narration" | "choice" | "system"
}

public class DialogueLog
{
    public int _maxItems = 500;
    private readonly List<DialogueLogEntry> _items = new();

    public IReadOnlyList<DialogueLogEntry> Items => _items;

    public void AddLine(string speakerId, string speaker, string text, string type = "line")
    {
        _items.Add(new DialogueLogEntry
        {
            speakerId = speakerId,
            speaker = speaker,
            text = text,
            type = type
        });
        // 최대 개수 관리하는 로직이 있으면 여기에 호출(예: Trim())
    }

    public void AddNarration(string text)
    {
        AddLine(null, null, text, "narration");
    }

    public void AddChoiceEcho(string text)
    {
        AddLine(null, null, $"▶ {text}", "choice");
    }

    void TrimAndNotify()
    {
        while (_items.Count > _maxItems) _items.RemoveAt(0);
        // OnChanged?.Invoke();  // (이벤트 방식 쓴다면)
    }

    public void Clear() => _items.Clear();
}
