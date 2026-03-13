using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldPartyCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider hpSlider;

    public void Bind(PartyMemberRuntimeData data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        var prefab = data.unitPrefab;

        if (nameText != null)
        {
            string displayName = prefab != null ? prefab.DisplayName : data.memberId;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = data.memberId;

            nameText.text = displayName;
        }

        if (portraitImage != null)
        {
            Sprite portrait = prefab != null ? prefab.PortraitSprite : null;
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            portraitImage.preserveAspect = true;
        }

        int maxHp = Mathf.Max(1, data.maxHP);
        int currentHp = Mathf.Clamp(data.currentHP, 0, maxHp);

        if (hpText != null)
            hpText.text = $"{currentHp} / {maxHp}";

        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        Debug.Log($"[WorldPartyCardUI] Bind {data.memberId}");
    }
}