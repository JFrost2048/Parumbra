using UnityEngine;

public class UIManager : MonoBehaviour
{
    // 특정 창을 토글하는 범용 함수
    public void ToggleWindow(GameObject targetWindow)
    {
        if (targetWindow != null)
        {
            targetWindow.SetActive(!targetWindow.activeSelf);
        }
    }

    // 특정 창을 강제로 켜기
    public void OpenWindow(GameObject targetWindow)
    {
        if (targetWindow != null)
        {
            targetWindow.SetActive(true);

        }
    }

    // 특정 창을 강제로 끄기
    public void CloseWindow(GameObject targetWindow)
    {
        if (targetWindow != null)
        {
            targetWindow.SetActive(false);
        }
    }


}
