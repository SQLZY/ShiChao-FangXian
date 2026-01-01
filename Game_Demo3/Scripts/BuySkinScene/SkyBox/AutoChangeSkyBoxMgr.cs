using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChangeSkyBoxMgr : MonoBehaviour
{
    // 天空盒材质球数组
    public Material[] skyboxes;
    // 当前索引
    private int index = -1;
    // 更换天空盒时间间隔
    private float changeTime = 60f;
    // 上次更换天空盒时间
    // 设置对应间隔时间负数 确保首次进入场景能随机更新天空盒
    private float frontChangeTime = -60f;

    // Update is called once per frame
    void Update()
    {
        // 达到时间间隔
        if (Time.time - frontChangeTime > changeTime)
        {
            // 随机新天空盒索引
            int newIndex = Random.Range(0, skyboxes.Length);
            while (newIndex == index)
            {
                newIndex = Random.Range(0, skyboxes.Length);
            }
            // 记录天空盒 重置时间 记录索引
            RenderSettings.skybox = skyboxes[newIndex];
            frontChangeTime = Time.time;
            index = newIndex;
        }
    }
}
