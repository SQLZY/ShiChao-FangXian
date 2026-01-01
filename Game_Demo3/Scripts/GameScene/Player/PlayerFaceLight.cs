using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFaceLight : MonoBehaviour
{
    //玩家脚本
    private PlayerObj player;
    //高度状态
    private bool isStand = true;

    // Start is called before the first frame update
    void Start()
    {
        //玩家脚本
        player = GameDataMgr.Instance.nowPlayerObj;
        //更新补光灯位置与父物体
        transform.SetParent(player.transform);
        //设置补光灯高度
        SetLightHeight(2.1f);
    }

    //更新补光灯高度
    private void Update()
    {
        if (player.crouchValue > 0.5f && isStand)
        {
            SetLightHeight(1.7f);
            isStand = false;
        }

        else if (player.crouchValue < 0.5f && !isStand)
        {
            SetLightHeight(2.1f);
            isStand = true;
        }
    }

    //设置补光灯高度
    private void SetLightHeight(float lightHeight)
    {
        //设置补光灯世界位置为角色身高高度向前一个单位
        transform.position = player.transform.position + Vector3.up * lightHeight + player.transform.forward;
        //光源对准角色面部
        transform.LookAt(player.transform.position + Vector3.up * lightHeight);
    }
}
