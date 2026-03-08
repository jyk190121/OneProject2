using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class BaseUnit : NetworkBehaviour
{
    public Image HP_BAR;

    // 이펙트 프리팹을 인스펙터나 SO에서 할당받기 위한 변수
    protected GameObject deathEffectPrefab;

    // 현재 실시간으로 변하는 스탯
    public float currentHP;
    // 비율 계산을 위해 최대 체력 저장 필요
    protected float maxHP;
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
        if (isDead) return;

        // [수정] 멀티
        bool isNetworkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        //if (isNetworkActive && !IsServer) return;
        //// 사망 이펙트 생성 (Instantiate는 싱글/멀티 공통)
        //if (deathEffectPrefab != null)
        //{
        //    GameObject effect = Instantiate(deathEffectPrefab, transform.position, transform.rotation);
        //    Destroy(effect, 1.5f);
        //}
        //if (BattleManager.Instance != null)
        //{
        //    BattleManager.Instance.GameOver(); // 이 함수 내부에서 UpdateSpawnerToAlivePlayer()를 호출함
        //}
        // 멀티와 싱글 구분
        // 네트워크 상에서는 Despawn (서버가 호출)
        //if (isNetworkActive)
        //{
        //    var netObj = GetComponent<NetworkObject>();
        //    if (netObj != null && netObj.IsSpawned) netObj.Despawn();
        //}
        //// 싱글 모드에서는 일반 Destroy
        //else
        //{
        //    Destroy(gameObject);
        //}

        if (isNetworkActive)
        {
            if (IsServer)
            {
                isDead = true;

                // 1. 모든 클라이언트에게 사망 연출(이펙트) 명령을 내립니다.
                PlayDieEffectClientRpc();

                // 2. 서버에서만 처리해야 할 로직 (점수 등)은 여기서 처리합니다.
                // (자식 클래스인 EnemyFSM에서 오버라이드하여 점수 추가)

                // 3. 객체 제거 (Despawn)
                // 이펙트가 생성될 시간을 아주 잠깐 주기 위해 지연 제거를 고려할 수 있습니다.
                var netObj = GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned) netObj.Despawn();
            }
        }
        else
        {
            // 싱글 모드 로직
            isDead = true;

            ExecuteDiePerformance();
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void PlayDieEffectClientRpc()
    {
        // 서버를 포함한 모든 클라이언트에서 실행됩니다.
        ExecuteDiePerformance();
    }

    // 사망 연출(이펙트, 애니메이션 등)만 담당하는 공통 함수
    private void ExecuteDiePerformance()
    {
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, transform.rotation);
            Destroy(effect, 1.5f);
        }
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


    public bool GetDeadStatus()
    {
        return isDead;
    }
}
