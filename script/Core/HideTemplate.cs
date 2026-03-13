using UnityEngine;
using UnityEngine.UI;

public class ChoiceFactory : MonoBehaviour
{
    public Transform content;      // ScrollView Content
    public GameObject template;    // ChoiceCardUI (원형, 꺼두기)

    void Awake()
    {

    }

    public GameObject AddChoice(ChoiceData data)
    {
        var go = Instantiate(template, content); // 복제
        go.SetActive(true);                      // 복제본만 활성화

        // 필요하면 레이아웃 즉시 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);

        // 데이터 바인딩 예시
        // go.GetComponent<ChoiceCardUI>().Bind(data);

        return go;
    }
}
