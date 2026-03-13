// Assets/script/UI/DialogueOrchestrator.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class DialogueOrchestrator : MonoBehaviour
{
    [Header("Plug-ins")]
    public MonoBehaviour rendererBehaviour; // DialogueView 할당
    public AIConfig aiConfig;

    private IDialogueRenderer _renderer;
    private IDialogueSource _source;
    private IAIChatProvider _ai;

    private DialogueScript _script;
    private Dictionary<string, DialogueBlock> _blocks = new();
    private DialogueBlock _current;

    private int _lineIndex = 0;
    private bool _waitingForInput = false;

    // ▼ 숫자키 선택 전용 상태
    private bool _waitingForChoice = false;
    private List<DialogueChoice> _activeChoices = null;

    [Header("Script")]
    public string scriptId = "prologue";

    public DialogueLogView logView;    // 인스펙터에 로그 패널(비활성 Panel) 드래그
    private DialogueLog _log = new DialogueLog();

    // ====== Skip Speed ======
    [Header("Skip Speed")]
    [Tooltip("S 키로 켜고 끄는 고속 진행 모드")]
    public bool isSkipMode = false;

    [Tooltip("스킵 모드에서 다음 줄로 넘어가는 간격(초). 0이면 프레임마다.")]
    [Range(0f, 1f)]
    public float skipInterval = 1f;

    [Tooltip("블록 경계에서 멈출지 여부. false면 블록을 계속 넘어감(선택지에서만 멈춤).")]
    public bool stopAtBlockBoundary = false;

    private Coroutine _skipRoutine;

    void Awake()
    {
        _renderer = (IDialogueRenderer)rendererBehaviour;
        _source = new JsonDialogueSource();
        _ai = new GeminiProvider(); // 필요 시 Factory/선택 UI로 교체
        if (logView) logView.Bind(_log);
    }

    async void Start()
    {
        LoadScript(scriptId);
        await ShowBlock(_script.entryBlockId);
    }

    void LoadScript(string id)
    {
        _script = _source.LoadScript(id);
        if (_script == null)
        {
            Debug.LogError($"[Dialogue] LoadScript('{id}') → null. 경로/JSON 확인 필요");
            return;
        }
        if (_script.blocks == null || _script.blocks.Count == 0)
        {
            Debug.LogError($"[Dialogue] '{id}'에 blocks가 비어있음. JSON에 blocks 배열이 있어야 함");
            return;
        }

        _blocks.Clear();
        foreach (var b in _script.blocks)
        {
            if (string.IsNullOrEmpty(b.id))
            {
                Debug.LogWarning("[Dialogue] blocks 항목에 id가 비어있음. 스킵");
                continue;
            }
            if (_blocks.ContainsKey(b.id))
            {
                Debug.LogWarning($"[Dialogue] 중복 block id '{b.id}' 감지. 뒤에 오는 것을 무시");
                continue;
            }
            _blocks[b.id] = b;
        }

        // entryBlockId가 비었으면 첫 블록으로 대체
        if (string.IsNullOrEmpty(_script.entryBlockId))
        {
            _script.entryBlockId = _script.blocks[0].id;
            Debug.LogWarning($"[Dialogue] entryBlockId가 비어 있어 '{_script.entryBlockId}'로 대체");
        }
    }

    // 블록 시작
    async Task ShowBlock(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) { Debug.LogError("[Dialogue] ShowBlock: id null/empty"); return; }
        id = id.Trim();

        if (!_blocks.TryGetValue(id, out _current))
        {
            Debug.LogError($"[Dialogue] 블록 '{id}' 없음. 존재 키: {string.Join(", ", _blocks.Keys)}");
            return;
        }

        _lineIndex = 0;
        _waitingForChoice = false;
        _activeChoices = null;

        await ShowNextLine();

        // 블록이 바뀐 직후 블록 경계에서 멈춰야 한다면 스킵 해제
        if (isSkipMode && stopAtBlockBoundary)
            SetSkipMode(false);
    }

    // 한 줄만 표시
    async Task ShowNextLine()
    {
        if (_current == null) return;

        if (_lineIndex >= (_current.lines?.Count ?? 0))
        {
            // 선택지로
            if (_current.choices != null && _current.choices.Count > 0)
            {
                _activeChoices = _current.choices;
                _waitingForChoice = true;

                _renderer.ShowChoices(_activeChoices, ch =>
                {
                    _ = HandleChoiceSelect(ch);
                });

                // 선택지가 뜨면 스킵 자동 해제
                if (isSkipMode) SetSkipMode(false);
            }
            else
            {
                // 다음 블록으로 자동 이동 (next가 없으면 종료)
                var nextId = (_current.nextBlockId ?? "").Trim();
                if (string.IsNullOrEmpty(nextId))
                {
                    EndDialogue();
                    // 종료 시 스킵 자동 해제
                    if (isSkipMode) SetSkipMode(false);
                }
                else
                {
                    // 블록 경계에서 멈출 옵션이면 여기서 멈춤
                    if (isSkipMode && stopAtBlockBoundary)
                    {
                        SetSkipMode(false);
                        return;
                    }
                    await ShowBlock(nextId);
                }
            }
            return;
        }

        var line = _current.lines[_lineIndex];
        _log.AddLine(null, line.speaker, line.text);
        if (logView) logView.Refresh();

        _renderer.ShowLine(line);
        _lineIndex++;
        _waitingForInput = true;
    }

    public async void ClickNext()   // ★ OnClick
    {
        if (_waitingForInput)
        {
            _waitingForInput = false;
            await ShowNextLine();
        }
    }

    // ====== Skip Speed 제어 ======
    public void ToggleSkipMode()
    {
        SetSkipMode(!isSkipMode);
    }

    public void SetSkipMode(bool on)
    {
        if (on == isSkipMode) return;
        isSkipMode = on;

        if (isSkipMode)
        {
            if (_skipRoutine != null) StopCoroutine(_skipRoutine);
            _skipRoutine = StartCoroutine(SkipLoop());
        }
        else
        {
            if (_skipRoutine != null) StopCoroutine(_skipRoutine);
            _skipRoutine = null;
        }
    }

    IEnumerator SkipLoop()
    {
        // 선택지/종료가 나오면 루프가 자동으로 해제되도록 조건 체크
        while (isSkipMode)
        {
            // 선택지 나오면 자동 종료
            if (_waitingForChoice || _current == null) { SetSkipMode(false); yield break; }

            // 입력 대기 중이면 한 줄 더 넘김
            if (_waitingForInput)
            {
                _waitingForInput = false;
                _ = ShowNextLine(); // async 호출
            }

            // 간격 조절
            if (skipInterval <= 1f) yield return null;
            else yield return new WaitForSeconds(skipInterval);
        }
    }

    // 선택지 숫자키 입력 처리 & 스킵 토글
    void Update()
    {
        // 일반 진행 (Space)
        if (_waitingForInput && Input.GetKeyDown(KeyCode.Space))
        {
            _waitingForInput = false;
            _ = ShowNextLine();
        }

        // 스킵 모드 토글: S키
        if (Input.GetKeyDown(KeyCode.S))
        {
            ToggleSkipMode();
        }

        // 숫자키로 선택 (상단 숫자열 + 키패드 1~9)
        if (_waitingForChoice && _activeChoices != null && _activeChoices.Count > 0)
        {
            int? idx = GetPressedChoiceIndex();
            if (idx.HasValue && idx.Value >= 0 && idx.Value < _activeChoices.Count)
            {
                var choice = _activeChoices[idx.Value];
                _ = HandleChoiceSelect(choice);
            }
        }
    }

    // 1~9 → 0~8 인덱스로 변환
    int? GetPressedChoiceIndex()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return 3;
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return 4;
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) return 5;
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) return 6;
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) return 7;
        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) return 8;
        return null;
    }

    // 선택 공통 처리: 버튼/숫자 입력 모두 여기로
    private async Task HandleChoiceSelect(DialogueChoice ch)
    {
        if (ch == null) return;

        var nextId = (ch.nextBlockId ?? "").Trim();

        _log.AddChoiceEcho(ch.text);
        if (logView) logView.Refresh();

        _waitingForChoice = false;
        _activeChoices = null;

        // 스킵은 선택 순간에 자동 해제(실수 방지)
        if (isSkipMode) SetSkipMode(false);

        _renderer.Clear();

        if (string.IsNullOrEmpty(nextId))
        {
            Debug.LogError("[Dialogue] nextBlockId가 비어 있습니다.");
            return;
        }

        await ShowBlock(nextId);
    }

    void EndDialogue()
    {
        Debug.Log("대화 종료");
    }

    DialogueBlock FindBlock(string id)
    {
        return _script.blocks.Find(b => b.id == id);
    }

    bool NeedsAI(DialogueLine line)
    {
        // 태그/토큰 등으로 판단. 예: tags에 "ai" 포함 시
        return line.tags != null && line.tags.Contains("ai");
    }
}
