using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntoHouseController : MonoBehaviour
{
    // 子物体触发器状态
    private TriggerState[] states;
    // 检测时间间隔
    private float updateTime = 0.4f;
    // 上次检测时间
    private float frontUpdateTime;
    // 玩家状态
    private bool isInHouse;

    // Start is called before the first frame update
    void Start()
    {
        states = GetComponentsInChildren<TriggerState>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - frontUpdateTime > updateTime)
        {
            // 检测玩家状态
            isInHouse = false;
            foreach (var state in states)
            {
                if (state.IsEnter)
                {
                    isInHouse = true;
                }
            }
            // 更新UI相关
            TrainingModeMgr.Instance.ChangePlayerInHouseState(isInHouse);
            // 重置检测时间
            frontUpdateTime = Time.time;
        }
    }
}
