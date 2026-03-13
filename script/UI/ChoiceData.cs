using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ChoiceData", menuName = "Custom/Choice")]
public class ChoiceData : ScriptableObject
{
    public string group;         // 예: 출신, 직업 등
    public string title;
    [TextArea] public string description;
    public Sprite image;
    public int pointCost;
    public string internalTag;   // 실제 적용되는 변수 이름

    public List<ChoiceEvent> events;
}

[System.Serializable]
public class ChoiceEvent
{
    public string type;     // 예: setValue
    public string target;   // 예: tags, affection_XXX 등
    public string value;    // 예: 생존, -5 등
    public bool hidden;     // 숨겨진 이벤트인지
}
