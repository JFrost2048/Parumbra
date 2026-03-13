using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChoiceUIManager : MonoBehaviour
{
    [System.Serializable]
    public class ChoiceGroupSet
    {
        public string groupName; // 예: "출신"
        public List<ChoiceData> choices;
    }

    // 🔵 카테고리별 UI 참조
    public List<ChoiceGroupSet> allGroups;
    public GameObject choiceCardPrefab;

    // 🔵 공통 UI 참조    
    public Transform contentParent; // → 카드들 들어갈 위치
    public TMP_Text subtitleText; // → 상단 제목

    private List<GameObject> spawnedCards = new();
    private void Start()
    {

    }


    public void ShowGroup(string groupName)
    {
        // 1. 기존 카드 제거
        foreach (var obj in spawnedCards)
            Destroy(obj);
        spawnedCards.Clear();

        // 2. 그룹 찾기
        var group = allGroups.Find(g => g.groupName == groupName);
        if (group == null)
        {
            Debug.LogWarning($"그룹 '{groupName}'을 찾을 수 없습니다.");
            return;
        }

        // 3. 공통 소재목 텍스트 갱신
        subtitleText.text = $"▶ {group.groupName}";

        // 4. 카드 생성
        foreach (ChoiceData choice in group.choices)
        {
            GameObject newCard = Instantiate(choiceCardPrefab, contentParent);
            spawnedCards.Add(newCard);  // 추적용

            ChoiceCardUI cardUI = newCard.GetComponent<ChoiceCardUI>();
            if (cardUI != null)
                cardUI.Setup(choice);
        }
    }

}