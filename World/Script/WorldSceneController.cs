using UnityEngine;

public class WorldSceneController : MonoBehaviour
{
    [SerializeField] private WorldMapUI mapUI;
    [SerializeField] private WorldPartyUI partyUI;

    private WorldRunGraph world;

    private void Start()
    {
        if (mapUI == null)
        {
            Debug.LogError("[WorldSceneController] mapUI is NULL (WorldMapUI 연결 필요)");
            enabled = false;
            return;
        }

        if (GameRunManager.Instance == null)
        {
            Debug.LogError("[WorldSceneController] GameRunManager.Instance is NULL. 월드 씬에 RunManager를 넣어줘.");
            enabled = false;
            return;
        }

        // 월드가 없으면 런매니저 내부 설정값으로 생성
        GameRunManager.Instance.EnsureWorldGraphInitialized();

        world = GameRunManager.Instance.World;

        if (world == null)
        {
            Debug.LogError("[WorldSceneController] World 생성 실패");
            enabled = false;
            return;
        }

        Debug.Log($"[WorldSceneController] current={world.currentRoomId}");

        mapUI.Build();
        mapUI.BindWorld(world);

        if (partyUI != null && GameRunManager.Instance != null)
        {
            partyUI.Refresh(GameRunManager.Instance.CurrentParty);
        }
    }
}