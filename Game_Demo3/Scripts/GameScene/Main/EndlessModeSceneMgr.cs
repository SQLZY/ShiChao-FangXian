using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无尽模式出怪字典
/// </summary>
public static class EndlessModeMonsterWaveDic
{
    // 波数对应出怪IDs字典
    public static Dictionary<int, int[]> waveToMonsterIDsDic = new Dictionary<int, int[]>()
    {
        { 5,new int[] {1} },
        {10,new int[] {1,4,7} },
        {15,new int[] {1,4,7,10} },
        {20,new int[] {1,4,7,10,2,5,8,11} },
        {25,new int[] {2,5,8,11} },
        {50,new int[] {2,5,8,11,3,6,9,12} },
        {99,new int[] {3,6,9,12} },
    };
}

public class EndlessModeSceneMgr : SceneLevelMgr
{
    // 无尽模式怪物对象池Keys
    private List<string> endlessModeMonsterKeys;
    // 无尽模式出怪字典
    private Dictionary<int, int[]> waveToMonsterIDsDic;
    // 当前出怪ID数组
    private int[] nowWaveMonsterIDs;
    // 出怪字典Keys
    private int[] monsterIDsDicKeys;
    // 是否触发无尽模式彩蛋
    public bool IsEggTrigger { get; private set; }

    public override void CheckGameOverWin() { }

    protected override void Start()
    {
        // 初始化UI相关
        UIManager.Instance.ShowPanel<GamePanel>().gameObject.SetActive(false);

        // 初始化无尽模式出怪字典
        waveToMonsterIDsDic = EndlessModeMonsterWaveDic.waveToMonsterIDsDic;
        // 获取出怪字典Keys
        monsterIDsDicKeys = new int[waveToMonsterIDsDic.Count];
        waveToMonsterIDsDic.Keys.CopyTo(monsterIDsDicKeys, 0);

        // 初始化玩家相关
        GameObject player;
        // 无皮肤
        if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo == null)
        {
            player = Instantiate(Resources.Load<GameObject>(GameDataMgr.Instance.nowSelHero.res), playerPos.position, playerPos.rotation);
        }
        // 有皮肤
        else
        {
            // 实例化皮肤 设置动画状态机
            player = Instantiate(Resources.Load<GameObject>(GameDataMgr.Instance.PlayerData.nowSelSkinInfo.res), playerPos.position, playerPos.rotation);
            player.GetComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>($"Animator/Hero/{GameDataMgr.Instance.nowSelHero.id}");
            // 实例化武器 设置开火点 设置正确位置/旋转/缩放
            GameObject weapon = Instantiate(Resources.Load<GameObject>($"Hero/Gun/{GameDataMgr.Instance.nowSelHero.id}"));
            Transform[] transforms = player.transform.GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms)
            {
                if (t.gameObject.name == "item_r")
                {
                    weapon.transform.SetParent(t, false);
                    break;
                }
            }
            weapon.transform.localScale = Vector3.one * 0.005f;
            weapon.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            player.GetComponent<PlayerObj>().gunFirePoint = weapon.transform.GetChild(0);
            player.GetComponent<PlayerObj>().weapon = weapon;
        }
        // 设置玩家脚本数据
        playerObj = player.GetComponent<PlayerObj>();
        playerObj.InitPlayerInfo(GameDataMgr.Instance.nowSelHero);
        GameDataMgr.Instance.nowPlayerObj = playerObj;

        // 初始化玩家音频播放组件
        Instantiate(Resources.Load<GameObject>("Music/PlayerSound/PlayerSoundMgr"));

        // 入场相机动画 允许操作延迟
        Invoke("EnterGameAnimation", 4.5f);

        // 注册并预热全部怪物对象池协程
        StartCoroutine(InitAllMonsterPoolCoroutine());

        // 更新波数信息
        UIManager.Instance.GetPanel<GamePanel>().UpdateWaveNum(0);
        // 更新保护区血量UI信息
        MainTowerObj.Instance.UpdateHpUI();

        // 异步重新烘焙寻路网格(适应造塔点数量)
        navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
    }

    protected override IEnumerator InitAllMonsterPoolCoroutine()
    {
        // 初始化怪物键列表
        endlessModeMonsterKeys = new List<string>();

        // 无尽模式 注册全部非Boss怪物对象池
        List<int> allMonsterIDs = new List<int>();
        foreach (var monsterInfo in GameDataMgr.Instance.MonsterList)
        {
            if (!monsterInfo.isBoss) allMonsterIDs.Add(monsterInfo.id);
        }

        yield return new WaitForSeconds(0.3f);

        // 初始化对象池 注册对象池并预热池
        foreach (int monsterID in allMonsterIDs)
        {
            RegisterAndWarmMonster(monsterID, endlessModeMonsterKeys);
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// 无尽模式获取随机怪物对象池键
    /// </summary>
    public string EndlessModeGetRandomMonsterKey()
    {
        return endlessModeMonsterKeys[nowWaveMonsterIDs[Random.Range(0, nowWaveMonsterIDs.Length)] - 1];
    }

    /// <summary>
    /// 检查波数信息并更新
    /// </summary>
    public override void CheckAndUpdateWaveInfo()
    {
        base.CheckAndUpdateWaveInfo();
        //更新当前波数对应的出怪ID数组
        UpdateNowWaveMonsterID();
    }

    /// <summary>
    /// 更新当前波数对应的出怪ID数组
    /// </summary>
    private void UpdateNowWaveMonsterID()
    {
        // 计算当前波数对应的出怪ID数组

        // 波数大于字典最大波数
        if (nowWave > monsterIDsDicKeys[monsterIDsDicKeys.Length - 1])
        {
            nowWaveMonsterIDs = EndlessModeMonsterWaveDic.waveToMonsterIDsDic[monsterIDsDicKeys.Length - 1];
        }
        // 其余情况
        else
        {
            foreach (int waveNum in monsterIDsDicKeys)
            {
                if (nowWave <= waveNum)
                {
                    nowWaveMonsterIDs = EndlessModeMonsterWaveDic.waveToMonsterIDsDic[waveNum];
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 触发无尽模式彩蛋
    /// </summary>
    public void EndlessModeEggTrigger()
    {
        IsEggTrigger = true;
        UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("恭喜触发隐藏彩蛋", false);
        UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("本轮无尽模式游戏", false);
        UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("玩家攻击力增加20%", false);
        UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("防御塔攻击力增加20%", false);

        //应用增益
        GameDataMgr.Instance.nowPlayerObj.AddAtkRatio(1.2f);
        TowerObj[] towers = GameObject.FindObjectsOfType<TowerObj>();
        foreach (TowerObj tower in towers)
        {
            tower.EndlessModeUpdateAtkRatio();
        }
    }
}
