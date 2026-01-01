using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

/// <summary>
/// 重置状态接口
/// </summary>
public interface IResetState
{
    /// <summary>
    /// 重置对象状态到初始值
    /// </summary>
    void ResetState();
}

public class ObjectPoolMgr
{
    // 单例模式
    private static ObjectPoolMgr instance = new ObjectPoolMgr();
    public static ObjectPoolMgr Instance => instance;
    private ObjectPoolMgr()
    {

    }

    /// <summary>
    /// 对象池配置信息
    /// </summary>
    private struct PoolConfig
    {
        // 默认容量
        public int defaultCapacity;
        // 最大容量
        public int maxCapacity;
        // 是否检查集合
        public bool collectionCheck;
        // 是否自动重置状态
        public bool autoResetState;
    }

    // 对象池字典
    private Dictionary<string, IObjectPool<GameObject>> poolsDic = new Dictionary<string, IObjectPool<GameObject>>();
    // 对象池对象配置信息字典
    private Dictionary<string, PoolConfig> poolsConfigDic = new Dictionary<string, PoolConfig>();
    // 对象池预制体引用
    private Dictionary<string, GameObject> poolsPrefabsDic = new Dictionary<string, GameObject>();

    /// <summary>
    /// 注册对象池
    /// </summary>
    /// <param name="poolKey">对象池键字符串</param>
    /// <param name="prefab">关联的预制体</param>
    /// <param name="defaultCapacity">默认容量</param>
    /// <param name="maxCapacity">最大容量</param>
    /// <param name="collectionCheck">是否启用集合检查</param>
    /// <param name="autoResetState">是否自动重置状态</param>
    public void RegisterPool(string poolKey, GameObject prefab, bool autoResetState = false, int defaultCapacity = 10,
                             int maxCapacity = 200, bool collectionCheck = false)
    {
        if (!poolsDic.ContainsKey(poolKey))
            CreateNewPool(poolKey, prefab, defaultCapacity, maxCapacity, collectionCheck, autoResetState);
    }

    /// <summary>
    /// 预热指定对象池
    /// </summary>
    /// <param name="poolKey">对象池键字符串</param>
    public void WarmPool(string poolKey)
    {
        if (poolsDic.ContainsKey(poolKey))
        {
            // 记录预热生成的对象列表
            List<GameObject> objList = new List<GameObject>();
            // 通过封装的方法获取默认容量的对象
            for (int i = 0; i < poolsConfigDic[poolKey].defaultCapacity; i++)
            {
                objList.Add(poolsDic[poolKey].Get());
            }
            foreach (GameObject obj in objList)
            {
                // 预热后立刻回收
                poolsDic[poolKey].Release(obj);
            }
        }
        else
        {
            Debug.LogWarning($"{poolKey}对象池键不存在");
        }
    }

    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    /// <param name="poolKey">对象池键字符串</param>
    /// <param name="pos">对象位置</param>
    /// <param name="quat">对象旋转</param>
    /// <param name="parent">对象父物体</param>
    /// <returns></returns>
    public GameObject Get(string poolKey, Vector3 pos, Quaternion quat, Transform parent = null)
    {
        // 确保键存在
        if (!poolsDic.ContainsKey(poolKey))
        {
            Debug.LogWarning(poolKey + "对象池键不存在");
            return null;
        }
        // 从对象池获取对象并设置位置和旋转
        GameObject obj = poolsDic[poolKey].Get();
        if (parent != null)
        {
            obj.transform.SetParent(parent, false);
        }
        obj.transform.SetPositionAndRotation(pos, quat);
        // 如果挂载寻路组件 用寻路组件相关方法设置位置 避免寻路出错
        if (obj.GetComponent<NavMeshAgent>())
        {
            obj.GetComponent<NavMeshAgent>().Warp(pos);
        }
        // 返回对象
        return obj;
    }

    /// <summary>
    /// 将对象返回到对象池
    /// </summary>
    /// <param name="obj">要返回的对象</param>
    public void ReleaseObj(GameObject obj)
    {
        GameObjectInPoolTag tag = obj.GetComponent<GameObjectInPoolTag>();
        // 判断对象池组件存在
        if (tag == null || string.IsNullOrEmpty(tag.PoolKey))
        {
            Debug.LogWarning($"{obj.name}未设置PoolKey");
            GameObject.Destroy(obj);
        }
        else
        {
            if (poolsDic.ContainsKey(tag.PoolKey))
            {
                // 如果设置了自动重置状态 调用对应实现的接口方法
                if (poolsConfigDic[tag.PoolKey].autoResetState)
                {
                    ResetObjectState(obj);
                }
                // 断绝父物体关系
                obj.transform.SetParent(null, false);
                // 回收对象
                poolsDic[tag.PoolKey].Release(obj);
            }
            else
            {
                Debug.LogWarning($"{tag.PoolKey}对象池键不存在");
                GameObject.Destroy(obj);
            }
        }
    }

    /// <summary>
    /// 清空指定的对象池
    /// </summary>
    /// <param name="poolKey">对象池键字符串</param>
    public void ClearPool(string poolKey)
    {
        if (poolsDic.ContainsKey(poolKey))
        {
            // 清空对应对象池
            poolsDic[poolKey].Clear();
            // 从字典中移除
            poolsDic.Remove(poolKey);
            poolsConfigDic.Remove(poolKey);
            poolsPrefabsDic.Remove(poolKey);
        }
    }

    /// <summary>
    /// 清空所有对象池
    /// </summary>
    public void ClearAllPool()
    {
        // 清空所有对象池
        foreach (IObjectPool<GameObject> pool in poolsDic.Values)
        {
            pool.Clear();
        }
        // 清空所有字典
        poolsDic.Clear();
        poolsConfigDic.Clear();
        poolsPrefabsDic.Clear();
    }

    /// <summary>
    /// 是否存在指定对象池
    /// </summary>
    /// <param name="poolKey">对象池键字符串</param>
    /// <returns></returns>
    public bool HasPool(string poolKey)
    {
        return poolsDic.ContainsKey(poolKey);
    }

    /// <summary>
    /// 创建新的对象池
    /// </summary>
    /// <param name="poolKey">对象池键字符串</param>
    /// <param name="prefab">关联的预制体</param>
    /// <param name="defaultCapacity">默认容量</param>
    /// <param name="maxCapacity">最大容量</param>
    /// <param name="collectionCheck">是否启用集合检查</param>
    /// <param name="autoResetState">是否自动重置状态</param>
    private void CreateNewPool(string poolKey, GameObject prefab, int defaultCapacity, int maxCapacity, bool collectionCheck, bool autoResetState)
    {
        // 创建新的对象池事例
        ObjectPool<GameObject> objectPool = new ObjectPool<GameObject>(
            () => CreateFunc(poolKey, prefab),
            (obj) => ActionOnGet(obj),
            (obj) => ActionOnRelease(obj),
            (obj) => ActionOnDestroy(obj),
            collectionCheck,
            defaultCapacity,
            maxCapacity
            );
        // 将对象池添加到字典中记录
        poolsDic.Add(poolKey, objectPool);
        // 记录配置信息
        poolsConfigDic.Add(poolKey, new PoolConfig
        {
            defaultCapacity = defaultCapacity,
            maxCapacity = maxCapacity,
            collectionCheck = collectionCheck,
            autoResetState = autoResetState,
        });
        // 记录原预设体
        poolsPrefabsDic.Add(poolKey, prefab);
    }

    /// <summary>
    /// 创建新对象函数
    /// </summary>
    /// <param name="poolKey">对象池键字符串</param>
    /// <param name="prefab">关联的预制体</param>
    private GameObject CreateFunc(string poolKey, GameObject prefab)
    {
        GameObject newObj = GameObject.Instantiate(prefab);
        // 为对象添加所属对象池标识
        GameObjectInPoolTag gameObjectInPoolTag = newObj.GetComponent<GameObjectInPoolTag>();
        if (gameObjectInPoolTag == null)
        {
            gameObjectInPoolTag = newObj.AddComponent<GameObjectInPoolTag>();
        }
        gameObjectInPoolTag.PoolKey = poolKey;
        // 初始化默认状态为失活
        newObj.SetActive(false);
        // 返回实例化的对象
        return newObj;
    }
    /// <summary>
    /// 获取对象时操作
    /// </summary>
    private void ActionOnGet(GameObject obj)
    {
        obj.SetActive(true);
    }
    /// <summary>
    /// 回收对象时操作
    /// </summary>
    private void ActionOnRelease(GameObject obj)
    {
        obj.SetActive(false);
    }
    /// <summary>
    /// 销毁对象时操作
    /// </summary>
    private void ActionOnDestroy(GameObject obj)
    {
        GameObject.Destroy(obj);
    }

    /// <summary>
    /// 重置对象状态时调用
    /// </summary>
    private void ResetObjectState(GameObject obj)
    {
        try
        {
            IResetState[] resetStates = obj.GetComponents<IResetState>();
            foreach (IResetState resetState in resetStates)
            {
                resetState.ResetState();
            }
        }
        catch
        {
            Debug.LogWarning("重置对象状态时出错");
        }
    }
}

public class GameObjectInPoolTag : MonoBehaviour
{
    //该物体所属对象池的键名
    public string PoolKey { get; set; }


}
