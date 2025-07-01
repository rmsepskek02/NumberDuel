using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HealthBar 의 기능 정의
/// </summary>
public class StatusUI : MonoBehaviour
{
    #region Variables
    //public Transform target;                // UI가 바라볼 타겟
    public GameObject healthBar;            // 체력바 GameObject
    public Image fillHealth;                // 체력바
    public TextMeshProUGUI healthText;      // 체력 텍스트
    public GameObject manaBar;              // 마나바 GameObject
    public Image fillMana;                  // 마나바
    public TextMeshProUGUI manaText;        // 마나 텍스트
    public GameObject buffsFisrt;           // 버프창1
    public GameObject buffsSecond;          // 버프창2
    [SerializeField] private Status status; // 상태를 받아올 스크립트
    #endregion

    void Start()
    {
        status.OnDamaged += SetFillHealth;
        status.OnDamaged += SetHealthText;
        status.OnUseMana += SetFillMana;
        status.OnUseMana += SetManaText;
        status.OnDamaged.Invoke();
        status.OnUseMana.Invoke();
        //if (target == null)
        //{
        //    target = FindFirstObjectByType<XROrigin>().gameObject.transform;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        // target 위치에서 현재 오브젝트의 위치를 뺀 방향 벡터를 계산
        //Vector3 direction = target.position - transform.position;

        // UI가 Z축이 아니라 다른 축(예: Y축)으로 바라보게 설정
        //Quaternion rotation = Quaternion.LookRotation(-direction); // Z축 반전을 위해 음수 사용
        //transform.rotation = rotation;
    }

    void SetFillHealth()
    {
        fillHealth.fillAmount = (status.CurrentHealth / status.MaxHealth);
    }
    void SetHealthText()
    {
        healthText.text = $"{Mathf.Round(status.CurrentHealth)}/{status.MaxHealth}";
    }
    void SetFillMana()
    {
        fillMana.fillAmount = (status.CurrentMana / status.MaxMana);
    }
    void SetManaText()
    {
        manaText.text = $"{Mathf.Round(status.CurrentMana)}/{status.MaxMana}";
    }
}