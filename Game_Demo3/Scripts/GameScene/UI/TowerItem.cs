using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 炮塔Item信息结构体
/// </summary>
public struct TowerItemInfo
{
    public GameObject towerObj;
    public int money;
    public int towerID;
    public bool isMaxLevel;
    public Color levelColor;
    public bool isUnlocked;
}

/// <summary>
/// 单个炮塔信息控件
/// </summary>
public class TowerItem : MonoBehaviour
{
    //炮塔图片
    public Image imgTower;
    //按键提示
    public Text txtTip;
    //金钱信息
    public Text txtMoney;
    //防御塔名字
    public Text txtTowerName;
    //防御塔等级对应前缀字典
    private Dictionary<int, string> levelNameDic;
    //防御塔等级对应颜色字典
    private Dictionary<int, Color> levelColorDic;
    //当前炮台对应炮塔Item信息结构体
    private TowerItemInfo nowTowerItemInfo;

    private void Awake()
    {
        levelNameDic = new Dictionary<int, string>()
        {
            {1,""},
            {2,"初级"},
            {3,"中级"},
            {4,"高级"}
        };

        levelColorDic = new Dictionary<int, Color>()
        {
            {1, Color.white},
            {2, Color.green},
            {3, Color.cyan},
            {4, Color.red}
        };
    }

    public void InitInfo(int towerId, int towerLevel, bool isLevelMax, int money = 0)
    {
        //更新对应防御塔图标
        imgTower.sprite = Resources.Load<Sprite>(towerId == 1 ? $"Sprites/TowersImg/Tower1" : $"Sprites/TowersImg/Tower{towerId}_{towerLevel - 1}");

        //获取对应防御塔信息
        TowerInfo info = GameDataMgr.Instance.TowerList[towerId - 1];

        //更新当前炮塔Item信息结构体
        nowTowerItemInfo.towerObj = Resources.Load<GameObject>(towerId == 1 ? $"Towers/Tower1" : $"Towers/Tower{towerId}_{towerLevel - 1}");
        nowTowerItemInfo.towerID = towerId;
        nowTowerItemInfo.money = money;
        nowTowerItemInfo.isMaxLevel = isLevelMax;
        nowTowerItemInfo.levelColor = levelColorDic[towerLevel];

        //更新文字信息 文字颜色
        txtTip.gameObject.SetActive(!isLevelMax);
        //更新炮台是否已解锁信息
        if (towerLevel > GameDataMgr.Instance.PlayerData.maxTowerLevel)
        {
            txtMoney.text = "未解锁";
            nowTowerItemInfo.isUnlocked = false;
        }
        else
        {
            txtMoney.text = isLevelMax ? "已满级" : $"筑晶{money}";
            nowTowerItemInfo.isUnlocked = true;
        }
        txtTowerName.text = $"{levelNameDic[towerLevel]}{info.name}";
        txtTowerName.color = levelColorDic[towerLevel];
    }

    /// <summary>
    /// 获取当前的炮塔Item信息结构体
    /// </summary>
    public TowerItemInfo GetNowTowerItemInfo()
    {
        return nowTowerItemInfo;
    }
}
