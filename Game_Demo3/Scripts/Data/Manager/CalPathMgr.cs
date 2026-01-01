using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CalPathMgr : MonoBehaviour
{
    // 无需挂载 自动生成并管理的单例模式
    private static CalPathMgr instance;
    public static CalPathMgr Instance
    {
        get
        {
            // 如果单例未挂载实例化 自动生成
            if (instance == null)
            {
                instance = GameObject.FindObjectOfType<CalPathMgr>();
                if (instance == null)
                {
                    GameObject gameObject = new GameObject("CalPathMgr");
                    instance = gameObject.AddComponent<CalPathMgr>();
                    DontDestroyOnLoad(gameObject);
                }
            }
            // 返回单例
            return instance;
        }
    }

    // 单例模式Awake自动管理
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        //实例化NavMeshPath类
        path = new NavMeshPath();
    }

    // 寻路路径计算时间间隔 优化性能
    private float updatePathTime = 0.2f;
    // 路径数据
    private NavMeshPath path;
    // 玩家位置是否可达或部分可达
    private bool isPlayerReachable;
    // 可达的最近玩家位置
    private Vector3 reachablePlayerPos;
    // 上一个玩家位置
    private Vector3 frontPlayerPos;
    // 上一次更新时间
    private float frontUpdateTime;

    // Update is called once per frame
    void Update()
    {
        // 非游戏状态 退出逻辑
        if (!GameDataMgr.Instance.nowPlayerObj || !GameDataMgr.Instance.isGaming ||
            MainTowerObj.Instance == null || !MainTowerObj.Instance.gameObject)
        {
            return;
        }

        // 更新计算通往玩家的路径
        UpdatePlayerPath();
    }

    /// <summary>
    /// 计算通往玩家的路径
    /// </summary>
    private void UpdatePlayerPath()
    {
        // 未达到更新时间间隔 退出逻辑
        if (Time.time - frontUpdateTime < updatePathTime) return;

        // 玩家未移动 退出逻辑
        if (Vector3.Distance(GameDataMgr.Instance.nowPlayerObj.transform.position, frontPlayerPos) < 0.1f) return;

        // 通过玩家出生点计算通往玩家路径
        NavMesh.CalculatePath(GameObject.Find("PlayerPos").transform.position,
                              GameDataMgr.Instance.nowPlayerObj.transform.position,
                              NavMesh.AllAreas,
                              path);

        // 路径计算状态
        switch (path.status)
        {
            // 路径完全可达 最近位置就是玩家位置
            case NavMeshPathStatus.PathComplete:
                reachablePlayerPos = GameDataMgr.Instance.nowPlayerObj.transform.position;
                isPlayerReachable = true;
                break;

            // 路径部分可达 最近位置设置为最后一个路径拐点
            case NavMeshPathStatus.PathPartial:
                //虽然最后一个路径拐点 在少数特殊情况下 不是距离目标最近的点
                //但是相比遍历拐点数组 节约大量性能
                reachablePlayerPos = path.corners[path.corners.Length - 1];
                isPlayerReachable = true;
                break;

            // 路径不可达/计算路径出错
            case NavMeshPathStatus.PathInvalid:
                reachablePlayerPos = Vector3.zero;
                isPlayerReachable = false;
                break;
        }

        // 更新变量参数
        frontUpdateTime = Time.time;
        frontPlayerPos = GameDataMgr.Instance.nowPlayerObj.transform.position;
    }

    /// <summary>
    /// 获取玩家路径寻路数据
    /// </summary>
    /// <returns>玩家路径寻路数据结构体</returns>
    public PlayerPathData GetPlayerPathData()
    {
        return new PlayerPathData
        {
            isPlayerReachable = isPlayerReachable,
            reachablePlayerPos = reachablePlayerPos
        };
    }
}

/// <summary>
/// 玩家路径寻路数据结构体
/// </summary>
public struct PlayerPathData
{
    public bool isPlayerReachable;
    public Vector3 reachablePlayerPos;
}
