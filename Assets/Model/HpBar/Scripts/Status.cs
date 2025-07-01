using UnityEngine;
using UnityEngine.Events;

public class Status : MonoBehaviour
{
    #region Variables
    // 최대 체력
    [SerializeField] private float maxHealth;
    public float MaxHealth
    {
        get { return maxHealth; }
        private set { maxHealth = value; }
    }
    // 현재 체력
    [SerializeField] private float currentHealth;
    public float CurrentHealth
    {
        get { return currentHealth; }
        private set
        {
            currentHealth = value;

            //죽음 처리
            if (currentHealth <= 0)
            {
                IsDeath = true;
            }
        }
    }
    // 죽음여부
    private bool isDeath = false;
    public bool IsDeath
    {
        get { return isDeath; }
        private set
        {
            isDeath = value;
            //애니메이션
            //animator.SetBool(AnimationString.IsDeath, value);
        }
    }

    // 마나
    [SerializeField] private float maxMana;
    public float MaxMana
    {
        get { return maxMana; }
        private set { maxMana = value; }
    }

    [SerializeField] private float currentMana;
    public float CurrentMana
    {
        get { return currentMana; }
        private set
        {
            currentMana = value;

            // 마나 부족
            if (currentMana <= 0)
            {

            }
        }
    }

    // 현재 방어력
    [SerializeField] private float currentArmor;
    public float CurrentArmor
    {
        get { return currentArmor; }
        private set
        {
            currentArmor = value;
        }
    }

    [SerializeField] private float manaRegenRatio = 1f;
    public float ManaRegenRatio
    {
        get { return manaRegenRatio; }
        set
        {
            manaRegenRatio = value;
        }
    }
    [SerializeField] private float healthRegenRatio = 1f;
    public float HealthRegenRatio
    {
        get { return healthRegenRatio; }
        set
        {
            healthRegenRatio = value;
        }
    }

    // Action
    public UnityAction OnDamaged;           // 데미지를 받을 때 호출하는 이벤트
    public UnityAction OnUseMana;             // 마나를 소모할 때 호출하는 이벤트
    #endregion

    private void Start()
    {

    }

    private void Update()
    {
        //ChargeMana(ManaRegenRatio);
    }
    // 초기화
    //public void Init(TowerInfo towerInfo)
    //{
    //    SetMaxHealth(towerInfo.maxHealth);
    //    SetMaxMana(towerInfo.maxMana);
    //    SetCurrentArmor(towerInfo.armor);
    //}

    // 최대체력 세팅
    public void SetMaxHealth(float amount)
    {
        maxHealth = amount;
        CurrentHealth = maxHealth;
    }

    // 최대마나 세팅
    public void SetMaxMana(float amount)
    {
        maxMana = amount;
        CurrentMana = maxMana;
    }

    // 아머 세팅
    public void SetCurrentArmor(float amount)
    {
        currentArmor = amount;
    }

    // 아머 감소
    public void ReduceArmor(float amount)
    {
        //CurrentArmor -= amount;
    }

    // 데미지 받음
    public void TakeDamage(float damage)
    {
        // 방어력 적용 후 최종 데미지 계산 
        float mitigatedDamage = Mathf.Clamp(damage - CurrentArmor, 0, Mathf.Infinity);

        // 실질적으로 들어온 데미지 계산 및 유효성 검사
        float realDamage = Mathf.Min(CurrentHealth, mitigatedDamage);

        // 체력 감소
        CurrentHealth -= realDamage;

        // 체력이 0 이하라면 사망 처리
        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0;
            //Die();
        }
        OnDamaged.Invoke();
    }

    // 체력 회복
    public void Heal(float amount)
    {
        // 힐 적용 전 체력 저장
        float beforeHealth = CurrentHealth;

        // 힐 적용
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);

        // 실제 힐량 계산
        float realHeal = CurrentHealth - beforeHealth;
    }

    // 마나 사용
    public void UseMana(float amount)
    {
        // 소모값 유효성 검사
        amount = Mathf.Clamp(amount, 0, Mathf.Infinity);

        // 마나 소모
        CurrentMana -= amount;
        // 마나 값 유효성 검사
        CurrentMana = Mathf.Clamp(CurrentMana, 0f, maxMana);

        // 마나가 0보다 작은 경우
        if (CurrentMana <= 0f)
        {
            CurrentMana = 0;
        }
        OnUseMana.Invoke();
    }

    // 마나 재생
    private void ChargeMana(float ratio)
    {
        // 최대 마나 보다 작은 경우
        if (CurrentMana < maxMana)
        {
            CurrentMana = Mathf.Min(CurrentMana + Time.deltaTime * ratio, maxMana);
            OnUseMana.Invoke();
        }
    }

    // 마나 재생 곱연산
    //public void MultiplyManaRegenRatio(float ratio)
    //{
    //    manaRegenRatio *= ratio;
    //}

    //// 체력 재생 곱연산
    //public void MultiplyHealthRegenRatio(float ratio)
    //{
    //    healthRegenRatio *= ratio;
    //}
}
