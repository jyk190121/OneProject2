using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class BaseUnit : MonoBehaviour
{
    public Image HP_BAR;

    // 이펙트 프리팹을 인스펙터나 SO에서 할당받기 위한 변수
    protected GameObject deathEffectPrefab;

    // 현재 실시간으로 변하는 스탯
    public float currentHP;
    protected float maxHP; // 비율 계산을 위해 최대 체력 저장 필요
    protected bool isDead = false;

    // 초기화 함수
    protected virtual void InitStats(float hp)
    {
        currentHP = maxHP = hp;
        //currentHP = maxHP;

        UpdateUI();
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHP = Mathf.Max(currentHP -= damage, 0);

        UpdateUI();
        if (currentHP <= 0) Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        //gameObject.SetActive(false);
        // 사망 이펙트 생성
        if (deathEffectPrefab != null)
        {
            // 현재 위치와 회전값으로 생성
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, transform.rotation);

            // 이펙트도 일정 시간 뒤 삭제 (또는 이펙트 자체에 AutoDisable이 있다면 생략)
            Destroy(effect, 1.5f);
        }

        Destroy(gameObject);
    }

    public void Heal(float heal)
    {
        currentHP += heal;
        if (currentHP >= maxHP) currentHP = maxHP;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (HP_BAR == null) return;
        HP_BAR.fillAmount = currentHP / maxHP;
    }

    // 초기화 시점에 이펙트 할당 (각 자식 클래스에서 호출)
    protected void SetDeathEffect(GameObject effect)
    {
        deathEffectPrefab = effect;
    }

}
