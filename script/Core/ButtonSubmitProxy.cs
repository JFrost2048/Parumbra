using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSubmitProxy : MonoBehaviour
{
    public Button target;

    public void Submit()
    {
        if (target == null) target = GetComponent<Button>();
        if (target == null || !target.interactable) return;

        var es = EventSystem.current;
        if (es != null)
        {
            // 선택해주면 하이라이트/네비게이션도 자연스러움
            es.SetSelectedGameObject(target.gameObject);

            // Submit 이벤트(버튼은 ISubmitHandler로 onClick을 트리거)
            ExecuteEvents.Execute(target.gameObject,
                new BaseEventData(es),
                ExecuteEvents.submitHandler);
        }
        else
        {
            // 비상용
            target.onClick.Invoke();
        }
    }
}
