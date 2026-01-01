using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 保护区脚本
/// </summary>
public class MainTowerObj : MonoBehaviour
{
    //当前血量
    private int nowHp;
    //最大血量
    private int maxHp;
    //是否死亡
    private bool isDead;
    //玩家对象
    private PlayerObj playerObj;

    //单例模式
    private static MainTowerObj instance;
    public static MainTowerObj Instance => instance;
    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        //读取玩家数据 读取增益控制信息
        PlayerData playerData = GameDataMgr.Instance.PlayerData;
        SkinAwardControlInfo skinAwardControlInfo = GameDataMgr.Instance.AllControlInfo.skinAwardControlInfo;
        //更新保护区基础血量
        nowHp = maxHp = playerData.mainTowerBasicHp + playerData.consumeMoney * skinAwardControlInfo.mainTowerHp;
        UpdateHp(nowHp, maxHp);
        //获取玩家对象
        playerObj = GameDataMgr.Instance.nowPlayerObj;
    }

    // Update is called once per frame
    void Update()
    {

    }

    //更新血量
    private void UpdateHp(int nowHp, int maxHp)
    {
        this.nowHp = nowHp;
        this.maxHp = maxHp;
        //更新界面显示
        UpdateHpUI();
    }

    /// <summary>
    /// 更新保护区血量UI显示
    /// </summary>
    public void UpdateHpUI()
    {
        //更新界面显示
        UIManager.Instance.GetPanel<GamePanel>().UpdateTowerHp(nowHp, maxHp);
    }

    //受到伤害
    public void Wound(int dmg)
    {
        if (isDead || !GameDataMgr.Instance.isGaming) return;
        nowHp -= dmg;
        UpdateHp(nowHp > 0 ? nowHp : 0, maxHp);
        //提示玩家防御塔受击
        UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("主基地正在受到攻击", true);
        if (nowHp <= 0)
        {
            isDead = true;
            //游戏结束逻辑
            SceneLevelMgr.Instance.GameOverLose(2);
        }
    }

    //过场景时清除引用
    private void OnDestroy()
    {
        instance = null;
    }

}
