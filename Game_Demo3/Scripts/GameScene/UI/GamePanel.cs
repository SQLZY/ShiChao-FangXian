using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GamePanel : BasePanel
{
    //保护区/玩家血条总宽度
    private float hpw = 480;

    //前景血条图片
    public Image imgHp;
    //真实血条图片
    public Image imgHpTrue;
    //血量文字
    public Text txtHpNum;
    //保护区当前血条宽度
    private float nowHpw;
    //保护区目标血条宽度
    private float targetHpw;

    //玩家前景血条图片
    public Image imgHpPlayer;
    //玩家真实血条图片
    public Image imgHpTruePlayer;
    //玩家血量文字
    public Text txtHpNumPlayer;
    //玩家当前血条宽度
    private float nowHpwPlayer;
    //玩家目标血条宽度
    private float targetHpwPlayer;

    //小地图图片
    public RawImage mapImg;
    //小地图父物体背景图片
    public Image mapBk;
    //波数文字
    public Text txtWaveNum;
    //金币文字
    public Text txtMoneyNum;
    //退出按钮
    public Button btnQuit;
    //炮塔显示区域
    public Transform towersTrans;
    //图标Icons父对象
    public Transform icons;
    //提示信息区域
    public Transform tipInfosArea;
    //炮塔控件管理
    private TowerItem[] towersArray;
    //提示信息队列
    private Queue<TipInfoItem> tipInfoItemsQueue;
    //提示信息列表
    private List<string> tipInfosList;

    //玩家受伤图标
    public Image imgHurt;
    //受伤图标滞留时间
    private float hurtTime = 0.2f;
    //受伤时间计时器
    private float nowHurtTime;
    //受伤图标的CanvasGroup
    private CanvasGroup hurtCanvasGroup;

    // ESC按键缓冲时间
    private float EscBufferTime = 0.35f;
    // ESC按键上次触发计时
    private float frontEscButtonDownTime;

    //武器图标
    public Image imgGun;
    //当前子弹数量
    public Text txtNowBullet;
    //最大子弹数量
    public Text txtMaxBullet;
    //枪械图标元素
    public GameObject[] gunIcons;
    //子弹图标元素
    public GameObject[] bulletIcons;
    //颜色滞留时间
    private float colorTime = 0.25f;
    //颜色滞留时间计时器
    private float nowColorTime;
    //换弹进度条图片
    public Image imgReload;
    //玩家动画状态机
    private Animator playerAnimator;
    //玩家动画状态机信息
    private AnimatorStateInfo playerAnimatorStateInfo;

    //所有姿态状态图片
    public List<Image> playerStateImages;
    //当前姿态索引
    private int nowStateIndex;
    //翻滚Cd图标
    public Image imgRoll;
    //翻滚Cd图标储能速度
    private float rollCdSpeed;

    //准星显示状态
    private bool isAimStarShowing;

    //Boss血条区域
    public GameObject bossArea;
    //Boss真实血条
    public Image imgHpTrueBoss;
    //Boss动画血条
    public Image imgHpBoss;
    //Boss名称
    public Text txtBossName;
    //Boss血条宽度
    private float bossHpw = 650f;
    //Boss当前血条宽度
    private float nowHpwBoss;
    //Boss目标血条宽度
    private float targetHpwBoss;

    //训练场场景不需要的组件
    public GameObject[] noNeedInTrainingMode;
    //训练场需要的组件
    public GameObject[] needInTrainingMode;
    //训练场相关文字
    public Text txtFireNum;
    public Text txtHitNum;
    public Text txtHitHeadNum;
    public Text txtWoundNum;
    public Text txtSkinProNum;
    //训练场相关数据统计
    public int fireCount;
    public int hitCount;
    public int hitHeadCount;
    public int woundCount;

    //自身CanvasGroup组件
    private CanvasGroup gamePanelCanvasGroup;
    //是否透明化面板
    private bool isHideGamePanel;

    protected override void Init()
    {
        //关联CanvasGroup 隐藏受伤图标
        hurtCanvasGroup = imgHurt.GetComponent<CanvasGroup>();
        imgHurt.gameObject.SetActive(false);
        //读取炮塔元素子对象
        towersArray = towersTrans.GetComponentsInChildren<TowerItem>();
        //隐藏炮塔选择区
        towersTrans.gameObject.SetActive(false);
        //隐藏Boss血条区域
        bossArea.SetActive(false);
        //隐藏全部玩家姿态信息图标
        foreach (var image in playerStateImages)
        {
            image.gameObject.SetActive(false);
        }
        //如果是训练场模式 隐藏不需要的图标和文字
        if (GameDataMgr.Instance.nowGameMode == GameMode.TrainingMode)
        {
            foreach (var obj in noNeedInTrainingMode)
            {
                obj.gameObject.SetActive(false);
            }
            //更新训练场相关数据
            UpdateTrainingModeInfo();
        }
        //不是训练场模式 隐藏不需要的图标和文字
        else
        {
            foreach (var obj in needInTrainingMode)
            {
                obj.gameObject.SetActive(false);
            }
        }
        //初始化提示信息队列
        tipInfoItemsQueue = new Queue<TipInfoItem>();
        tipInfosList = new List<string>();
        //关联小地图
        mapImg.texture = Resources.Load<RenderTexture>("UI/MapImg/GameSceneMap");
        //关联玩家动画状态机
        playerAnimator = GameDataMgr.Instance.nowPlayerObj.GetComponent<Animator>();
        //关联自身CanvasGroup组件
        gamePanelCanvasGroup = GetComponent<CanvasGroup>();

        //更新武器相关图标
        UpdateWeaponInfo();

        btnQuit.onClick.AddListener(() =>
        {
            TipPanel tipPanel = UIManager.Instance.ShowPanel<TipPanel>();

            string tip = GameDataMgr.Instance.nowGameMode == GameMode.TrainingMode ?
                         "确认退出训练场？" : "确认退出游戏？\n中途退出游戏无任何奖励";
            tipPanel.InitInfo(tip, false);

            // 暂停时间(等待0.2s面板加载)
            Invoke("StopTime", 0.2f);

            tipPanel.InitAction((v) =>
            {
                if (v)
                {
                    //更新游戏进行状态
                    GameDataMgr.Instance.isGaming = false;
                    //隐藏游戏相关面板 
                    UIManager.Instance.HideAllPanel();
                    //存储玩家数据
                    GameDataMgr.Instance.SavePlayerData();
                    //切换场景
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
                }
                else
                {
                    //恢复游戏进行状态
                    GameDataMgr.Instance.isGaming = true;
                    //隐藏提示面板
                    UIManager.Instance.HidePanel<TipPanel>();
                }
                // 恢复时间
                Time.timeScale = 1f;
            });
        });
    }

    /// <summary>
    /// 暂停时间
    /// </summary>
    private void StopTime()
    {
        Time.timeScale = 0f;
        GameDataMgr.Instance.isGaming = false;
    }

    /// <summary>
    /// 更新武器相关图标
    /// </summary>
    public void UpdateWeaponInfo()
    {
        //根据是否为刀具武器 显示隐藏部分图标
        txtNowBullet.gameObject.SetActive(GameDataMgr.Instance.nowSelHero.atkType != 1);
        txtMaxBullet.gameObject.SetActive(GameDataMgr.Instance.nowSelHero.atkType != 1);
        foreach (GameObject image in gunIcons)
        {
            image.gameObject.SetActive(GameDataMgr.Instance.nowSelHero.atkType != 1);
        }

        //更新武器图标 子弹数据
        txtMaxBullet.text = txtNowBullet.text = GameDataMgr.Instance.nowSelHero.bulletCount.ToString();
        imgGun.sprite = Resources.Load<Sprite>($"Sprites/Gun/Gun{GameDataMgr.Instance.nowSelHero.id}");
        imgReload.fillAmount = 0;
        ChangeBulletCount(GameDataMgr.Instance.nowSelHero.bulletCount);

        //关联玩家动画状态机
        playerAnimator = GameDataMgr.Instance.nowPlayerObj.GetComponent<Animator>();
    }

    /// <summary>
    /// 更新训练场相关数据
    /// </summary>
    private void UpdateTrainingModeInfo()
    {
        txtFireNum.text = fireCount.ToString();
        if (fireCount == 0)
        {
            txtHitNum.text = txtHitHeadNum.text = "0.00%";
        }
        else
        {
            txtHitNum.text = ((float)hitCount * 100 / fireCount).ToString("F2") + "%";
            txtHitHeadNum.text = ((float)hitHeadCount * 100 / fireCount).ToString("F2") + "%";
        }
        txtWoundNum.text = woundCount.ToString();
        txtSkinProNum.text = GameDataMgr.Instance.PlayerData.buySkin.Count + "/" + GameDataMgr.Instance.SkinList.Count;
        if (GameDataMgr.Instance.PlayerData.buySkin.Count == GameDataMgr.Instance.SkinList.Count) txtSkinProNum.color = Color.red;
    }

    /// <summary>
    /// 增加开火次数
    /// </summary>
    public void AddFireCount()
    {
        ++fireCount;
        UpdateTrainingModeInfo();
    }

    /// <summary>
    /// 增加命中次数
    /// </summary>
    /// <param name="isHead">是否命中头部</param>
    /// <param name="wound">伤害值</param>
    public void AddHitCount(bool isHead, int wound)
    {
        ++hitCount;
        if (isHead) ++hitHeadCount;
        woundCount += wound;
        UpdateTrainingModeInfo();
    }

    /// <summary>
    /// 更新基地塔血量
    /// </summary>
    /// <param name="nowHp">当前血量</param>
    /// <param name="maxHp">最大血量</param>
    public void UpdateTowerHp(int nowHp, int maxHp)
    {
        txtHpNum.text = nowHp + "/" + maxHp;
        targetHpw = hpw * nowHp / maxHp;
        //即时更新真实血条背景图
        (imgHpTrue.transform as RectTransform).sizeDelta = new Vector2(targetHpw, (imgHpTrue.transform as RectTransform).sizeDelta.y);
    }

    /// <summary>
    /// 更新玩家血量
    /// </summary>
    /// <param name="nowHp">当前血量</param>
    /// <param name="maxHp">最大血量</param>
    public void UpdatePlayerHp(int nowHp, int maxHp)
    {
        txtHpNumPlayer.text = nowHp + "/" + maxHp;
        targetHpwPlayer = hpw * nowHp / maxHp;
        //即时更新真实血条背景图
        (imgHpTruePlayer.transform as RectTransform).sizeDelta = new Vector2(targetHpwPlayer, (imgHpTruePlayer.transform as RectTransform).sizeDelta.y);
    }

    /// <summary>
    /// 更新Boss血量
    /// </summary>
    /// <param name="ratio">血量比例</param>
    public void UpdateBossHp(float ratio)
    {
        targetHpwBoss = bossHpw * ratio;
        //即时更新真实血条背景图
        (imgHpTrueBoss.transform as RectTransform).sizeDelta = new Vector2(targetHpwBoss, (imgHpTrueBoss.transform as RectTransform).sizeDelta.y);
    }

    /// <summary>
    /// 更新剩余波数
    /// </summary>
    /// <param name="nowWave">当前波数</param>
    /// <param name="maxWave">最大波数</param>
    public void UpdateWaveNum(int nowWave, int maxWave)
    {
        txtWaveNum.text = nowWave + "/" + maxWave;
    }

    /// <summary>
    /// 无尽模式更新波数信息
    /// </summary>
    /// <param name="nowWave">当前波数</param>
    public void UpdateWaveNum(int nowWave)
    {
        txtWaveNum.text = nowWave.ToString();
    }

    /// <summary>
    /// 更新金币数量
    /// </summary>
    /// <param name="money">金币</param>
    public void UpdateMoneyNum(int money)
    {
        txtMoneyNum.text = money.ToString();
    }

    protected override void Update()
    {
        base.Update();

        //动画前景血量更新效果 保护区
        nowHpw = Mathf.Lerp((imgHp.transform as RectTransform).sizeDelta.x, targetHpw, Time.deltaTime * 2f);
        (imgHp.transform as RectTransform).sizeDelta = new Vector2(nowHpw, (imgHp.transform as RectTransform).sizeDelta.y);

        //动画前景血量更新效果 玩家
        nowHpwPlayer = Mathf.Lerp((imgHpPlayer.transform as RectTransform).sizeDelta.x, targetHpwPlayer, Time.deltaTime * 2f);
        (imgHpPlayer.transform as RectTransform).sizeDelta = new Vector2(nowHpwPlayer, (imgHpPlayer.transform as RectTransform).sizeDelta.y);

        //动画前景血量更新效果 Boss
        nowHpwBoss = Mathf.Lerp((imgHpBoss.transform as RectTransform).sizeDelta.x, targetHpwBoss, Time.deltaTime * 2f);
        (imgHpBoss.transform as RectTransform).sizeDelta = new Vector2(nowHpwBoss, (imgHpBoss.transform as RectTransform).sizeDelta.y);

        //退出游戏
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ESC按键未到缓冲时间 结束逻辑
            if (Time.unscaledTime - frontEscButtonDownTime < EscBufferTime) return;
            else frontEscButtonDownTime = Time.unscaledTime;

            // 游戏结束 中断逻辑
            if (GameDataMgr.Instance.nowGameMode != GameMode.TrainingMode && SceneLevelMgr.Instance.IsGameOver) return;

            TipPanel tipPanel = UIManager.Instance.GetPanel<TipPanel>();
            if (tipPanel != null)
            {
                // 恢复时间
                Time.timeScale = 1f;
                GameDataMgr.Instance.isGaming = true;
                // 隐藏提示面板
                UIManager.Instance.HidePanel<TipPanel>();
            }
            else
            {
                btnQuit.onClick.Invoke();
            }
        }

        //不是游戏状态 结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        //打开地图
        if (Input.GetKeyDown(KeyCode.M) && GameDataMgr.Instance.nowGameMode != GameMode.TrainingMode)
        {
            UIManager.Instance.ShowPanel<MapPanel>();
            //切换渲染大地图
            MapIconCamera.Instance.ChangeCameraState(true);
            mapBk.gameObject.SetActive(false);
        }

        //更新受伤图标显隐
        nowHurtTime -= Time.deltaTime;
        imgHurt.gameObject.SetActive(nowHurtTime > 0);

        //更新子弹字体颜色
        if (nowColorTime > 0)
        {
            nowColorTime -= Time.deltaTime;
        }
        else
        {
            txtNowBullet.color = Color.white;
        }
        if (txtNowBullet.text == "0")
        {
            txtNowBullet.color = Color.red;
        }

        //更新换弹进度条
        if (playerAnimator != null)
        {
            playerAnimatorStateInfo = playerAnimator.GetCurrentAnimatorStateInfo(2);
            if (playerAnimatorStateInfo.IsName("Reload"))
            {
                imgReload.fillAmount = playerAnimatorStateInfo.normalizedTime;
            }
            else
            {
                imgReload.fillAmount = 0;
            }
        }

        //更新翻滚Cd进度条
        if (imgRoll.gameObject.activeSelf)
        {
            imgRoll.fillAmount += rollCdSpeed * Time.deltaTime;
            if (imgRoll.fillAmount == 1)
            {
                imgRoll.gameObject.SetActive(false);
            }
        }

        //更新玩家姿态图标
        UpdatePlayerState();

        //更新准星面板显隐 自由视角/舞蹈状态 隐藏准星
        if (isAimStarShowing && (Camera.main.GetComponent<ThirdPersonCamera>().IsFreeLook() || GameDataMgr.Instance.nowPlayerObj.isDancing))
        {
            //隐藏准星面板
            ShowOrHideAimStarPanel(false);
        }
        else if (!isAimStarShowing && !Camera.main.GetComponent<ThirdPersonCamera>().IsFreeLook() && !GameDataMgr.Instance.nowPlayerObj.isDancing)
        {
            //显示准星面板
            ShowOrHideAimStarPanel(true);
        }

        //舞蹈状态隐藏面板
        if (GameDataMgr.Instance.nowPlayerObj.isDancing) gamePanelCanvasGroup.alpha = 0;
        //面板整体是否隐藏
        if (isHideGamePanel) gamePanelCanvasGroup.alpha = 0;
    }

    public override void ShowMe()
    {
        base.ShowMe();
        //显示准星面板
        ShowOrHideAimStarPanel(true);
    }

    public override void HideMe(UnityAction hideCallBack)
    {
        base.HideMe(hideCallBack);
        //隐藏准星面板
        ShowOrHideAimStarPanel(false);
    }

    private void OnEnable()
    {
        //显示准星面板
        ShowOrHideAimStarPanel(true);
    }

    private void OnDisable()
    {
        //隐藏准星面板
        ShowOrHideAimStarPanel(false);
    }

    /// <summary>
    /// 显示或隐藏准星面板
    /// </summary>
    /// <param name="isShow">是否显示? true显示 false隐藏</param>
    private void ShowOrHideAimStarPanel(bool isShow)
    {
        if (isShow)
        {
            UIManager.Instance.ShowPanel<AimStarPanel>();
            isAimStarShowing = true;
        }
        else
        {
            UIManager.Instance.HidePanel<AimStarPanel>(false);
            isAimStarShowing = false;
        }
    }

    /// <summary>
    /// 显示危险提示图标
    /// </summary>
    /// <param name="target">怪物位置</param>
    public void ShowWarningIcon(Transform target)
    {
        //确保对应对象池存在
        if (!ObjectPoolMgr.Instance.HasPool("WarningIcon"))
        {
            ObjectPoolMgr.Instance.RegisterPool("WarningIcon", Resources.Load<GameObject>("UI/WarningIcon"), true);
        }

        //对象池获取图标
        GameObject icon = ObjectPoolMgr.Instance.Get("WarningIcon", Vector3.zero, Quaternion.identity, icons);
        //设置对象跟随物体
        icon.GetComponent<IconFollowTarget>().followTarget = target;
    }

    /// <summary>
    /// 怪物获取血条图标
    /// </summary>
    public void ShowMonsterHpIcon(MonsterObj monsterObj)
    {
        //确保对应对象池存在
        if (!ObjectPoolMgr.Instance.HasPool("MonsterHpIcon"))
        {
            ObjectPoolMgr.Instance.RegisterPool("MonsterHpIcon", Resources.Load<GameObject>("UI/MonsterHpIcon"));
        }

        //对象池获取图标
        GameObject icon = ObjectPoolMgr.Instance.Get("MonsterHpIcon", Vector3.zero, Quaternion.identity, icons);
        icon.GetComponent<MonsterHpIcon>().InitMonster(monsterObj);
    }

    /// <summary>
    /// 显示玩家受伤图标
    /// </summary>
    public void ShowPlayerHurt()
    {
        //重置时间
        nowHurtTime = hurtTime;
        hurtCanvasGroup.alpha = 1.0f;
    }

    /// <summary>
    /// 改变子弹数量
    /// </summary>
    /// <param name="count">当前子弹数量</param>
    public void ChangeBulletCount(int count)
    {
        //更新数量 改变字体颜色
        txtNowBullet.text = count.ToString();
        txtNowBullet.color = Color.yellow;
        nowColorTime = colorTime;

        //更新备弹图标数量
        for (int i = 0; i < bulletIcons.Length; ++i)
        {
            //根据剩余弹容量占比失活部分子弹图标
            bulletIcons[bulletIcons.Length - i - 1].SetActive
            (i <= Mathf.CeilToInt(count / float.Parse(txtMaxBullet.text) * bulletIcons.Length) - 1);
        }
    }

    /// <summary>
    /// 显示炮塔区域
    /// </summary>
    public void ShowTowersItem(out bool isShowing, int level, bool isMaxLevel, int[] towersIds, int money = 0)
    {
        //防御塔区域物体显示
        towersTrans.gameObject.SetActive(true);
        //赋值展示状态
        isShowing = true;
        //展示对应id列表数量的炮塔items信息
        for (int i = 0; i < towersArray.Length; ++i)
        {
            if (i < towersIds.Length)
            {
                towersArray[i].gameObject.SetActive(true);
                towersArray[i].InitInfo(towersIds[i], level, isMaxLevel, money);
            }
            else
            {
                towersArray[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 隐藏炮塔区域
    /// </summary>
    public void HideTowersItem(out bool isShowing)
    {
        //防御塔区域物体隐藏
        towersTrans.gameObject.SetActive(false);
        //赋值展示状态
        isShowing = false;
    }

    /// <summary>
    /// 获取目标造塔炮塔信息
    /// </summary>
    /// <param name="keyCodeNum">按键数字</param>
    /// <returns></returns>
    public TowerItemInfo GetTowerItemInfo(int keyCodeNum)
    {
        if (towersArray[keyCodeNum - 1].gameObject.activeSelf == true)
        {
            return towersArray[keyCodeNum - 1].GetNowTowerItemInfo();
        }
        return new TowerItemInfo();
    }

    /// <summary>
    /// 展示一条提示信息
    /// </summary>
    /// <param name="info">提示信息</param>
    /// <param name="isRed">是否红色</param>
    public void ShowTipInfo(string info, bool isRed)
    {
        //已经提示该条信息 结束逻辑
        if (tipInfosList.Contains(info)) return;

        if (!ObjectPoolMgr.Instance.HasPool("TipInfoItem"))
        {
            ObjectPoolMgr.Instance.RegisterPool("TipInfoItem", Resources.Load<GameObject>("UI/TipInfoItem"));
        }
        //获取提示信息物体并入队列
        GameObject obj = ObjectPoolMgr.Instance.Get("TipInfoItem", Vector3.zero, Quaternion.identity, tipInfosArea);
        TipInfoItem tipInfoItem = obj.GetComponent<TipInfoItem>();
        tipInfoItem.ChangeTipInfo(info, isRed);
        tipInfoItemsQueue.Enqueue(tipInfoItem);
        tipInfosList.Add(info);
        //播放提示音效
        GameDataMgr.Instance.PlaySound(isRed ? "Music/Tips/MonsterTip" : "Music/Tips/PlayerTip", 0.6f);
        //开启淡入淡出动效+对象池回收协程
        StartCoroutine(TipInfoItemCoroutine());
    }

    //提示控件动效协程
    IEnumerator TipInfoItemCoroutine()
    {
        //记录相关信息
        TipInfoItem item = tipInfoItemsQueue.Dequeue();
        RectTransform rect = item.transform as RectTransform;
        float width = rect.sizeDelta.x;
        rect.sizeDelta = new Vector2(0, rect.sizeDelta.y);

        //淡入1/3s
        while (rect.sizeDelta.x < width)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x + width * 3 * Time.deltaTime, rect.sizeDelta.y);
            if (rect.sizeDelta.x > width) rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            yield return null;
        }

        //显示4秒
        yield return new WaitForSeconds(4);

        //淡出1/3s
        while (rect.sizeDelta.x > 0)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x - width * 3 * Time.deltaTime, rect.sizeDelta.y);
            if (rect.sizeDelta.x < 0) rect.sizeDelta = new Vector2(0, rect.sizeDelta.y);
            yield return null;
        }

        //队列删除该条提示信息
        tipInfosList.Remove(item.tipInfo.text);
        //回收前重置状态
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        //对象池回收
        ObjectPoolMgr.Instance.ReleaseObj(item.gameObject);
    }

    /// <summary>
    /// 更新玩家姿态状态图标
    /// </summary>
    private void UpdatePlayerState()
    {
        if (GameDataMgr.Instance.nowPlayerObj.isJumping || !GameDataMgr.Instance.nowPlayerObj.isEndJump)
        {
            ChangePlayerState(3);
        }
        else if (GameDataMgr.Instance.nowPlayerObj.isRolling)
        {
            ChangePlayerState(4);
        }
        else if (GameDataMgr.Instance.nowPlayerObj.crouchValue > 0.5f)
        {
            ChangePlayerState(5);
        }
        else
        {
            if (GameDataMgr.Instance.nowPlayerObj.moveDirMagnitude == 0) ChangePlayerState(0);
            else if (GameDataMgr.Instance.nowPlayerObj.moveDirMagnitude < 0.7f) ChangePlayerState(1);
            else ChangePlayerState(2);
        }
    }

    /// <summary>
    /// 整体隐藏或显示游戏面板
    /// </summary>
    /// <param name="isHide">是否隐藏</param>
    public void HideOrShowThisGamePanel(bool isHide)
    {
        isHideGamePanel = isHide;
    }

    /// <summary>
    /// 改变姿态图标
    /// </summary>
    /// <param name="stateIndex">姿态索引 0站立 1走路 2奔跑 3跳跃 4翻滚 5下蹲</param>
    private void ChangePlayerState(int stateIndex)
    {
        playerStateImages[nowStateIndex].gameObject.SetActive(false);
        playerStateImages[stateIndex].gameObject.SetActive(true);
        nowStateIndex = stateIndex;
    }

    /// <summary>
    /// 翻滚Cd提升
    /// </summary>
    /// <param name="rollTime">翻滚间隔时间</param>
    public void RollCdTip(float rollTime)
    {
        imgRoll.gameObject.SetActive(true);
        imgRoll.fillAmount = 0;
        rollCdSpeed = 1 / rollTime;
    }

    /// <summary>
    /// 初始化Boss血条区域
    /// </summary>
    /// <param name="bossName">Boss名称</param>
    public void InitBossArea(string bossName)
    {
        bossArea.SetActive(true);
        txtBossName.text = bossName;
    }
}
