using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public class BuildTowerPointObj : MonoBehaviour
{
    // 建塔点坐标
    public Transform towerPoint;
    // 玻璃保护罩
    public GameObject glassBox;
    // 地图图标组件
    private AddMapIcon addMapIcon;
    // 当前塔等级
    private int towerLevel;
    // 当前塔ID
    private int nowtowerID;
    // 当前造塔预制体
    private GameObject nowTowerObj;
    // 当前造塔区域UI是否在显示
    private bool isShowing;
    // 造塔区域光环粒子特效
    private ParticleSystem[] particleSystems;

    // Start is called before the first frame update
    void Start()
    {
        addMapIcon = GetComponentInChildren<AddMapIcon>();
        particleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        //非游戏进行状态 直接结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        if (isShowing)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                UpgradeTower(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                UpgradeTower(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                UpgradeTower(3);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                UpgradeTower(4);
            }
        }
    }

    /// <summary>
    /// 玩家进入造塔区域
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        //非游戏进行状态 直接结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        if (other.CompareTag("Player"))
        {
            //未造塔
            if (towerLevel == 0)
            {
                UIManager.Instance.GetPanel<GamePanel>().ShowTowersItem(out isShowing, 1, false, new int[] { 1 }, GetNowUpgradeMoney(1));
            }
            //已造基础塔
            else if (towerLevel == 1)
            {
                UIManager.Instance.GetPanel<GamePanel>().ShowTowersItem(out isShowing, 2, false, new int[] { 2, 3, 4, 5 }, GetNowUpgradeMoney(2));
            }
            //炮塔未满级
            else if (towerLevel < 4)
            {
                UIManager.Instance.GetPanel<GamePanel>().ShowTowersItem(out isShowing, towerLevel + 1, false, new int[] { nowtowerID }, GetNowUpgradeMoney(nowtowerID));
            }
            //炮台已满级
            else
            {
                UIManager.Instance.GetPanel<GamePanel>().ShowTowersItem(out isShowing, towerLevel, true, new int[] { nowtowerID });
            }
        }
    }

    /// <summary>
    /// 玩家离开造塔区域
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        //非游戏进行状态 直接结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        if (other.CompareTag("Player"))
        {
            UIManager.Instance.GetPanel<GamePanel>().HideTowersItem(out isShowing);
        }
    }

    /// <summary>
    /// 获取当前指定ID炮台升级费用
    /// </summary>
    /// <param name="towerId">炮塔ID</param>
    /// <returns></returns>
    private int GetNowUpgradeMoney(int towerId)
    {
        if (towerLevel < 2)
        {
            return GameDataMgr.Instance.TowerList[towerId - 1].money;
        }
        else
        {
            //每级升级费用翻2.5倍(基于基础升级费用)
            return (int)(GameDataMgr.Instance.TowerList[towerId - 1].money * (towerLevel - 1) * 2.5f);
        }
    }

    private void UpgradeTower(int keyCode)
    {
        //获取对应炮台信息
        TowerItemInfo towerItemInfo = UIManager.Instance.GetPanel<GamePanel>().GetTowerItemInfo(keyCode);
        //按键对应槽位为空 或 塔已满级 结束逻辑
        if (towerItemInfo.towerObj == null) return;
        if (towerItemInfo.isMaxLevel)
        {
            UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("防御塔已满级", false);
            return;
        }
        //防御塔等级未解锁 结束逻辑
        if (!towerItemInfo.isUnlocked)
        {
            UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("防御塔尚未解锁该等级", false);
            return;
        }

        //判断金钱是否足够
        if (GameDataMgr.Instance.nowPlayerObj.money >= towerItemInfo.money)
        {
            //升级炮台逻辑

            //升级特效
            GameDataMgr.Instance.PlayEff("Eff/TowersEff/UpgradeTower", towerPoint.position, Quaternion.LookRotation(towerPoint.up));
            //升级音效 使用无刚体组件的父物体作为音源父物体 避免播放出错
            GameDataMgr.Instance.PlaySound("Music/Towers/BuildTower", 1, transform.parent.transform);
            //扣钱
            GameDataMgr.Instance.nowPlayerObj.RemoveMoney(towerItemInfo.money);
            //删除当前炮塔预制体
            Destroy(nowTowerObj);
            //实例化新目标炮塔
            nowTowerObj = Instantiate(towerItemInfo.towerObj, towerPoint.position, Quaternion.identity);
            //升级当前炮塔等级
            towerLevel++;
            //更新当前炮塔ID
            nowtowerID = towerItemInfo.towerID;
            //更新炮台TowerObj信息
            nowTowerObj.GetComponent<TowerObj>().InitInfo(GameDataMgr.Instance.TowerList[nowtowerID - 1], towerLevel);
            //首次造塔
            if (towerLevel == 1)
            {
                //更新小地图图标
                addMapIcon.ChangeIcon(E_IconType.Tower);
                //移除玻璃罩
                glassBox.transform.GetComponentInChildren<MeshRenderer>().enabled = false;
            }
            //更新粒子特效颜色
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule mainModule = particleSystem.main;
                mainModule.startColor = new ParticleSystem.MinMaxGradient(towerItemInfo.levelColor);
            }
        }
        else
        {
            //提示玩家金钱不足
            UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("升级筑晶不足", false);
        }
    }
}
