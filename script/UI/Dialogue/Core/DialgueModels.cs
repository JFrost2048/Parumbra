using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speaker;
    public string text;
    public string emotion;   // 선택
    public List<string> tags; // 조건용
}

[Serializable]
public class CharacterProfile {
    public string speakerId;   // 일치 비교용 (없으면 speaker로 비교)
    public string displayName; // 표시에 쓸 이름(없으면 로그의 speaker 사용)
    public Sprite avatar;      // 얼굴 이미지
}


[Serializable] public class DialogueChoice {
    public string id;
    public string text;
    public List<string> requiredTags; // 조건 표시/잠금
    public string nextBlockId;
}

[Serializable] public class DialogueBlock {
    public string id;
    public List<DialogueLine> lines;
    public List<DialogueChoice> choices;

    // 🔹 블록이 끝난 후 자동으로 이동할 다음 블록 ID
    public string nextBlockId; 
}

[Serializable] public class DialogueScript {
    public string entryBlockId;
    public List<DialogueBlock> blocks;
    // 검색 편의 맵은 로딩 시 캐시
}
