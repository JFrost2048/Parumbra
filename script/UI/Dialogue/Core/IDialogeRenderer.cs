using System.Collections.Generic;

public interface IDialogueRenderer {
    void ShowLine(DialogueLine line);
    void ShowChoices(List<DialogueChoice> choices, System.Action<DialogueChoice> onSelected);
    void Clear();
}
