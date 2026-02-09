using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyEffectPooler : MonoBehaviour
{
    public static EnemyEffectPooler Instance;

    //[System.Serializable]
    //public struct EffectData
    //{
    //    public string name;
    //    public GameObject prefab;
    //}

    //public List<EffectData> effects;
    //private Dictionary<string, IObjectPool<GameObject>> poolDict = new Dictionary<string, IObjectPool<GameObject>>();
    
    // 프리팹의 InstanceID를 키로 사용하는 딕셔너리
    private Dictionary<int, IObjectPool<GameObject>> poolDict = new Dictionary<int, IObjectPool<GameObject>>();

    void Awake()
    {
        Instance = this;
        //foreach (var effect in effects)
        //{
        //    // 각 이펙트 이름별로 풀 생성
        //    var pool = new ObjectPool<GameObject>(
        //        () => Instantiate(effect.prefab, transform),
        //        obj => obj.SetActive(true),
        //        obj => obj.SetActive(false),
        //        obj => Destroy(obj),
        //        false, 10, 20);

        //    poolDict.Add(effect.name, pool);
        //}
    }

    //public GameObject GetEffect(string effectName) => poolDict[effectName].Get();
    //public void ReturnEffect(string effectName, GameObject obj) => poolDict[effectName].Release(obj);

    // [수정] GameObject 프리팹을 직접 받아서 풀에서 꺼내주는 함수
    public GameObject GetEffect(GameObject prefab)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();

        // 해당 프리팹용 풀이 없다면 새로 생성
        if (!poolDict.ContainsKey(key))
        {
            poolDict.Add(key, new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 20
            ));
        }

        return poolDict[key].Get();
    }

    // [수정] 반납 시에도 프리팹을 키로 사용
    public void ReturnEffect(GameObject prefab, GameObject obj)
    {
        if (prefab == null || obj == null) return;

        int key = prefab.GetInstanceID();
        if (poolDict.ContainsKey(key))
        {
            poolDict[key].Release(obj);
        }
    }

    public IEnumerator ReturnEffectAfterTime(GameObject prefab, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnEffect(prefab, obj);
    }
}