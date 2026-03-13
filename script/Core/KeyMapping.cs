using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

public class HotkeyHub : MonoBehaviour
{
    [Serializable]
    public class Hotkey
    {
        public string name;
        public InputActionReference action;
        public UnityEvent onPerformed;        // 한 번 눌렀을 때
        public bool allowWhileTyping = false; // 입력필드 포커스 중에도 허용할지
        public bool continuous = false;       // 길게 눌러 반복
        [Range(0.01f,1f)] public float repeatDelay = 0.3f;
        [Range(0.01f,0.5f)] public float repeatRate  = 0.08f;

        [NonSerialized] public float _nextRepeatTime;
        [NonSerialized] public bool _isHeld;
    }

    public Hotkey[] hotkeys;

    void OnEnable()
    {
        foreach (var hk in hotkeys)
        {
            if (hk.action == null) continue;
            hk.action.action.Enable();

            hk.action.action.started  += _ => hk._isHeld = true;
            hk.action.action.canceled += _ => hk._isHeld = false;

            hk.action.action.performed += ctx =>
            {
                if (!hk.allowWhileTyping && IsTyping()) return;

                // 단발
                if (!hk.continuous) hk.onPerformed?.Invoke();
                else
                {
                    // 길게 눌러 반복 시작
                    hk._nextRepeatTime = Time.unscaledTime + hk.repeatDelay;
                    hk.onPerformed?.Invoke();
                }
            };
        }
    }

    void OnDisable()
    {
        foreach (var hk in hotkeys)
        {
            if (hk.action == null) continue;
            hk.action.action.Disable();
        }
    }

    void Update()
    {
        // 길게 눌렀을 때 반복 트리거
        foreach (var hk in hotkeys)
        {
            if (!hk.continuous || !hk._isHeld) continue;
            if (!hk.allowWhileTyping && IsTyping()) continue;

            if (Time.unscaledTime >= hk._nextRepeatTime)
            {
                hk._nextRepeatTime = Time.unscaledTime + hk.repeatRate;
                hk.onPerformed?.Invoke();
            }
        }
    }

    static bool IsTyping()
    {
        var go = EventSystem.current?.currentSelectedGameObject;
        if (go == null) return false;
        return go.GetComponent<TMP_InputField>() != null || go.GetComponent<UnityEngine.UI.InputField>() != null;
    }
}
