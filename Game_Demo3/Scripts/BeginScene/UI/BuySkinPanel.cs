using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 角色展示列表类型枚举
/// </summary>
public enum E_ShowSkinState
{
    All,
    Buy,
    SSS,
    S,
    A,
    B,
}

public class BuySkinPanel : BasePanel
{
    //右下角区域按钮
    public Button btnUnlock;
    public Button btnChoose;
    public Button btnBack;

    //左下角区域单选框开关
    public Toggle togDance;
    public Toggle togRotate;
    public Toggle togShow;
    public GameObject[] hideInShowModeObjs;

    //选择角色区域按钮
    public Button btnSelNow;
    public Button btnSelBasic;
    public Button btnSelAll;
    public Button btnSelBuy;
    public Button btnSelSSS;
    public Button btnSelS;
    public Button btnSelA;
    public Button btnSelB;

    //索引信息和左右按钮
    public Button btnLeft;
    public Button btnRight;
    public Text txtIndexInfo;

    //增伤信息与当前增伤
    public Text txtEffValue;
    public Text txtNowEffValue;

    //皮肤名字与金钱
    public Text txtSkinName;
    public Text txtMoney;
    //玩家拥有金钱
    public Text txtPlayerMoney;
    //角色收集进度
    public Text txtSkinCount;

    //角色脸部补光灯
    public Transform skinFaceLight;

    //当前展示角色列表
    private E_ShowSkinState nowState;
    private SkinInfo nowShowSkinInfo;
    private SkinInfo nowSelSkinInfo;
    //当前对应列表的索引值
    private int nowIndex;
    //列表对应字典
    private Dictionary<E_ShowSkinState, List<SkinInfo>> stateToListDic;

    //六类角色信息列表
    private List<SkinInfo> allSkinList;
    private List<SkinInfo> buySkinList;
    private List<SkinInfo> SSSSkinList;
    private List<SkinInfo> SSkinList;
    private List<SkinInfo> ASkinList;
    private List<SkinInfo> BSkinList;

    //角色模型实例化位置
    private Transform showSkinPoint;
    //当前角色模型
    private GameObject nowSkinObj;
    //是否点击中角色
    private bool isClickHero;
    //当前旋转角度
    private float nowRotateAngle;
    //舞蹈动画状态机
    private RuntimeAnimatorController dance9RuntimeAnimatorController;
    //舞蹈进行进度
    private float danceProgress;

    protected override void Init()
    {
        //等待场景加载完毕
        if (SceneManager.GetActiveScene().name != "ShowSkinScene")
        {
            Invoke("Init", 0.2f);
            return;
        }

        //初始化文字提示信息
        txtPlayerMoney.text = "$" + GameDataMgr.Instance.PlayerData.money;
        txtEffValue.text = CalEffValueString(false);
        txtNowEffValue.text = CalEffValueString();

        //关联模型展示点
        showSkinPoint = GameObject.Find("SkinPoint").transform;
        //关联面部补光灯
        skinFaceLight = GameObject.Find("SkinFaceLight").transform;
        //关联舞蹈动画状态机
        dance9RuntimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Hero/Skin/Dance/Animator/Dance9");
        //初始化全部皮肤状态
        nowState = E_ShowSkinState.All;
        //初始化全部皮肤列表
        allSkinList = GameDataMgr.Instance.SkinList;
        buySkinList = new List<SkinInfo>();
        SSSSkinList = new List<SkinInfo>();
        SSkinList = new List<SkinInfo>();
        ASkinList = new List<SkinInfo>();
        BSkinList = new List<SkinInfo>();

        //更新记录各个列表
        foreach (SkinInfo skinInfo in allSkinList)
        {
            if (GameDataMgr.Instance.PlayerData.buySkin.Contains(skinInfo.id))
            {
                buySkinList.Add(skinInfo);
            }

            switch (skinInfo.level)
            {
                case 1:
                    BSkinList.Add(skinInfo);
                    break;
                case 2:
                    ASkinList.Add(skinInfo);
                    break;
                case 3:
                    SSkinList.Add(skinInfo);
                    break;
                case 4:
                    SSSSkinList.Add(skinInfo);
                    break;
            }
        }

        //初始化状态对应列表字典
        stateToListDic = new Dictionary<E_ShowSkinState, List<SkinInfo>>()
        {
            {E_ShowSkinState.All, allSkinList},
            {E_ShowSkinState.Buy, buySkinList},
            {E_ShowSkinState.SSS, SSSSkinList},
            {E_ShowSkinState.S, SSkinList },
            {E_ShowSkinState.A, ASkinList},
            {E_ShowSkinState.B, BSkinList},
        };

        //初始化角色模型
        if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo != null)
        {
            nowSelSkinInfo = GameDataMgr.Instance.PlayerData.nowSelSkinInfo;
            nowIndex = nowSelSkinInfo.id - 1;
            UpdateAllUIInfo();
        }
        else
        {
            CreateBasicSkin();
        }

        //更新角色收集进度
        txtSkinCount.text = buySkinList.Count + "/" + allSkinList.Count;

        //舞蹈开关
        togDance.onValueChanged.AddListener((v) =>
        {
            if (nowShowSkinInfo == null) return;
            nowSkinObj.GetComponent<Animator>().runtimeAnimatorController = v ? dance9RuntimeAnimatorController : null;
        });
        //相机旋转开关
        togRotate.onValueChanged.AddListener((v) =>
        {
            Camera.main.GetComponent<Animator>().enabled = v;
        });
        //风景模式开关
        togShow.onValueChanged.AddListener((v) =>
        {
            foreach (GameObject obj in hideInShowModeObjs)
            {
                obj.SetActive(!v);
            }
        });
        //返回按钮
        btnBack.onClick.AddListener(() =>
        {
            //StartCoroutine("BackBeginSceneCoroutine");
            //切换场景
            UIManager.Instance.HidePanel<BuySkinPanel>();
            LoadScenePanel loadScenePanel = UIManager.Instance.ShowPanel<LoadScenePanel>();
            loadScenePanel.InitInfo("BeginScene", "开始场景", () =>
            {
                //相机动画
                Camera.main.GetComponent<CameraAnimator>().TurnFarOrClose(() =>
                {
                    UIManager.Instance.ShowPanel<ChooseHeroPanel>();
                }, false);
            });
        });
        //回到当前选择按钮
        btnSelNow.onClick.AddListener(() =>
        {
            nowState = E_ShowSkinState.All;
            if (nowSelSkinInfo == null)
            {
                CreateBasicSkin();
            }
            else
            {
                nowIndex = nowSelSkinInfo.id - 1;
                UpdateAllUIInfo();
            }
        });
        //默认角色按钮
        btnSelBasic.onClick.AddListener(() =>
        {
            CreateBasicSkin();
        });
        //全部角色
        btnSelAll.onClick.AddListener(() =>
        {
            nowIndex = 0;
            nowState = E_ShowSkinState.All;
            UpdateAllUIInfo();
        });
        //已购买角色
        btnSelBuy.onClick.AddListener(() =>
        {
            if (buySkinList.Count > 0)
            {
                nowIndex = 0;
                nowState = E_ShowSkinState.Buy;
                UpdateAllUIInfo();
            }
            else
            {
                //断绝面部补光灯父子关系 避免被删除
                skinFaceLight.SetParent(null);
                //删除上个角色模型
                Destroy(nowSkinObj);
                //更新索引信息
                txtIndexInfo.text = "";
                //隐藏左右按钮
                btnLeft.gameObject.SetActive(false);
                btnRight.gameObject.SetActive(false);
                //关联模型信息
                nowShowSkinInfo = null;
                //更新文字信息
                txtSkinName.text = "无角色";
                txtSkinName.color = Color.white;
                txtMoney.text = "";
                //更新两个按钮的状态
                btnUnlock.gameObject.SetActive(false);
                btnChoose.gameObject.SetActive(false);
            }
        });
        // SSS角色
        btnSelSSS.onClick.AddListener(() =>
        {
            nowIndex = 0;
            nowState = E_ShowSkinState.SSS;
            UpdateAllUIInfo();
        });
        // S角色
        btnSelS.onClick.AddListener(() =>
        {
            nowIndex = 0;
            nowState = E_ShowSkinState.S;
            UpdateAllUIInfo();
        });
        // A角色
        btnSelA.onClick.AddListener(() =>
        {
            nowIndex = 0;
            nowState = E_ShowSkinState.A;
            UpdateAllUIInfo();
        });
        // B角色
        btnSelB.onClick.AddListener(() =>
        {
            nowIndex = 0;
            nowState = E_ShowSkinState.B;
            UpdateAllUIInfo();
        });
        // 左按键
        btnLeft.onClick.AddListener(() =>
        {
            if (--nowIndex < 0) nowIndex = stateToListDic[nowState].Count - 1;
            UpdateAllUIInfo();
        });
        // 右按键
        btnRight.onClick.AddListener(() =>
        {
            if (++nowIndex == stateToListDic[nowState].Count) nowIndex = 0;
            UpdateAllUIInfo();
        });
        // 解锁按钮
        btnUnlock.onClick.AddListener(() =>
        {
            PlayerData playerData = GameDataMgr.Instance.PlayerData;
            if (playerData.money >= nowShowSkinInfo.money)
            {
                TipPanel tipPanel = UIManager.Instance.ShowPanel<TipPanel>();
                tipPanel.InitInfo($"是否确认解锁角色？\n拥有金钱<color=yellow>${playerData.money}</color>\n需要金钱${nowShowSkinInfo.money}", false);
                tipPanel.InitAction((v) =>
                {
                    if (v)
                    {
                        //解锁成功逻辑
                        playerData.money -= nowShowSkinInfo.money;
                        playerData.buySkin.Add(nowShowSkinInfo.id);
                        playerData.consumeMoney += nowShowSkinInfo.money / 3000;
                        GameDataMgr.Instance.SavePlayerData();
                        //更新已购买角色列表
                        buySkinList.Add(nowShowSkinInfo);
                        //更新增益信息
                        txtNowEffValue.text = CalEffValueString();
                        //更新解锁按钮等等信息
                        UpdateAllUIInfo();
                        //更新解锁成功提示
                        tipPanel.ClearAction();
                        tipPanel.InitInfo("解锁成功");
                    }
                    else
                    {
                        UIManager.Instance.HidePanel<TipPanel>();
                    }
                });
            }
            else
            {
                TipPanel tipPanel = UIManager.Instance.ShowPanel<TipPanel>();
                tipPanel.InitInfo($"金钱不足\n拥有金钱<color=yellow>${playerData.money}</color>\n需要金钱${nowShowSkinInfo.money}");
            }
        });
        // 选择按钮
        btnChoose.onClick.AddListener(() =>
        {
            //获取子对象Text信息
            Text txtInfo = btnChoose.transform.GetComponentInChildren<Text>();
            if (txtInfo.text == "✔") return;
            //勾选当前角色
            txtInfo.text = "✔";
            if (nowShowSkinInfo != null)
            {
                GameDataMgr.Instance.PlayerData.nowSelSkinInfo = stateToListDic[nowState][nowIndex];
                nowSelSkinInfo = stateToListDic[nowState][nowIndex];
            }
            else
            {
                GameDataMgr.Instance.PlayerData.nowSelSkinInfo = null;
                nowSelSkinInfo = null;
            }
            GameDataMgr.Instance.SavePlayerData();
        });
    }

    //更新计算当前玩家增益值
    private string CalEffValueString(bool isNowEff = true)
    {
        SkinAwardControlInfo skinAwardControlInfo = GameDataMgr.Instance.AllControlInfo.skinAwardControlInfo;
        int consumeMoney = isNowEff ? GameDataMgr.Instance.PlayerData.consumeMoney : 1;
        return string.Format($"{skinAwardControlInfo.playerHp * consumeMoney}\n{skinAwardControlInfo.mainTowerHp * consumeMoney}\n{skinAwardControlInfo.basicMoney * consumeMoney}\n" +
            $"{(skinAwardControlInfo.playerAtkRatio * 100 * consumeMoney).ToString("F1")}%\n{(skinAwardControlInfo.towerAtkRatio * 100 * consumeMoney).ToString("F1")}%");
    }

    //根据当前索引更新界面显示
    private void UpdateAllUIInfo()
    {
        //更新玩家金钱显示
        txtPlayerMoney.text = "$" + GameDataMgr.Instance.PlayerData.money;
        //更新角色收集进度
        txtSkinCount.text = buySkinList.Count + "/" + allSkinList.Count;
        //当前状态对应的角色列表
        List<SkinInfo> skinInfos = stateToListDic[nowState];

        //记录舞蹈动画进度
        if (nowSkinObj && nowSkinObj.GetComponent<Animator>().runtimeAnimatorController != null && nowShowSkinInfo != null)
        {
            danceProgress = Mathf.Clamp01(nowSkinObj.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime);
        }

        //实例化新皮肤模型 设置正确位置与旋转
        GameObject skinObj = Instantiate(Resources.Load<GameObject>(skinInfos[nowIndex].res), showSkinPoint.position, Quaternion.identity);
        if (nowSkinObj) skinObj.transform.localRotation = Quaternion.Euler(0, nowRotateAngle, 0);
        else skinObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

        //删除新皮肤模型不需要的脚本
        Destroy(skinObj.GetComponent<PlayerObj>());
        Destroy(skinObj.GetComponent<PlayerRotationController>());
        Destroy(skinObj.GetComponent<PlayerIKController>());

        //更新舞蹈动作状态机
        if (togDance.isOn)
        {
            skinObj.GetComponent<Animator>().runtimeAnimatorController = dance9RuntimeAnimatorController;
            skinObj.GetComponent<Animator>().Play("Dance9", 0, danceProgress);
        }

        //更新补光灯位置与父物体
        skinFaceLight.SetParent(skinObj.transform);
        //设置补光灯世界位置为角色身高高度向前一个单位
        skinFaceLight.position = skinObj.transform.position + Vector3.up * 2.3f + skinObj.transform.forward;
        //光源对准角色面部
        skinFaceLight.LookAt(skinObj.transform.position + Vector3.up * 2.3f);

        //删除上个角色模型
        Destroy(nowSkinObj);

        //更新索引信息
        txtIndexInfo.text = nowIndex + 1 + "/" + skinInfos.Count;
        //显示左右按钮
        btnLeft.gameObject.SetActive(true);
        btnRight.gameObject.SetActive(true);
        //关联模型
        nowSkinObj = skinObj;
        //记录原始旋转角度
        nowRotateAngle = nowSkinObj.transform.localRotation.eulerAngles.y;
        //关联模型信息
        nowShowSkinInfo = skinInfos[nowIndex];
        //更新文字信息
        txtSkinName.text = nowShowSkinInfo.name;
        switch (nowShowSkinInfo.level)
        {
            case 1:
                txtSkinName.color = Color.white;
                break;
            case 2:
                txtSkinName.color = Color.green;
                break;
            case 3:
                txtSkinName.color = Color.cyan;
                break;
            case 4:
                txtSkinName.color = Color.red;
                break;
        }
        //更新两个按钮的状态
        btnUnlock.gameObject.SetActive(!GameDataMgr.Instance.PlayerData.buySkin.Contains(nowShowSkinInfo.id));
        txtMoney.text = btnUnlock.gameObject.activeSelf ? "$" + nowShowSkinInfo.money : "";
        btnChoose.gameObject.SetActive(!btnUnlock.gameObject.activeSelf);
        Text text = btnChoose.transform.GetComponentInChildren<Text>();
        if (nowShowSkinInfo == null || nowSelSkinInfo == null) text.text = nowShowSkinInfo == nowSelSkinInfo ? "✔" : "选择";
        else text.text = nowShowSkinInfo.id == nowSelSkinInfo.id ? "✔" : "选择";
    }

    //实例化默认角色
    private void CreateBasicSkin()
    {
        //记录舞蹈动画进度
        if (nowSkinObj && nowSkinObj.GetComponent<Animator>().runtimeAnimatorController != null && nowShowSkinInfo != null)
        {
            danceProgress = Mathf.Clamp01(nowSkinObj.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime);
        }

        //实例化皮肤模型 设置正确位置与旋转
        GameObject skinObj = Instantiate(Resources.Load<GameObject>(GameDataMgr.Instance.nowSelHero.res), showSkinPoint.position, Quaternion.identity);
        if (nowSkinObj) skinObj.transform.localRotation = Quaternion.Euler(0, nowRotateAngle, 0);
        else skinObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

        //删除不需要的脚本
        Destroy(skinObj.GetComponent<PlayerObj>());
        Destroy(skinObj.GetComponent<PlayerRotationController>());
        Destroy(skinObj.GetComponent<PlayerIKController>());

        //更新补光灯位置与父物体
        skinFaceLight.SetParent(skinObj.transform);
        //设置补光灯世界位置为角色身高高度向前一个单位
        skinFaceLight.position = skinObj.transform.position + Vector3.up * 2f + skinObj.transform.forward;
        //光源对准角色面部
        skinFaceLight.LookAt(skinObj.transform.position + Vector3.up * 2f);

        //删除上个角色模型
        Destroy(nowSkinObj);

        //更新索引信息
        txtIndexInfo.text = "";
        //隐藏左右按钮
        btnLeft.gameObject.SetActive(false);
        btnRight.gameObject.SetActive(false);
        //关联模型
        nowSkinObj = skinObj;
        //记录原始旋转角度
        nowRotateAngle = nowSkinObj.transform.localRotation.eulerAngles.y;
        //关联模型信息
        nowShowSkinInfo = null;
        //更新文字信息
        txtSkinName.text = "默认角色";
        txtSkinName.color = Color.white;
        txtMoney.text = "";
        //更新两个按钮的状态
        btnUnlock.gameObject.SetActive(false);
        btnChoose.gameObject.SetActive(true);
        btnChoose.transform.GetComponentInChildren<Text>().text = nowSelSkinInfo == null ? "✔" : "选择";
    }

    public override void HideMe(UnityAction hideCallBack)
    {
        base.HideMe(hideCallBack);
        //隐藏界面时删除模型
        Destroy(nowSkinObj);
    }

    public override void ShowMe()
    {
        base.ShowMe();
        //调用相机显示皮肤时的方法
    }

    //返回开始场景的协程
    //IEnumerator BackBeginSceneCoroutine()
    //{
    //    //开启场景异步加载
    //    AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("BeginScene");
    //    asyncOperation.allowSceneActivation = false;

    //    //返回按钮禁止点击
    //    btnBack.interactable = false;

    //    //等待加载
    //    while (asyncOperation.progress < 0.8f)
    //    {
    //        yield return null;
    //    }

    //    asyncOperation.allowSceneActivation = true;

    //    while (!asyncOperation.isDone)
    //    {
    //        yield return null;
    //    }

    //    UIManager.Instance.HidePanel<BuySkinPanel>();
    //    Camera.main.GetComponent<CameraAnimator>().TurnFarOrClose(() =>
    //    {
    //        UIManager.Instance.ShowPanel<ChooseHeroPanel>();
    //    }, false);
    //}

    protected override void Update()
    {
        base.Update();
        //鼠标拖动控制角色模型旋转
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), 100f, 1 << LayerMask.NameToLayer("Player")))
            {
                isClickHero = true;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isClickHero = false;
        }

        if (isClickHero)
        {
            float move = Input.GetAxis("Mouse X");
            nowRotateAngle -= move * 2000f * Time.deltaTime;
            nowSkinObj.transform.localRotation = Quaternion.Euler(0, nowRotateAngle, 0);
        }
    }
}
