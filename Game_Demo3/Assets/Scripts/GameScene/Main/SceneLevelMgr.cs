using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class SceneLevelMgr : MonoBehaviour
{
    // 单例模式
    private static SceneLevelMgr instance;
    public static SceneLevelMgr Instance => instance;
    private void Awake()
    {
        instance = this;
    }

    // 怪物点信息列表 每个场景中外部关联
    public List<MonsterPointObj> monsterPointsList;
    // Boss点信息 每个场景中外部关联
    public Transform bossPoint;
    // 造塔点信息列表 每个场景中外部关联
    public List<GameObject> buildTowerPointsList;
    // 寻路烘焙组件 每个场景中外部关联
    public NavMeshSurface navMeshSurface;
    // 玩家初始位置 外部关联
    public Transform playerPos;
    // 当前选中场景及等级信息
    private SceneLevelMonsterInfo sceneLevelMonsterInfo;
    // 当前场景上的怪物数量
    public int nowMonsterNum { get; private set; }
    // 当前出怪波数信息
    protected int nowWave;
    // 上一个出怪波数信息
    protected int frontWave;
    // 玩家游戏对象脚本记录
    protected PlayerObj playerObj;
    // 是否已经实例化Boss
    private bool isCreatedBoss;
    // 游戏是否已经结束
    public bool IsGameOver { get; protected set; }

    // 前半段怪物ID 预制体名(对象池键名)列表
    private int[] monsterStage1;
    private List<string> stage1Keys = new List<string>();
    // 后半段怪物ID
    private int[] monsterStage2;
    private List<string> stage2Keys = new List<string>();

    // Start is called before the first frame update
    protected virtual void Start()
    {
        // 初始化UI相关
        UIManager.Instance.ShowPanel<GamePanel>().gameObject.SetActive(false);

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

        // 记录当前场景关卡等级信息
        sceneLevelMonsterInfo = GameDataMgr.Instance.nowSelSceneLevel;

        // 注册并预热全部怪物对象池协程
        StartCoroutine(InitAllMonsterPoolCoroutine());

        // 根据关卡出怪点数量 删除不需要的出怪点
        for (int i = 0; i < monsterPointsList.Count; i++)
        {
            if (i >= sceneLevelMonsterInfo.monsterPointNum)
            {
                DestroyImmediate(monsterPointsList[i].gameObject);
            }
        }

        // 根据是否是Boss关 决定是否保留Boss点
        if (sceneLevelMonsterInfo.bossID == 0) Destroy(bossPoint.gameObject);

        // 根据关卡出怪点数量 删除不需要的造塔点
        for (int i = 0; i < buildTowerPointsList.Count; i++)
        {
            // 每一个出怪点对应两个造塔点
            if (i >= sceneLevelMonsterInfo.monsterPointNum * 2)
            {
                DestroyImmediate(buildTowerPointsList[i]);
            }
        }

        // 移除关卡不需要的出怪点引用
        monsterPointsList.RemoveAll(n => n == null);
        // 移除关卡不需要的造塔点引用
        buildTowerPointsList.RemoveAll(n => n == null);

        // 更新波数信息
        UIManager.Instance.GetPanel<GamePanel>().UpdateWaveNum(0, sceneLevelMonsterInfo.waveNum);
        // 更新保护区血量UI信息
        MainTowerObj.Instance.UpdateHpUI();

        // 异步重新烘焙寻路网格(适应造塔点数量)
        navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void EnterGameAnimation()
    {
        //允许玩家控制
        GameDataMgr.Instance.isGaming = true;
        //恢复相机跟随速度
        ThirdPersonCamera thirdPersonCamera = Camera.main.GetComponent<ThirdPersonCamera>();
        FightSettingsData fightSettingsData = GameDataMgr.Instance.FightSettingsData;
        if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo == null)
        {
            thirdPersonCamera.ResetToDefaults();
        }
        else
        {
            thirdPersonCamera.ResetToDefaultsWithSkin();
        }
        //设置相机鼠标灵敏度
        thirdPersonCamera.rotationSpeed *= fightSettingsData.normalSen / 10 * 2;
        thirdPersonCamera.freeLookRotationSpeed *= fightSettingsData.freeSen / 10 * 2;
        //显示游戏界面UI
        UIManager.Instance.GetPanel<GamePanel>().gameObject.SetActive(true);
    }

    protected virtual IEnumerator InitAllMonsterPoolCoroutine()
    {
        // 记录当前场景两个阶段所用怪物信息
        monsterStage1 = Array.ConvertAll(sceneLevelMonsterInfo.monsterStage1.Split(","), int.Parse);
        monsterStage2 = Array.ConvertAll(sceneLevelMonsterInfo.monsterStage2.Split(","), int.Parse);
        // 初始化对象池 注册对象池并预热池
        foreach (int monsterID in monsterStage1)
        {
            RegisterAndWarmMonster(monsterID, stage1Keys);
            yield return new WaitForSeconds(0.3f);
        }
        foreach (int monsterID in monsterStage2)
        {
            RegisterAndWarmMonster(monsterID, stage2Keys);
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// 对象池注册怪物信息
    /// </summary>
    /// <param name="ID">怪物ID编号</param>
    /// <param name="stage">怪物所属阶段</param>
    protected void RegisterAndWarmMonster(int ID, List<string> keysList)
    {
        //加载并注册对象池 预热并记录键值
        GameObject monsterPrefab = Resources.Load<GameObject>(GameDataMgr.Instance.MonsterList[ID - 1].res);
        ObjectPoolMgr.Instance.RegisterPool(monsterPrefab.name, monsterPrefab, true);
        ObjectPoolMgr.Instance.WarmPool(monsterPrefab.name);
        keysList.Add(monsterPrefab.name);
    }

    /// <summary>
    /// 获取随机的怪物键名
    /// </summary>
    /// <param name="stage">怪物所属阶段</param>
    /// <returns></returns>
    public string GetRandomMonsterKey(int stage)
    {
        switch (stage)
        {
            case 1:
                return stage1Keys[UnityEngine.Random.Range(0, stage1Keys.Count)];
            case 2:
                return stage2Keys[UnityEngine.Random.Range(0, stage2Keys.Count)];
        }
        return null;
    }

    /// <summary>
    /// 检查波数信息并更新
    /// </summary>
    public virtual void CheckAndUpdateWaveInfo()
    {
        //非游戏进行状态 直接结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        nowWave = monsterPointsList[0].nowWave;
        //遍历寻找最小的出怪点波数作为当前波数
        foreach (var point in monsterPointsList)
        {
            if (point.nowWave < nowWave)
                nowWave = point.nowWave;
        }

        //更新UI波数信息
        if (GameDataMgr.Instance.nowGameMode == GameMode.EndlessMode) UIManager.Instance.GetPanel<GamePanel>().UpdateWaveNum(nowWave);
        else UIManager.Instance.GetPanel<GamePanel>().UpdateWaveNum(nowWave, sceneLevelMonsterInfo.waveNum);

        //查看刷新波数是否改变
        if (frontWave != nowWave)
        {
            frontWave = nowWave;
            UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo("新的一波怪物已刷新", true);
        }
    }

    /// <summary>
    /// 增减当前怪物总数计数
    /// </summary>
    /// <param name="num">增减值</param>
    public void ChangeMonsterNum(int num)
    {
        nowMonsterNum += num;
    }

    /// <summary>
    /// 游戏失败方法
    /// </summary>
    /// <param name="gameOverType">游戏失败类型 1玩家死亡 2保护区死亡</param>
    public void GameOverLose(int gameOverType)
    {
        //解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        //玩家死亡
        if (gameOverType != 1) playerObj.DeadPlayer();
        //游戏结束
        IsGameOver = true;
        //移除第三人称相机脚本 玩家转向脚本 IK控制脚本
        Destroy(Camera.main.GetComponent<ThirdPersonCamera>());
        Destroy(playerObj.GetComponent<PlayerRotationController>());
        Destroy(playerObj.GetComponent<PlayerIKController>());
        //展示并初始化面板
        TipPanel tipPanel = UIManager.Instance.ShowPanel<TipPanel>();
        string info = gameOverType == 1 ? "玩家已死亡" : "保护区已被摧毁";

        //获得金钱
        int playerMoney;
        if (GameDataMgr.Instance.nowGameMode == GameMode.EndlessMode)
        {
            playerMoney = playerObj.awardMoney;

            // 更新历史最佳记录
            if (nowWave > GameDataMgr.Instance.PlayerData.endlessModeMaxWave) GameDataMgr.Instance.PlayerData.endlessModeMaxWave = nowWave;

            tipPanel.InitInfo($"GameOver\n{info}\n获得无尽模式奖励\n<color=Yellow>${playerMoney}</color>" +
                              $"\n当前坚守波数:{nowWave}\n历史最高记录:{GameDataMgr.Instance.PlayerData.endlessModeMaxWave}");
        }
        else
        {
            playerMoney = Mathf.RoundToInt(playerObj.awardMoney * 0.3f);
            tipPanel.InitInfo($"GameOver\n{info}\n获得失败奖励\n<color=Yellow>${playerMoney}</color>");
        }

        //记录玩家金钱增加
        GameDataMgr.Instance.PlayerData.money += playerMoney;
        GameDataMgr.Instance.SavePlayerData();
        //注册点击事件返回开始界面
        tipPanel.InitAction((v) =>
        {
            // 隐藏面板
            UIManager.Instance.HideAllPanel();
            // 切换场景
            LoadScenePanel loadScenePanel = UIManager.Instance.ShowPanel<LoadScenePanel>();
            loadScenePanel.InitInfo("BeginScene", "开始场景", () =>
            {
                //相机动画
                Camera.main.GetComponent<CameraAnimator>().TurnFarOrClose(() =>
                {
                    UIManager.Instance.ShowPanel<ChooseHeroPanel>();
                }, false);
            });
            //清空玩家信息记录
            GameDataMgr.Instance.nowPlayerObj = null;
        });
    }

    /// <summary>
    /// 游戏胜利方法
    /// </summary>
    public virtual void CheckGameOverWin()
    {
        // 场上怪物未死完 返回
        if (nowMonsterNum > 0) return;
        // 未到最后一波 返回
        if (nowWave < sceneLevelMonsterInfo.waveNum) return;
        // 遍历每个出怪点是否出怪完成
        foreach (var point in monsterPointsList)
        {
            if (!point.isMonsterOver) return;
        }
        // 判断是否是Boss关卡
        if (sceneLevelMonsterInfo.bossID > 0 && !isCreatedBoss)
        {
            // Boss出怪相关逻辑
            GameObject boss = Instantiate(Resources.Load<GameObject>(GameDataMgr.Instance.MonsterList[sceneLevelMonsterInfo.bossID - 1].res));
            boss.transform.SetPositionAndRotation(bossPoint.position, Quaternion.identity);
            boss.GetComponent<NavMeshAgent>().Warp(bossPoint.position);
            nowMonsterNum++;
            isCreatedBoss = true;
            // UI提示信息
            UIManager.Instance.GetPanel<GamePanel>().ShowTipInfo
            ($"{GameDataMgr.Instance.SceneList[sceneLevelMonsterInfo.sceneID - 1].name}Boss" +
            $"{GameDataMgr.Instance.MonsterList[sceneLevelMonsterInfo.bossID - 1].tipsName}已出现!", true);
            // 切换Boss战斗背景音乐
            BKMusic.Instance.ChangeBKMusic(GameDataMgr.Instance.SceneList[sceneLevelMonsterInfo.sceneID - 1].sceneName + "Boss");
            return;
        }
        // 游戏胜利逻辑
        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        // 停止玩家游戏进行中状态
        GameDataMgr.Instance.isGaming = false;
        //游戏结束
        IsGameOver = true;
        //移除第三人称相机脚本 玩家转向脚本 IK控制脚本
        Destroy(Camera.main.GetComponent<ThirdPersonCamera>());
        Destroy(playerObj.GetComponent<PlayerRotationController>());
        Destroy(playerObj.GetComponent<PlayerIKController>());
        // 展示并初始化面板
        TipPanel tipPanel = UIManager.Instance.ShowPanel<TipPanel>();
        // 获得金钱
        int playerMoney = Mathf.RoundToInt(playerObj.awardMoney * 0.8f);
        // 记录玩家金钱增加
        GameDataMgr.Instance.PlayerData.money += playerMoney;
        // 记录玩家关卡进度
        int[] sceneLevel = GameDataMgr.Instance.PlayerData.sceneLevelInfo;

        //最后一关情况
        if (sceneLevelMonsterInfo.sceneLevel == 10)
        {
            // 1.通关下一场景情况
            if (sceneLevelMonsterInfo.sceneID < sceneLevel.Length && sceneLevel[sceneLevelMonsterInfo.sceneID] == 0)
            {
                sceneLevel[sceneLevelMonsterInfo.sceneID]++;
                //提示信息
                tipPanel.InitInfo($"游戏胜利\n成功通关{GameDataMgr.Instance.SceneList[sceneLevelMonsterInfo.sceneID - 1].name} 难度" +
                $"<color=Red>{sceneLevelMonsterInfo.sceneLevel}</color>\n首次击败<color=Red>{GameDataMgr.Instance.MonsterList[sceneLevelMonsterInfo.bossID - 1].tipsName}</color>奖励\n<color=Yellow>${playerMoney}</color>" +
                $"\n<color=Red>{GameDataMgr.Instance.SceneList[sceneLevelMonsterInfo.sceneID].name}</color>已解锁");
            }
            // 2.首次通关全部关卡情况
            else if (sceneLevelMonsterInfo.sceneID == sceneLevel.Length && !GameDataMgr.Instance.PlayerData.isWinAllGame)
            {
                //提示信息
                tipPanel.InitInfo($"游戏胜利\n成功通关{GameDataMgr.Instance.SceneList[sceneLevelMonsterInfo.sceneID - 1].name} 难度" +
                $"<color=Red>{sceneLevelMonsterInfo.sceneLevel}</color>\n首次击败<color=Red>{GameDataMgr.Instance.MonsterList[sceneLevelMonsterInfo.bossID - 1].tipsName}</color>奖励\n<color=Yellow>${playerMoney}</color>" +
                $"\n<color=Red>恭喜你通关全部冒险模式场景!</color>");
                //通关状态变更
                GameDataMgr.Instance.PlayerData.isWinAllGame = true;
                // 存储玩家数据
                GameDataMgr.Instance.SavePlayerData();

                // 通关彩蛋
                // 注册点击事件返回开始界面
                tipPanel.InitAction((v) =>
                {
                    // 隐藏面板
                    UIManager.Instance.HideAllPanel();
                    // 清空玩家信息记录
                    GameDataMgr.Instance.nowPlayerObj = null;
                    // 片尾彩蛋面板
                    UIManager.Instance.ShowPanel<EndPanel>();
                });

                // 跳出逻辑
                return;
            }
            // 3.非首次通关当前场景情况
            else
            {
                //提示信息
                tipPanel.InitInfo($"游戏胜利\n成功通关{GameDataMgr.Instance.SceneList[sceneLevelMonsterInfo.sceneID - 1].name} 难度" +
                $"<color=Red>{sceneLevelMonsterInfo.sceneLevel}</color>\n获得再次通关奖励\n<color=Yellow>${playerMoney}</color>");
            }
        }
        //普通情况
        else
        {
            //提示信息
            tipPanel.InitInfo($"游戏胜利\n成功通关{GameDataMgr.Instance.SceneList[sceneLevelMonsterInfo.sceneID - 1].name} 难度" +
            $"<color=Red>{sceneLevelMonsterInfo.sceneLevel}</color>\n获得通关奖励\n<color=Yellow>${playerMoney}</color>");
            //关卡进度更新
            sceneLevel[sceneLevelMonsterInfo.sceneID - 1]++;
        }

        // 存储玩家数据
        GameDataMgr.Instance.SavePlayerData();
        // 注册点击事件返回开始界面
        tipPanel.InitAction((v) =>
        {
            // 隐藏面板
            UIManager.Instance.HideAllPanel();
            // 切换场景
            LoadScenePanel loadScenePanel = UIManager.Instance.ShowPanel<LoadScenePanel>();
            loadScenePanel.InitInfo("BeginScene", "开始场景", () =>
            {
                //相机动画
                Camera.main.GetComponent<CameraAnimator>().TurnFarOrClose(() =>
                {
                    UIManager.Instance.ShowPanel<ChooseHeroPanel>();
                }, false);
            });
            //清空玩家信息记录
            GameDataMgr.Instance.nowPlayerObj = null;
        });
    }

    private void OnDestroy()
    {
        //过场景时清空对象池
        ObjectPoolMgr.Instance.ClearAllPool();
    }
}
