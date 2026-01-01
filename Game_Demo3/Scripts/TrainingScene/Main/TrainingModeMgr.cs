using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class TrainingModeMgr : MonoBehaviour
{
    // 单例模式
    private static TrainingModeMgr instance;
    public static TrainingModeMgr Instance => instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 玩家脚本
    private PlayerObj playerObj;
    // 拥有的武器ID
    private List<int> weaponIDs;
    // 当前武器索引
    private int nowHeroInfoIndex;
    // 上个武器索引
    private int frontHeroInfoIndex;
    // 场景中心点
    private Transform centrePos;
    // 玩家最远距离(距离中心点)
    public float maxDistance;
    // 僵尸对象池键列表
    private List<string> monsterKeys;
    // 僵尸对象池键与ID对应关系字典
    private Dictionary<string, int> monsterIDs;

    // 场景静态僵尸数量
    public int standMonsterCount;
    // 场景动态僵尸数量
    public int moveMonsterCount;
    // 场上总僵尸数量
    private int allMonsterCount;
    // 场上僵尸位置数组
    private Vector3[] monstersPos;
    // 场上僵尸脚本数组
    private TrainingModeMonster[] trainingModeMonsters;

    // 玩家状态是否进房子
    private bool isInHouse;

    // Start is called before the first frame update
    void Start()
    {
        // 初始化UI相关
        UIManager.Instance.ShowPanel<GamePanel>().gameObject.SetActive(false);

        // 关联玩家点
        Transform playerPos = GameObject.Find("PlayerPos").transform;
        // 关联场景中心点
        centrePos = GameObject.Find("CentrePos").transform;

        // 初始化拥有的武器ID
        weaponIDs = new List<int>(GameDataMgr.Instance.PlayerData.buyHero) { 1 };
        weaponIDs.Sort();

        // 初始化玩家相关
        ChangeWeapon(playerPos);

        // 初始化玩家音频播放组件
        Instantiate(Resources.Load<GameObject>("Music/PlayerSound/PlayerSoundMgr"));

        // 入场相机动画 允许操作延迟
        Invoke("EnterGameAnimation", 4.5f);

        // 对象池初始化训练场僵尸
        int[] monstersID = new int[] { 1, 3, 6, 9, 12 };
        monsterKeys = new List<string>();
        monsterIDs = new Dictionary<string, int>();

        // 注册并预热全部怪物对象池+实例化第一批僵尸协程
        StartCoroutine(InitAllMonsterPoolCoroutine(monstersID));
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

    /// <summary>
    /// 对象池注册怪物信息
    /// </summary>
    /// <param name="ID">怪物ID编号</param>
    private void RegisterAndWarmMonster(int ID)
    {
        //加载僵尸预制体
        GameObject monsterPrefab = Instantiate(Resources.Load<GameObject>(GameDataMgr.Instance.MonsterList[ID - 1].res));
        monsterPrefab.name = $"Monster{ID}";
        //删除不需要的脚本
        DestroyImmediate(monsterPrefab.GetComponent<NavMeshAgent>());
        DestroyImmediate(monsterPrefab.GetComponent<MonsterObj>());
        DestroyImmediate(monsterPrefab.GetComponent<AddMapIcon>());
        //添加训练场僵尸脚本
        monsterPrefab.AddComponent<TrainingModeMonster>();
        //注册对象池 预热并记录键值
        ObjectPoolMgr.Instance.RegisterPool(monsterPrefab.name, monsterPrefab, true);
        ObjectPoolMgr.Instance.WarmPool(monsterPrefab.name);
        monsterKeys.Add(monsterPrefab.name);
        monsterIDs.Add(monsterPrefab.name, ID);
        //删除实例化的预制体
        DestroyImmediate(monsterPrefab);
    }

    /// <summary>
    /// 注册并预热全部怪物对象池+实例化第一批僵尸协程
    /// </summary>
    /// <param name="monstersID">全体怪物ID数组</param>
    /// <returns></returns>
    private IEnumerator InitAllMonsterPoolCoroutine(int[] monstersID)
    {
        // 注册并预热全部僵尸对象池
        foreach (int monsterID in monstersID)
        {
            RegisterAndWarmMonster(monsterID);
            yield return new WaitForSeconds(0.3f);
        }

        // 初始化僵尸点/第一批僵尸脚本
        allMonsterCount = standMonsterCount + moveMonsterCount;
        monstersPos = new Vector3[standMonsterCount + moveMonsterCount * 2];
        trainingModeMonsters = new TrainingModeMonster[allMonsterCount];
        // 每次僵尸位置旋转角度
        float angle = 360f / (standMonsterCount + moveMonsterCount * 2);

        // 初始化全部僵尸位置
        for (int i = 0; i < standMonsterCount + moveMonsterCount * 2; ++i)
        {
            Vector3 dir = Quaternion.AngleAxis(angle * i, Vector3.up) * -centrePos.right;
            monstersPos[i] = centrePos.position + dir.normalized * (maxDistance + 10);
            yield return null;
        }

        // 初始化全部僵尸
        for (int i = 0; i < allMonsterCount; ++i)
        {
            GameObject monster;
            string key = monsterKeys[UnityEngine.Random.Range(0, monsterKeys.Count)];
            if (i < standMonsterCount)
            {
                monster = ObjectPoolMgr.Instance.Get(key, monstersPos[i], Quaternion.identity);
            }
            else
            {
                monster = ObjectPoolMgr.Instance.Get(key, monstersPos[standMonsterCount + (i - standMonsterCount) * 2], Quaternion.identity);
                monster.GetComponent<TrainingModeMonster>().InitMovePos(new Vector3[]
                {
                    monstersPos[standMonsterCount + (i - standMonsterCount) * 2],
                    monstersPos[standMonsterCount + (i - standMonsterCount) * 2 + 1]
                });
            }
            trainingModeMonsters[i] = monster.GetComponent<TrainingModeMonster>();
            trainingModeMonsters[i].InitInfo(monsterIDs[key]);
            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //限制玩家最远距离
        ConfinePlayerMaxDistance();
        //检测切换武器输入
        UpdateChangeWeaponInput();
    }

    /// <summary>
    /// 根据当前HeroInfo信息切换武器
    /// </summary>
    public void ChangeWeapon(Transform playerPos)
    {
        // 新玩家模型
        GameObject player;
        // 无皮肤
        if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo == null)
        {
            // 实例化新模型
            player = Instantiate(Resources.Load<GameObject>(GameDataMgr.Instance.nowSelHero.res), playerPos.position, playerPos.rotation);
        }
        // 有皮肤
        else
        {
            // 实例化新模型
            player = Instantiate(Resources.Load<GameObject>(GameDataMgr.Instance.PlayerData.nowSelSkinInfo.res), playerPos.position, playerPos.rotation);
            // 更新设置动画状态机
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
        // 删除上个玩家模型
        if (playerObj) Destroy(playerObj.gameObject);
        // 设置玩家脚本数据
        playerObj = player.GetComponent<PlayerObj>();
        playerObj.InitPlayerInfo(GameDataMgr.Instance.nowSelHero);
        nowHeroInfoIndex = frontHeroInfoIndex = weaponIDs.IndexOf(GameDataMgr.Instance.nowSelHero.id);
        GameDataMgr.Instance.nowPlayerObj = playerObj;
    }

    /// <summary>
    /// 限制玩家最远距离
    /// </summary>
    private void ConfinePlayerMaxDistance()
    {
        if (Vector3.Distance(playerObj.transform.position, centrePos.position) > maxDistance)
        {
            //计算方向
            Vector3 dir = playerObj.transform.position - centrePos.position;
            dir.y = 0;
            Vector3 targetPos = centrePos.position + dir.normalized * maxDistance;
            //重置距离
            CharacterController characterController = playerObj.GetComponent<CharacterController>();
            Vector3 moveDelta = targetPos - playerObj.transform.position;
            characterController.Move(moveDelta);
        }
    }

    /// <summary>
    /// 检测切换武器输入
    /// </summary>
    private void UpdateChangeWeaponInput()
    {
        //输入检测
        if (Input.GetKeyDown(KeyCode.Q)) { --nowHeroInfoIndex; }
        else if (Input.GetKeyDown(KeyCode.E)) { ++nowHeroInfoIndex; }
        //索引更新
        if (nowHeroInfoIndex < 0) { nowHeroInfoIndex = weaponIDs.Count - 1; }
        if (nowHeroInfoIndex >= weaponIDs.Count) { nowHeroInfoIndex = 0; }
        //切换武器
        if (frontHeroInfoIndex != nowHeroInfoIndex)
        {
            //播放音效
            if (playerObj) GameDataMgr.Instance.PlaySound("Music/Towers/BuildTower", 0.5f);
            GameDataMgr.Instance.nowSelHero = GameDataMgr.Instance.HeroList[weaponIDs[nowHeroInfoIndex] - 1];
            ChangeWeapon(playerObj.transform);
            //更新UI
            UIManager.Instance.GetPanel<GamePanel>().UpdateWeaponInfo();
        }
    }

    /// <summary>
    /// 处理怪物死亡
    /// </summary>
    public void DeadMonster(TrainingModeMonster monster)
    {
        //获取怪物索引 随机新怪物
        int index = Array.IndexOf(trainingModeMonsters, monster);
        string key = monsterKeys[UnityEngine.Random.Range(0, monsterKeys.Count)];
        GameObject newMonster;
        //静态怪物
        if (index < standMonsterCount)
        {
            newMonster = ObjectPoolMgr.Instance.Get(key, monstersPos[index], Quaternion.identity);
        }
        //动态怪物
        else
        {
            newMonster = ObjectPoolMgr.Instance.Get(key, monster.moveVector3s[0], Quaternion.identity);
            newMonster.GetComponent<TrainingModeMonster>().InitMovePos(new Vector3[]
            {
                monster.moveVector3s[0],
                monster.moveVector3s[1]
            });
        }
        //初始化怪物信息
        newMonster.GetComponent<TrainingModeMonster>().InitInfo(monsterIDs[key]);
        //更新怪物数组
        trainingModeMonsters[index] = newMonster.GetComponent<TrainingModeMonster>();
    }

    /// <summary>
    /// 处理玩家进出房子
    /// </summary>
    public void ChangePlayerInHouseState(bool isInHouse)
    {
        if (this.isInHouse == isInHouse) return;

        this.isInHouse = isInHouse;
        if (isInHouse) Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("Monster"));
        else Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("Monster"));
        UIManager.Instance.GetPanel<GamePanel>().HideOrShowThisGamePanel(isInHouse);
    }

    private void OnDestroy()
    {
        //过场景时清空对象池
        ObjectPoolMgr.Instance.ClearAllPool();
    }
}
