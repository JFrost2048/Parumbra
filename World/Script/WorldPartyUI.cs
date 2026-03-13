using System.Collections.Generic;
using UnityEngine;

public class WorldPartyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform cardRoot;
    [SerializeField] private WorldPartyCardUI cardPrefab;

    private readonly List<WorldPartyCardUI> spawnedCards = new();

    public void Refresh(List<PartyMemberRuntimeData> party)
    {
        ClearCards();
        Debug.Log($"[WorldPartyUI] Refresh count = {party?.Count ?? 0}");

        if (party == null || party.Count == 0)
            return;

        for (int i = 0; i < party.Count; i++)
        {
            var member = party[i];
            if (member == null) continue;
            if (member.isDead) continue;

            var card = Instantiate(cardPrefab, cardRoot);
            card.Bind(member);
            spawnedCards.Add(card);
            Debug.Log($"[WorldPartyUI] Spawn card for {member.memberId}");
        }
    }

    private void ClearCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                Destroy(spawnedCards[i].gameObject);
        }

        spawnedCards.Clear();
    }
}