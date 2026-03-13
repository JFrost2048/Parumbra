// using System.Collections.Generic;
// using System.Text;
// using UnityEngine;

// [System.Serializable]
// public class DialogueLogEntry {
//     public string speaker;
//     public List<string> lines = new List<string>();
//     public DialogueLogEntry(string speaker, string firstLine) {
//         this.speaker = speaker;
//         lines.Add(firstLine);
//     }
// }

// public class DialogueLog : MonoBehaviour
// {
//     public static DialogueLog Instance { get; private set; }

//     [Header("Limits")]
//     public int maxEntries = 200;
//     public int maxLinesPerEntry = 50;

//     [SerializeField] private List<DialogueLogEntry> entries = new List<DialogueLogEntry>();

//     void Awake() {
//         if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//         Instance = this;
//         // DontDestroyOnLoad(gameObject); // 전역 유지 원하면 해제
//     }

//     public void AddLine(string speaker, string text) {
//         if (string.IsNullOrWhiteSpace(text)) return;

//         if (entries.Count > 0 && entries[^1].speaker == speaker) {
//             var last = entries[^1];
//             last.lines.Add(text);
//             if (last.lines.Count > maxLinesPerEntry) last.lines.RemoveAt(0);
//         } else {
//             entries.Add(new DialogueLogEntry(speaker, text));
//             if (entries.Count > maxEntries) entries.RemoveAt(0);
//         }
//     }

//     public string BuildFormattedText() {
//         var sb = new StringBuilder();
//         foreach (var e in entries) {
//             sb.Append("- ").AppendLine(e.speaker);
//             foreach (var line in e.lines) sb.Append("- ").AppendLine(line);
//             sb.AppendLine();
//         }
//         return sb.ToString().TrimEnd();
//     }

//     public IReadOnlyList<DialogueLogEntry> GetEntries() => entries;
// }
