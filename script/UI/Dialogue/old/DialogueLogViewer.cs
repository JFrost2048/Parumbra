// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// using System.Text;
// using System.Collections;

// public class DialogueLogViewer : MonoBehaviour
// {
//     [SerializeField] TMP_Text logText;      // Viewport/Content/LogText
//     [SerializeField] ScrollRect scrollRect; // Scroll View의 ScrollRect
//     [SerializeField] bool scrollToBottomOnOpen = true;

//     bool justOpened = false;

//     void OnEnable()
//     {
//         justOpened = true;
//         Refresh();                // 내용 갱신
//         if (scrollToBottomOnOpen) // 창 켰을 때만 아래로 스냅(다음 프레임)
//             StartCoroutine(SnapBottomNextFrame());
//     }

//     IEnumerator SnapBottomNextFrame()
//     {
//         // 레이아웃이 다 갱신된 '다음 프레임'에 위치 설정
//         yield return null;
//         Canvas.ForceUpdateCanvases();
//         if (scrollRect) scrollRect.verticalNormalizedPosition = 0f; // 0=bottom
//         justOpened = false;
//     }

//     public void Refresh()
//     {
//         if (DialogueLog.Instance == null || logText == null) return;

//         // 사용자의 현재 위치(맨 아래에 있었는지) 기억
//         float prevNorm = scrollRect ? scrollRect.verticalNormalizedPosition : 1f;
//         bool wasAtBottom = scrollRect && prevNorm <= 0.001f;

//         // 텍스트 구성
//         var entries = DialogueLog.Instance.GetEntries();
//         var sb = new StringBuilder();
//         foreach (var e in entries)
//         {
//             sb.AppendLine("- " + e.speaker);
//             foreach (var line in e.lines) sb.AppendLine("- " + line);
//             sb.AppendLine();
//         }
//         logText.text = sb.ToString().TrimEnd();

//         // 레이아웃 강제 갱신
//         Canvas.ForceUpdateCanvases();

//         // 위치 복원 정책:
//         // - 창 막 켰을 때: 코루틴에서 한 번만 아래로 스냅
//         // - 그 외: 사용자가 맨 아래 보고 있었다면 유지(아래로), 아니라면 이전 위치 복원
//         if (scrollRect)
//         {
//             if (!justOpened)
//             {
//                 if (wasAtBottom) scrollRect.verticalNormalizedPosition = 0f;
//                 else             scrollRect.verticalNormalizedPosition = prevNorm;
//             }
//         }
//     }

//     // 필요 시 버튼에 연결해서 수동으로 아래로 가기
//     public void ScrollToBottomNow()
//     {
//         if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;
//     }
// }
