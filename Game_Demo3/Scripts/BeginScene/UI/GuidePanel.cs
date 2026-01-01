using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuidePanel : BasePanel
{
    //开始界面五个按钮
    public Button btnMonster;
    public Button btnWeapon;
    public Button btnTower;
    public Button btnKeyBoard;
    public Button btnInfos;
    //开始界面返回按钮
    public Button btnBackBasic;

    //基础区域
    public GameObject basicItems;
    //百科图鉴区域
    public GameObject BKPanel;
    //键位指南区域
    public GameObject KeyBoardPanel;
    //键位指南返回按钮
    public Button btnBackKeyBorad;
    //成就信息区域
    public GameObject infosPanel;
    //成就信息返回按钮
    public Button btnBackInfos;
    //成就信息删除存档按钮
    public Button btnDeleteAllInfos;

    //百科值最大长度
    public float maxValuew = 880;
    //百科文字
    public Text txtNowBKName;
    public Text txtNowItemName;
    public Text txtIndex;
    public Text txtTips;
    public Text txtTipsDamageLevel;
    public Text[] txtValueTitles;
    //全部百科文字
    private List<Text> txtBKAll;
    //百科值图片
    public Image[] imgValues;
    //百科值物体元素
    public GameObject[] BKItems;
    //百科按钮
    public Button btnLeft;
    public Button btnRight;
    public Button btnBackBK;

    //百科相机 实例化物体位置
    private Camera showCamera;
    private Transform monsterPos;

    //成就信息所有文字
    public Text[] txtCJInfos;

    //当前展示百科类型 1怪物 2武器 3防御塔
    private int nowBKType;
    //索引
    private int nowIndex;
    private int maxIndex;
    //最大值与目标值
    private float[] maxValues;
    private float[] targetValueWs;
    //当前是否是Boss怪物最大值 用于怪物百科
    private bool isBossValues;
    //当前实例化的游戏物体
    private GameObject nowObj;
    //所有信息列表
    private List<MonsterInfo> monsters;
    private List<MonsterInfo> normalMonsters;
    private List<MonsterInfo> bossMonsters;
    private List<HeroInfo> heroInfos;
    private List<TowerInfo> towerInfos;

    protected override void Init()
    {
        //初始化三个信息列表
        monsters = new List<MonsterInfo>(GameDataMgr.Instance.MonsterList);
        heroInfos = new List<HeroInfo>(GameDataMgr.Instance.HeroList);
        towerInfos = new List<TowerInfo>(GameDataMgr.Instance.TowerList);
        //关联百科全部文字
        txtBKAll = new List<Text>() { txtNowBKName, txtNowItemName, txtTipsDamageLevel };
        txtBKAll.AddRange(txtValueTitles);
        //初始化各个容器
        normalMonsters = new List<MonsterInfo>();
        bossMonsters = new List<MonsterInfo>();
        targetValueWs = new float[5];
        //区分Boss和普通怪物
        foreach (MonsterInfo monsterInfo in monsters)
        {
            if (monsterInfo.isBoss) bossMonsters.Add(monsterInfo);
            else normalMonsters.Add(monsterInfo);
        }
        //怪物列表按危险等级排序
        monsters.Sort((a, b) =>
        {
            if (a.tipsDmgLevel < b.tipsDmgLevel) return -1;
            else if (a.tipsDmgLevel > b.tipsDmgLevel) return 1;
            else if (a.id < b.id) return -1;
            else return 0;
        });
        //关联相机和物品实例化点
        showCamera = GameObject.Find("ShowCamera").GetComponent<Camera>();
        monsterPos = GameObject.Find("MonsterPos").transform;
        //隐藏百科界面
        BKPanel.SetActive(false);
        //隐藏键位指南界面
        KeyBoardPanel.SetActive(false);
        //隐藏成就统计界面
        infosPanel.SetActive(false);
        //怪物百科按钮
        btnMonster.onClick.AddListener(() =>
        {
            ChangeBKPanel(1);
            UpdateBKInfo();
        });
        //武器百科按钮
        btnWeapon.onClick.AddListener(() =>
        {
            ChangeBKPanel(2);
            UpdateBKInfo();
        });
        //防御塔百科按钮
        btnTower.onClick.AddListener(() =>
        {
            ChangeBKPanel(3);
            UpdateBKInfo();
        });
        //基础界面返回按钮
        btnBackBasic.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<GuidePanel>();
        });
        //百科界面返回按钮
        btnBackBK.onClick.AddListener(() =>
        {
            Destroy(nowObj);
            BKPanel.gameObject.SetActive(false);
            basicItems.gameObject.SetActive(true);
        });
        //百科界面左按键
        btnLeft.onClick.AddListener(() =>
        {
            UpdateIndexNum(false);
            UpdateBKInfo();
        });
        //百科界面右按键
        btnRight.onClick.AddListener(() =>
        {
            UpdateIndexNum(true);
            UpdateBKInfo();
        });
        //键位指南按钮
        btnKeyBoard.onClick.AddListener(() =>
        {
            basicItems.gameObject.SetActive(false);
            KeyBoardPanel.gameObject.SetActive(true);
        });
        //键位指南返回按钮
        btnBackKeyBorad.onClick.AddListener(() =>
        {
            basicItems.gameObject.SetActive(true);
            KeyBoardPanel.gameObject.SetActive(false);
        });
        //成就统计按钮
        btnInfos.onClick.AddListener(() =>
        {
            basicItems.gameObject.SetActive(false);
            infosPanel.gameObject.SetActive(true);
            //更新成就统计信息
            UpdateCJPanelInfos();
        });
        //成就统计返回按钮
        btnBackInfos.onClick.AddListener(() =>
        {
            basicItems.gameObject.SetActive(true);
            infosPanel.gameObject.SetActive(false);
        });
        //成就统计删除存档按钮
        btnDeleteAllInfos.onClick.AddListener(() =>
        {
            TipPanel tipPanel = UIManager.Instance.ShowPanel<TipPanel>();
            tipPanel.InitInfo("删除当前游戏存档 恢复初始状态<color=red>\n警告\n一切关卡进度、武器角色收集进度将被清空\n确定删除当前存档？</color>", false);
            tipPanel.InitAction((v) =>
            {
                //确认
                if (v)
                {
                    GameDataMgr.Instance.ResetPlayerData();
                    UpdateCJPanelInfos();
                    tipPanel.ClearAction();
                    tipPanel.InitInfo("成功删除游戏存档！");
                }
                //取消
                else
                {
                    UIManager.Instance.HidePanel<TipPanel>();
                }
            });
        });
    }

    //切换百科界面
    private void ChangeBKPanel(int type)
    {
        //切换百科类型
        nowBKType = type;
        //切换界面
        basicItems.SetActive(false);
        BKPanel.SetActive(true);
        //更新颜色
        ChangeBKTextsAndImgsColor();
        //更新索引
        nowIndex = 0;
        //重置值图片宽度
        foreach (var img in imgValues)
            (img.transform as RectTransform).sizeDelta = new Vector2(0, (img.transform as RectTransform).sizeDelta.y);
        //清空说明信息
        txtTips.text = "";
        txtTipsDamageLevel.text = "";

        switch (type)
        {
            //怪物
            case 1:
                foreach (var item in BKItems) item.SetActive(true);
                maxIndex = monsters.Count - 1;
                txtNowBKName.text = "怪物百科";
                txtValueTitles[0].text = "生命值";
                txtValueTitles[1].text = "攻击力";
                txtValueTitles[2].text = "攻击范围";
                txtValueTitles[3].text = "移动速度";
                txtValueTitles[4].text = "攻击速度";
                maxValues = new float[5];
                //基于普通怪物的最大值
                UpdateMonsterMaxValues(false);
                isBossValues = false;
                break;
            //武器
            case 2:
                foreach (var item in BKItems) item.SetActive(true);
                maxIndex = heroInfos.Count - 1;
                txtNowBKName.text = "武器百科";
                txtValueTitles[0].text = "攻击力";
                txtValueTitles[1].text = "最大射速";
                txtValueTitles[2].text = "有效射程";
                txtValueTitles[3].text = "散布控制(瞄准)";
                txtValueTitles[4].text = "后座力控制";
                maxValues = new float[5];
                //更新各属性最大值
                maxValues[0] = MathCalTool.Instance.CalMax<HeroInfo>("atk", heroInfos);
                maxValues[1] = MathCalTool.Instance.CalMax<HeroInfo>("shootSpeed", heroInfos);
                maxValues[2] = MathCalTool.Instance.CalMax<HeroInfo>("atkDistance", heroInfos);
                List<HeroInfo> gunList = new List<HeroInfo>();
                foreach (var hero in heroInfos)
                {
                    if (hero.atkType == 2) gunList.Add(hero);
                }
                maxValues[3] = MathCalTool.Instance.CalMin<HeroInfo>("bulletAimOffset", gunList);
                maxValues[4] = MathCalTool.Instance.CalMin<HeroInfo>("xRecoil", gunList) +
                               MathCalTool.Instance.CalMin<HeroInfo>("yRecoil", gunList);
                break;
            //防御塔
            case 3:
                for (int i = 0; i < BKItems.Length; ++i)
                {
                    if (i > 2) BKItems[i].SetActive(false);
                }
                maxIndex = towerInfos.Count - 1;
                txtNowBKName.text = "防御塔百科";
                txtValueTitles[0].text = "攻击力";
                txtValueTitles[1].text = "攻击速度";
                txtValueTitles[2].text = "攻击范围";
                maxValues = new float[3];
                //更新各属性最大值
                maxValues[0] = MathCalTool.Instance.CalMax<TowerInfo>("atk", towerInfos);
                maxValues[1] = MathCalTool.Instance.CalMin<TowerInfo>("offsetTime", towerInfos);
                maxValues[2] = MathCalTool.Instance.CalMax<TowerInfo>("atkRange", towerInfos);
                break;
        }
        txtIndex.text = $"{nowIndex + 1}/{maxIndex + 1}";
    }

    //更新怪物属性最大值
    private void UpdateMonsterMaxValues(bool isBoss)
    {
        if (isBoss)
        {
            maxValues[0] = MathCalTool.Instance.CalMax<MonsterInfo>("hp", bossMonsters);
            maxValues[1] = MathCalTool.Instance.CalMax<MonsterInfo>("atk", bossMonsters);
            maxValues[2] = MathCalTool.Instance.CalMax<MonsterInfo>("atkRange", bossMonsters);
            maxValues[3] = MathCalTool.Instance.CalMax<MonsterInfo>("moveSpeedRatio", bossMonsters);
            maxValues[4] = MathCalTool.Instance.CalMin<MonsterInfo>("atkCd", bossMonsters);
        }
        else
        {
            maxValues[0] = MathCalTool.Instance.CalMax<MonsterInfo>("hp", normalMonsters);
            maxValues[1] = MathCalTool.Instance.CalMax<MonsterInfo>("atk", normalMonsters);
            maxValues[2] = MathCalTool.Instance.CalMax<MonsterInfo>("atkRange", normalMonsters);
            maxValues[3] = MathCalTool.Instance.CalMax<MonsterInfo>("moveSpeedRatio", normalMonsters);
            maxValues[4] = MathCalTool.Instance.CalMin<MonsterInfo>("atkCd", normalMonsters);
        }
    }

    //更新索引
    private void UpdateIndexNum(bool addNum)
    {
        if (addNum) ++nowIndex;
        else --nowIndex;

        if (nowIndex < 0) nowIndex = maxIndex;
        else if (nowIndex > maxIndex) nowIndex = 0;

        txtIndex.text = $"{nowIndex + 1}/{maxIndex + 1}";
    }

    //更新百科界面信息
    private void UpdateBKInfo()
    {
        switch (nowBKType)
        {
            //怪物
            case 1:
                Destroy(nowObj);
                MonsterInfo monsterInfo = monsters[nowIndex];
                //全部层都可见
                showCamera.cullingMask = -1;
                //根据是否是Boss更新最大值数组 字体/值图片颜色
                if (monsterInfo.isBoss && !isBossValues)
                {
                    UpdateMonsterMaxValues(true);
                    isBossValues = true;
                    ChangeBKTextsAndImgsColor();
                }
                else if (!monsterInfo.isBoss && isBossValues)
                {
                    UpdateMonsterMaxValues(false);
                    isBossValues = false;
                    ChangeBKTextsAndImgsColor();
                }
                //实例化怪物
                nowObj = Instantiate(Resources.Load<GameObject>(monsterInfo.res), monsterPos.position, monsterPos.rotation);
                DestroyImmediate(nowObj.GetComponent<MonsterObj>());
                DestroyImmediate(nowObj.GetComponent<BossObj>());
                //改变名称
                txtNowItemName.text = monsterInfo.tipsName;
                //更新等级提示信息
                txtTipsDamageLevel.text = $"危险等级:{monsterInfo.tipsDmgLevel}";
                //改变目标值数组
                targetValueWs[0] = monsterInfo.hp / maxValues[0] * maxValuew;
                targetValueWs[1] = monsterInfo.atk / maxValues[1] * maxValuew;
                targetValueWs[2] = monsterInfo.atkRange / maxValues[2] * maxValuew;
                targetValueWs[3] = monsterInfo.moveSpeedRatio / maxValues[3] * maxValuew;
                targetValueWs[4] = maxValues[4] / monsterInfo.atkCd * maxValuew;
                break;
            //武器
            case 2:
                Destroy(nowObj);
                HeroInfo heroInfo = heroInfos[nowIndex];
                //仅武器层可见
                showCamera.cullingMask = 1 << LayerMask.NameToLayer("Weapon");
                //实例化武器
                nowObj = Instantiate(Resources.Load<GameObject>($"Hero/Gun/{heroInfo.id}"),
                                     monsterPos.position + Vector3.up * 2, monsterPos.rotation);
                nowObj.AddComponent<RotateShowObj>();
                nowObj.transform.rotation *= Quaternion.AngleAxis(180, Vector3.right);
                nowObj.transform.localScale = Vector3.one * 1.8f;
                //改变名称
                txtNowItemName.text = heroInfo.tips;
                //改变目标值数组
                targetValueWs[0] = heroInfo.atk / maxValues[0] * maxValuew;
                targetValueWs[1] = heroInfo.shootSpeed / maxValues[1] * maxValuew;
                targetValueWs[2] = heroInfo.atkDistance / maxValues[2] * maxValuew;
                if (heroInfo.bulletAimOffset < 0.001f) targetValueWs[3] = 0;
                else targetValueWs[3] = maxValues[3] / heroInfo.bulletAimOffset * maxValuew;
                if (heroInfo.xRecoil + heroInfo.yRecoil < 0.001f) targetValueWs[4] = 0;
                else targetValueWs[4] = maxValues[4] / (heroInfo.xRecoil + heroInfo.yRecoil) * maxValuew;
                break;
            //防御塔
            case 3:
                Destroy(nowObj);
                TowerInfo towerInfo = towerInfos[nowIndex];
                //全部层都可见
                showCamera.cullingMask = -1;
                //获取玩家最大造塔等级
                int maxTowerLevel = GameDataMgr.Instance.PlayerData.maxTowerLevel;
                //实例化防御塔
                if (nowIndex == 0)
                {
                    nowObj = Instantiate(Resources.Load<GameObject>("Towers/Tower1"), monsterPos.position, monsterPos.rotation);
                }
                else if (maxTowerLevel > 1)
                {
                    nowObj = Instantiate(Resources.Load<GameObject>($"Towers/Tower{towerInfo.id}_{maxTowerLevel - 1}"),
                                                                    monsterPos.position, monsterPos.rotation);
                }
                else
                {
                    nowObj = Instantiate(Resources.Load<GameObject>($"Towers/Tower{towerInfo.id}_1"),
                                                                    monsterPos.position, monsterPos.rotation);
                }
                //改变名称
                txtNowItemName.text = towerInfo.name;
                //更新提示信息
                txtTips.text = towerInfo.tips;
                //改变目标值数组
                targetValueWs[0] = towerInfo.atk / maxValues[0] * maxValuew;
                targetValueWs[1] = maxValues[1] / towerInfo.offsetTime * maxValuew;
                targetValueWs[2] = towerInfo.atkRange / maxValues[2] * maxValuew;
                break;
        }
    }

    //改变百科界面字体颜色
    private void ChangeBKTextsAndImgsColor()
    {
        switch (nowBKType)
        {
            case 1:
                if (isBossValues)
                {
                    foreach (var txt in txtBKAll) txt.color = new Color(1, 0, 1, 1);
                    foreach (var img in imgValues) img.color = new Color(1, 0, 1, 1);
                }
                else
                {
                    foreach (var txt in txtBKAll) txt.color = Color.red;
                    foreach (var img in imgValues) img.color = Color.red;
                }
                break;
            case 2:
                foreach (var txt in txtBKAll) txt.color = Color.green;
                foreach (var img in imgValues) img.color = Color.green;
                break;
            case 3:
                foreach (var txt in txtBKAll) txt.color = Color.blue;
                foreach (var img in imgValues) img.color = Color.blue;
                break;
        }
    }

    //更新成就统计界面文字信息
    private void UpdateCJPanelInfos()
    {
        //玩家数据
        PlayerData playerData = GameDataMgr.Instance.PlayerData;
        //杀敌数量
        txtCJInfos[0].text = playerData.killMonsterCount.ToString();
        //武器解锁进度
        txtCJInfos[1].text = $"{playerData.buyHero.Count + 1}/{GameDataMgr.Instance.HeroList.Count}";
        if (playerData.buyHero.Count + 1 == GameDataMgr.Instance.HeroList.Count) txtCJInfos[1].color = Color.red;
        else txtCJInfos[1].color = Color.black;
        //防御塔升级进度
        txtCJInfos[2].text = $"{playerData.maxTowerLevel}/4";
        if (playerData.maxTowerLevel == 4) txtCJInfos[2].color = Color.red;
        else txtCJInfos[2].color = Color.black;
        //角色解锁进度
        txtCJInfos[3].text = $"{playerData.buySkin.Count}/{GameDataMgr.Instance.SkinList.Count}";
        if (playerData.buySkin.Count == GameDataMgr.Instance.SkinList.Count) txtCJInfos[3].color = Color.red;
        else txtCJInfos[3].color = Color.black;
        //击败Boss数量
        txtCJInfos[4].text = $"{playerData.killBoss.Count}/{bossMonsters.Count}";
        if (playerData.killBoss.Count == bossMonsters.Count) txtCJInfos[4].color = Color.red;
        else txtCJInfos[4].color = Color.black;
        //总关卡进度
        int maxLevelNum = GameDataMgr.Instance.SceneLevelMonsterList.Count;
        int playerLevelCount = -1;
        foreach (var level in playerData.sceneLevelInfo) playerLevelCount += level;
        txtCJInfos[5].text = playerData.isWinAllGame ? $"{maxLevelNum}/{maxLevelNum}" : $"{playerLevelCount}/{maxLevelNum}";
        if (playerData.isWinAllGame) txtCJInfos[5].color = Color.red;
        else txtCJInfos[5].color = Color.black;
        //金钱数量
        txtCJInfos[11].text = $"${playerData.money}";

        //增益信息相关
        SkinAwardControlInfo skinAwardControlInfo = GameDataMgr.Instance.AllControlInfo.skinAwardControlInfo;
        int consumeMoney = playerData.consumeMoney;
        //角色生命值
        txtCJInfos[6].text = $"{skinAwardControlInfo.playerHp * consumeMoney + playerData.playerBasicHp}";
        //基地生命值
        txtCJInfos[7].text = $"{skinAwardControlInfo.mainTowerHp * consumeMoney + playerData.mainTowerBasicHp}";
        //关卡初始筑晶
        txtCJInfos[8].text = $"{skinAwardControlInfo.basicMoney * consumeMoney + playerData.basicMoney}";
        //武器伤害系数
        txtCJInfos[9].text = ((skinAwardControlInfo.playerAtkRatio * consumeMoney + 1) * 100).ToString("F1") + "%";
        //防御塔伤害系数
        txtCJInfos[10].text = ((skinAwardControlInfo.towerAtkRatio * consumeMoney + 1) * 100).ToString("F1") + "%";
    }

    //帧更新更新数值条长度
    protected override void Update()
    {
        base.Update();
        //百科面板未激活 结束逻辑
        if (!BKPanel.activeSelf) { return; }
        //渐变更新各个数值条长度
        for (int i = 0; i < imgValues.Length; ++i)
        {
            (imgValues[i].transform as RectTransform).sizeDelta = new Vector2
            (Mathf.Lerp((imgValues[i].transform as RectTransform).sizeDelta.x, targetValueWs[i], Time.deltaTime * 6),
            (imgValues[i].transform as RectTransform).sizeDelta.y);
        }
    }
}
