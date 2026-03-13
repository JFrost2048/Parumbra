using UnityEngine;

namespace TacticsGrid
{
    public class BillboardToCamera : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        // 보통 전술게임은 "카메라 회전 그대로 따라가기"가 정답
        [SerializeField] private bool followCameraRotation = true;

        // 스프라이트가 원근/기울기로 눌려보이는 느낌을 더 없애고 싶으면 true
        // (카메라가 아래로 기울어져 있어도 캐릭터는 완전 수직로 서있게)
        [SerializeField] private bool keepUpright = true;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            if (followCameraRotation)
            {
                // 1) 카메라 회전을 그대로 따라가면 "항상 화면 정면"이 된다
                transform.rotation = targetCamera.transform.rotation;

                if (keepUpright)
                {
                    // 2) 다만 카메라 pitch(위아래 기울기)까지 따라가면 눌려 보일 수 있으니,
                    //    yaw(좌우 회전)만 따라가도록 보정
                    var e = transform.eulerAngles;
                    transform.rotation = Quaternion.Euler(0f, e.y, 0f);
                }

                return;
            }

            // 혹시 모드 바꾸고 싶을 때를 위해 남겨둠(기본은 위 방식 추천)
            Vector3 dir = transform.position - targetCamera.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
