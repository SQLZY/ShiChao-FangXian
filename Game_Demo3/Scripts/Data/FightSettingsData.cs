using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightSettingsData
{
    //准星颜色 1234 WRGB
    public int aimStarColor = 1;
    //准星中心点开关
    public bool aimStarCentre = true;
    //动态准星开关
    public bool aimStarDynamic = true;

    //操作模式 true切换 false长按
    public bool runControlMode;
    public bool crouchControlMode;
    public bool aimControlMode;
    public bool freeControlMode;

    //灵敏度数值 1-10范围
    public float normalSen = 5;
    public float freeSen = 5;
}
