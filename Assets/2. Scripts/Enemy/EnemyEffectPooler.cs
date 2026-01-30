using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyEffectPooler : MonoBehaviour
{
    public static EnemyEffectPooler Instance;

    [System.Serializable]
    public struct EffectData
    {
        public string name;
        public GameObject prefab;
    }

    public List<EffectData> effects;
    private Dictionary<string, IObjectPool<GameObject>> poolDict = new Dictionary<string, IObjectPool<GameObject>>();

    void Awake()
    {
        Instance = this;
        foreach (var effect in effects)
        {
            // 각 이펙트 이름별로 풀 생성
            var pool = new ObjectPool<GameObject>(
                () => Instantiate(effect.prefab, transform),
                obj => obj.SetActive(true),
                obj => obj.SetActive(false),
                obj => Destroy(obj),
                false, 10, 20);

            poolDict.Add(effect.name, pool);
        }
    }

    public GameObject GetEffect(string effectName) => poolDict[effectName].Get();
    public void ReturnEffect(string effectName, GameObject obj) => poolDict[effectName].Release(obj);

    public IEnumerator ReturnEffectAfterTime(string poolName, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnEffect(poolName, obj);
    }
}