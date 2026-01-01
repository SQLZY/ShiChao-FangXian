using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChooseHeroPanel : BasePanel
{
    //金钱数量与英雄名
    public Text txtMoney;
    public Text txtHero;
    //解锁所需金钱数量
    public Text txtUnlockMoney;
    //左右切换按钮
    public Button btnLeft;
    public Button btnRight;
    //开始与返回按钮
    public Button btnStart;
    public Button btnBack;
    //解锁角色
    public Button btnUnlock;
    //解锁新角色皮肤
    public Button btnSkin;
    //解锁防御塔
    public Button btnTower;

    //实例化英雄位置
    private Transform heroPos;
    //是否点击中角色
    private bool isClickHero;
    //当前旋转角度
    private float nowRotateAngle;

    //当前选中的角色Obj 角色数据 索引值
    private GameObject nowHeroObj;
    private HeroInfo nowHeroInfo;
    private int nowIndex;

    protected override void Init()
    {
        heroPos = GameObject.Find("HeroPos").transform;

        //初始化界面信息
        txtMoney.text = GameDataMgr.Instance.PlayerData.money.ToString();
        if (GameDataMgr.Instance.nowSelHero != null) nowIndex = GameDataMgr.Instance.nowSelHero.id - 1;
        ChangeHero();

        //左按键
        btnLeft.onClick.AddListener(() =>
        {
            //更新索引
            --nowIndex;
            if (nowIndex < 0)
            {
                nowIndex = GameDataMgr.Instance.HeroList.Count - 1;
            }
            //更新模型界面数据
            ChangeHero();
        });
        //右按键
        btnRight.onClick.AddListener(() =>
        {
            //更新索引
            ++nowIndex;
            if (nowIndex >= GameDataMgr.Instance.HeroList.Count)
            {
                nowIndex = 0;
            }
            //更新模型界面数据
            ChangeHero();
        });
        //开始按键
        btnStart.onClick.AddListener(() =>
        {
            //隐藏自己
            UIManager.Instance.HidePanel<ChooseHeroPanel>();
            //显示模式选择面板
            Camera.main.GetComponent<CameraAnimator>().TurnUpOrDown(() =>
            {
                UIManager.Instance.ShowPanel<ChooseModePanel>();
            }, true);
        });
        //返回按键
        btnBack.onClick.AddListener(() =>
        {
            //切换场景
            Camera.main.GetComponent<CameraAnimator>().TurnLeftOrRight(() =>
            {
                UIManager.Instance.ShowPanel<BeginPanel>();
            }, false);
            //隐藏自己
            UIManager.Instance.HidePanel<ChooseHeroPanel>();
        });
        //解锁按键
        btnUnlock.onClick.AddListener(() =>
        {
            PlayerData playerData = GameDataMgr.Instance.PlayerData;
            if (playerData.money >= nowHeroInfo.lockMoney)
            {
                TipPanel tipPanel = UIManager.Instance.ShowPanel<TipPanel>();
                tipPanel.InitInfo("是否确认购买？", false);
                tipPanel.InitAction((v) =>
                {
                    if (v)
                    {
                        //购买成功逻辑
                        playerData.money -= nowHeroInfo.lockMoney;
                        playerData.buyHero.Add(nowHeroInfo.id);
                        GameDataMgr.Instance.SavePlayerData();
                        //更新解锁按钮 金钱显示
                        UpdateUnlockBtn();
                        txtMoney.text = playerData.money.ToString();
                        //更新购买成功提示
                        tipPanel.ClearAction();
                        tipPanel.InitInfo("购买成功");
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
                tipPanel.InitInfo("金钱不足");
            }
        });
        //解锁防御塔按键
        btnTower.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<ChooseHeroPanel>();
            UIManager.Instance.ShowPanel<UnlockTowerPanel>();
        });
        //解锁新角色皮肤按键
        btnSkin.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<ChooseHeroPanel>();

            //切换场景
            LoadScenePanel loadScenePanel = UIManager.Instance.ShowPanel<LoadScenePanel>();
            loadScenePanel.InitInfo("ShowSkinScene", "购买角色", () =>
            {
                UIManager.Instance.ShowPanel<BuySkinPanel>();
            });

            Camera.main.GetComponent<CameraAnimator>().TurnFarOrClose(null, true);
        });
    }

    /// <summary>
    /// 更新显示信息与模型
    /// </summary>
    private void ChangeHero()
    {
        //删除上个模型
        Destroy(nowHeroObj);
        //更新当前角色数据
        nowHeroInfo = GameDataMgr.Instance.HeroList[nowIndex];
        //实例化角色并记录更新数据
        GameObject heroObj;
        if (GameDataMgr.Instance.PlayerData.nowSelSkinInfo == null)
        {
            heroObj = Instantiate(Resources.Load<GameObject>(nowHeroInfo.res), heroPos.position, heroPos.rotation);
        }
        else
        {
            //实例化皮肤 设置动画状态机
            heroObj = Instantiate(Resources.Load<GameObject>(GameDataMgr.Instance.PlayerData.nowSelSkinInfo.res), heroPos.position, heroPos.rotation);
            heroObj.GetComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>($"Animator/Hero/1");
            //实例化武器 设置正确位置/旋转/缩放
            GameObject weapon = Instantiate(Resources.Load<GameObject>($"Hero/Gun/{nowHeroInfo.id}"));
            Transform[] transforms = heroObj.transform.GetComponentsInChildren<Transform>();
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
        }

        Destroy(heroObj.GetComponent<PlayerObj>());
        Destroy(heroObj.GetComponent<PlayerRotationController>());
        Destroy(heroObj.GetComponent<PlayerIKController>());
        nowHeroObj = heroObj;
        nowRotateAngle = heroObj.transform.localRotation.eulerAngles.y;
        txtHero.text = nowHeroInfo.tips;
        //更新解锁按钮
        UpdateUnlockBtn();

        //记录选择的角色
        GameDataMgr.Instance.nowSelHero = nowHeroInfo;
    }
    /// <summary>
    /// 更新解锁按钮
    /// </summary>
    private void UpdateUnlockBtn()
    {
        if (nowHeroInfo.lockMoney == 0 || GameDataMgr.Instance.PlayerData.buyHero.Contains(nowHeroInfo.id))
        {
            btnUnlock.gameObject.SetActive(false);
            btnStart.gameObject.SetActive(true);
        }
        else
        {
            btnUnlock.gameObject.SetActive(true);
            txtUnlockMoney.text = "＄" + nowHeroInfo.lockMoney;
            btnStart.gameObject.SetActive(false);
        }
    }

    public override void HideMe(UnityAction hideCallBack)
    {
        base.HideMe(hideCallBack);
        //删除场景上的角色
        DestroyImmediate(nowHeroObj);
    }

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
            nowRotateAngle -= move * 500f * Time.deltaTime;
            nowHeroObj.transform.localRotation = Quaternion.Euler(0, nowRotateAngle, 0);
        }
    }
}
