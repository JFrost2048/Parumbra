public interface IDialogueSource {
    // 파일/번들/서버 등에서 스크립트 로드
    DialogueScript LoadScript(string scriptId);
    // 필요 시 핫리로드
    void Reload(string scriptId);
}
