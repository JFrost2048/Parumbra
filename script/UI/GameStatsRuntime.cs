using UnityEngine;

public class GameStatsRuntime : MonoBehaviour
{
    public GameStats source;      // 에디터에서 에셋 할당
    [HideInInspector] public GameStats runtime; // 실행 중에만 쓰는 복제본

    void Awake()
    {
        // 플레이 시 메모리 복제 → 원본 에셋은 안전
        runtime = Instantiate(source);
    }
}
