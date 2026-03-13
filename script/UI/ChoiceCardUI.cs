using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChoiceCardUI : MonoBehaviour
{
    public Image image;
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text costText;

    private ChoiceData data;

    public void Setup(ChoiceData choice)
    {
        data = choice;
        titleText.text = choice.title;
        descText.text = choice.description;
        costText.text = choice.pointCost >= 0 ? $"Cost: {choice.pointCost}" : $"Gain: {-choice.pointCost}";

        if (choice.image != null)
            image.sprite = choice.image;
    }
    public void SetData(ChoiceData data)
    {
        if (image != null) image.sprite = data.image;
        if (titleText != null) titleText.text = data.title;
        if (descText != null) descText.text = data.description;
        if (costText != null) costText.text = $"Cost : {data.pointCost}";
    }

    public void OnClick()
    {
        Debug.Log($"선택됨: {data.title}");
    }
    
}
