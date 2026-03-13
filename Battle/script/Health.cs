using UnityEngine;
using System;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP = 10;
    public int MaxHP => maxHP;
    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    public event Action<int, int> OnChanged; // (cur, max)
    public event Action<object> OnDied;      // (source)

    [Header("Debug")]
    [SerializeField] private bool debugInput = true;
    [SerializeField] private int debugAmount = 1;

    private void Awake()
    {
        CurrentHP = maxHP;
        OnChanged?.Invoke(CurrentHP, maxHP);
    }

    private void Update()
    {
        if (!debugInput) return;
        /*

        // 체력 증감 확인용
                // 1 : 데미지
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    TakeDamage(debugAmount, this);
                }

                // 2 : 회복
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Heal(debugAmount);
                }

                // 3 : 풀회복
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    SetHP(maxHP);
                }
                */
    }

    public void SetHP(int hp)
    {
        CurrentHP = Mathf.Clamp(hp, 0, maxHP);
        OnChanged?.Invoke(CurrentHP, maxHP);
    }

    public void TakeDamage(int amount, object source = null)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Max(0, CurrentHP - Mathf.Max(0, amount));
        Debug.Log($"[{name}] took {amount} damage (HP {CurrentHP}/{maxHP})");
        OnChanged?.Invoke(CurrentHP, maxHP);

        if (CurrentHP == 0)
            OnDied?.Invoke(source);
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Min(maxHP, CurrentHP + Mathf.Max(0, amount));
        OnChanged?.Invoke(CurrentHP, maxHP);
    }
}
