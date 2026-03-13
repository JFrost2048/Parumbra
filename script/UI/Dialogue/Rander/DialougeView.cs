using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class DialogueView : MonoBehaviour, IDialogueRenderer
{
    [Header("Refs")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;
    public Transform choicesRoot;
    public Button choiceButtonPrefab;

    private List<Button> _pool = new();

    public void ShowLine(DialogueLine line)
    {
        ClearChoicesOnly();
        if (speakerText) speakerText.text = line.speaker ?? "";
        if (bodyText) bodyText.text = line.text ?? "";
        // emotion, portrait 등은 여기서 처리
    }

    public void ShowChoices(List<DialogueChoice> choices, System.Action<DialogueChoice> onSelected)
{
    ClearChoicesOnly();

    for (int i = 0; i < choices.Count; i++)
    {
        var btn = (i < _pool.Count) ? _pool[i] : Instantiate(choiceButtonPrefab, choicesRoot);
        if (i >= _pool.Count) _pool.Add(btn);
        btn.gameObject.SetActive(true);

        var choice = choices[i]; // ★ 루프 변수 캡처 방지: 지역 변수에 고정
        btn.GetComponentInChildren<TextMeshProUGUI>().text = choice.text;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onSelected?.Invoke(choice)); // ★ 객체 자체 전달
    }
}


    public void Clear() {
        if (speakerText) speakerText.text = "";
        if (bodyText) bodyText.text = "";
        ClearChoicesOnly();
    }

    private void ClearChoicesOnly() {
        foreach (var b in _pool) b.gameObject.SetActive(false);
    }
}
