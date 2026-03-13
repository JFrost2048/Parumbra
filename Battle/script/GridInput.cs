using UnityEngine;
using UnityEngine.EventSystems;
namespace TacticsGrid
{
    public class GridInput : MonoBehaviour
    {
        [SerializeField] private Camera cam;

        // ⚠️ 이 마스크는 "타일 레이어"를 포함해야 함.
        // 유닛 레이어까지 포함되어 있어도 RaycastAll로 Tile만 골라낼 거라 상관은 없음.
        [SerializeField] private LayerMask tileMask;

        [SerializeField] private GridController controller;

        [Header("Raycast")]
        [SerializeField] private float rayDistance = 500f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;



        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (controller == null) controller = FindFirstObjectByType<GridController>();
        }

        private void Update()
        {


            if (controller.IsBusy) return; // 바쁠 때는 입력 무시
            if (cam == null || controller == null) return;

            // 1~9로 공격 선택 (키보드 위 숫자 + 키패드 둘 다)
            for (int i = 1; i <= 9; i++)
            {
                bool down =
                    Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)) ||
                    Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + i));

                if (!down) continue;

                // ✅ 버튼 클릭과 동일한 경로로 처리 (UI 프레임 갱신 포함)
                controller.SelectAttackByIndex(i - 1);
                break;
            }

            // 공격 모드 토글
            if (Input.GetKeyDown(KeyCode.F))
            {
                controller.ToggleAttackMode();
            }


            // 공격 모드 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                controller.ExitAttackMode();

            }


            // UI 위 클릭이면 무시(선택 해제/이동 방지) - 필요하면 켜기

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (!Input.GetMouseButtonDown(0)) return;

            var ray = cam.ScreenPointToRay(Input.mousePosition);

            // 타일만 레이캐스트
            if (!Physics.Raycast(ray, out var hit, rayDistance, tileMask, triggerInteraction))
                return;

            var tile = hit.collider.GetComponentInParent<Tile>();
            if (tile == null) return;

            // ✅ 공격 모드면 선택/해제 로직 건드리지 말고 공격 시도 우선
            if (controller.IsAttackMode)
            {
                controller.OnTileClicked(tile.Coord); // 내부에서 TryAttackAt 호출
                return;
            }

            // ✅ 공격 모드가 아닐 때: 유닛이 있으면 선택(토글이 좋으면 ToggleSelectUnit 권장)
            if (controller.TryGetUnitAt(tile.Coord, out var u) && u != null)
            {
                // 같은 유닛 다시 클릭하면 선택 해제하고 싶으면
                // if (controller.Selected == u) controller.ClearSelection();
                // else controller.SelectUnit(u);

                controller.SelectUnit(u);
                return;
            }

            // ✅ 빈 타일 클릭
            if (controller.Selected != null)
            {
                // 선택된 유닛이 있으면: 이동 시도 (선택 유지)
                controller.OnTileClicked(tile.Coord);
            }
            else
            {
                // 선택된 유닛이 없으면: 그냥 비우기(이미 비었으면 변화 없음)
                controller.ClearSelection();
            }




        }

        private void Start()
        {
            Debug.Log("[GridInput] Start called");
        }


    }
}
