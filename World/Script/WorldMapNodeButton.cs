using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapNodeButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text roomIdText;     // A1 같은 방 번호
    [SerializeField] private TMP_Text roomKindText;   // ??? / Combat / Event ...
    [SerializeField] private GameObject currentFrame;

    [NonSerialized] public string nodeId;

    public void Bind(string id, string labelText, Action<string> onClick)
    {
        nodeId = id;

        // ✅ 방 번호는 항상
        if (roomIdText != null)
            roomIdText.text = id;

        // ✅ 핵심: 종류 텍스트를 여기서 초기화 안 하면 프리팹 기본값(Room Name)이 그대로 남음
        if (roomKindText != null)
            roomKindText.text = "???";

        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Debug.Log($"[NodeButton] CLICK nodeId={nodeId}");
            onClick?.Invoke(nodeId);
        });
    }

    public void SetCurrent(bool isCurrent)
    {
        if (currentFrame) currentFrame.SetActive(isCurrent);
    }

    public void SetRoomId(string text)
    {
        if (roomIdText) roomIdText.text = text;
    }

    public void SetRoomKind(string text)
    {
        if (roomKindText) roomKindText.text = text;
    }
}