using UnityEngine;

[CreateAssetMenu(fileName="GameStats", menuName="Game/Stats")]
public class GameStats : ScriptableObject
{
    [Header("Core")]
    public int point;         // 포인트
    public int hp = 100;    // 체력
    public int maxHp = 100;

    [Header("Affection")]
    public int affection_Medeia;
    public int affection_Eileen;
    public int affection_Emma;
    // ... 필요한 만큼 추가

    // 편의 메서드
    public void AddPoint(int v) => point += v;
    public void SubPoint(int v) => point -= v;
    public void Damage(int v) { hp = Mathf.Clamp(hp - v, 0, maxHp); }
    public void Heal(int v)   { hp = Mathf.Clamp(hp + v, 0, maxHp); }
}
