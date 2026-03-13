using UnityEngine;
using UnityEngine.UI;

public class PointBarUI : MonoBehaviour
{
    [Header("Data")]
    public GameStats stats;     // GameStats.asset 할당
    public int maxPoint = 30;   // 시작 최대치(필요하면 stats에 옮겨도 됨)

    [Header("UI")]
    public RectTransform barArea; // 여유 용량 패널의 RectTransform
    public Image barRemain;       // 남은 포인트 (예: 청색/시안)
    public Image barUsed;         // 사용한 포인트 (예: 회색)
    public Image barDeficit;      // 0 이하 초과분 (빨강)

    [Header("Visual")]
    [Range(0f, 1f)] public float smooth = 0.2f; // 0=즉시, 0.2~0.3 부드럽게

    float curRemain01; // 0~1 (보이는 값, 보간용)
    float curUsed01;   // 0~1
    float curDef01;    // 0~1

    void Reset()
    {
        // 편의: 자동으로 자기 RectTransform을 참조
        if (!barArea) barArea = transform as RectTransform;
    }

    void Update()
    {
        // 실제 논리값 계산
        int p = stats ? stats.point : 0;

        // remain = clamp(p, 0..max) / max
        float remain01 = Mathf.Clamp01(p / (float)maxPoint);
        // used = clamp(max - p, 0..max) / max
        float used01 = Mathf.Clamp01((maxPoint - Mathf.Clamp(p, 0, maxPoint)) / (float)maxPoint);
        // deficit = clamp(-p, 0..max) / max  (0 이하에서만)
        float def01 = Mathf.Clamp01((-p) / (float)maxPoint);

        // 부드럽게
        float t = (smooth <= 0f) ? 1f : (1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smooth)));
        curRemain01 = Mathf.Lerp(curRemain01, remain01, t);
        curUsed01 = Mathf.Lerp(curUsed01, used01, t);
        curDef01 = Mathf.Lerp(curDef01, def01, t);

        // 폭 적용
    //    ApplyBarWidth(barRemain.rectTransform, 0f, curRemain01);           // 왼쪽에서 오른쪽으로
    //    ApplyBarWidthFromRight(barUsed.rectTransform, 0f, curUsed01);      // 오른쪽에서 왼쪽으로
    //    ApplyBarWidth(barDeficit.rectTransform, 0f, curDef01);             // 오버플로는 왼쪽부터 빨강

        ApplyBarHeight(barRemain.rectTransform, curUsed01, false); // 아래에서 위로
        ApplyBarHeight(barUsed.rectTransform, curDef01, true);      // 위에서

        // 보임/숨김(투명으로 처리해도 됨)

        if (barUsed) barUsed.enabled = curUsed01 > 0.001f;
        if (barDeficit) barDeficit.enabled = curDef01 > 0.001f;



        // 기존 Update() 마지막에 추가
        if (Input.GetKeyDown(KeyCode.UpArrow)) AddPoint(+1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) AddPoint(-1);


    }

    // 왼쪽 기준(0→1)으로 폭 비율 적용
    void ApplyBarWidth(RectTransform rt, float min, float norm01)
    {
        if (!rt || !barArea) return;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(Mathf.Clamp01(norm01), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // 오른쪽 기준(1→0)으로 폭 비율 적용
    void ApplyBarWidthFromRight(RectTransform rt, float min, float norm01)
    {
        if (!rt || !barArea) return;
        float a = Mathf.Clamp01(1f - norm01);
        rt.anchorMin = new Vector2(a, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    void ApplyBarHeight(RectTransform rt, float norm01, bool fromTop)
{
    if (!rt) return;
    norm01 = Mathf.Clamp01(norm01);

    if (!fromTop)
    {
        // 아래에서 위로 차오름
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, norm01);
    }
    else
    {
        // 위에서 아래로 차오름
        rt.anchorMin = new Vector2(0f, 1f - norm01);
        rt.anchorMax = new Vector2(1f, 1f);
    }

    rt.offsetMin = Vector2.zero;
    rt.offsetMax = Vector2.zero;
}


    // 외부에서 포인트 증감 테스트용
    public void AddPoint(int v)
    {
        if (!stats) return;
        stats.point += v;
    }
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 60, 30), "+1")) AddPoint(+1);
        if (GUI.Button(new Rect(75, 10, 60, 30), "-1")) AddPoint(-1);
    }

}
