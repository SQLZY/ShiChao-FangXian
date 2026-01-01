using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipInfoItem : MonoBehaviour
{
    public Text tipInfo;
    
    /// <summary>
    /// 改变提示信息
    /// </summary>
    /// <param name="tipInfo">提示信息</param>
    /// <param name="isRed">是否红色等级</param>
    public void ChangeTipInfo(string info,bool isRed)
    {
        tipInfo.text = info;
        tipInfo.color = isRed ? Color.red : Color.yellow;
    }
}
