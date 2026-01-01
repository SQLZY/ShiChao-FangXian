using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseModePanel : BasePanel
{
    // 返回与模式按钮
    public Button btnBack;
    public Button btnMode1;
    public Button btnMode2;
    public Button btnMode3;
    // 提示文字
    public Text txtMode2;
    public Text txtMode3;

    protected override void Init()
    {
        PlayerData playerData = GameDataMgr.Instance.PlayerData;
        //初始化模式2相关
        if (playerData.sceneLevelInfo[1] > 0)
        {
            btnMode2.interactable = true;
            txtMode2.gameObject.SetActive(false);
        }
        else
        {
            btnMode2.interactable = false;
            txtMode2.gameObject.SetActive(true);
        }
        //初始化模式3相关
        if (playerData.isWinAllGame)
        {
            btnMode3.interactable = true;
            txtMode3.text = $"历史最大坚守波数:{GameDataMgr.Instance.PlayerData.endlessModeMaxWave}";
        }
        else
        {
            btnMode3.interactable = false;
        }

        //返回按钮
        btnBack.onClick.AddListener(() =>
        {
            //切换场景
            Camera.main.GetComponent<CameraAnimator>().TurnUpOrDown(() =>
            {
                UIManager.Instance.ShowPanel<ChooseHeroPanel>();
            }, false);
            UIManager.Instance.HidePanel<ChooseModePanel>();
        });
        //模式1
        btnMode1.onClick.AddListener(() =>
        {
            //切换模式
            GameDataMgr.Instance.ChangeNowGameMode(GameMode.BasicMode);
            //隐藏自己
            UIManager.Instance.HidePanel<ChooseModePanel>();
            //显示场景选择面板
            UIManager.Instance.ShowPanel<ChooseScenePanel>();
        });
        //模式2
        btnMode2.onClick.AddListener(() =>
        {
            //切换模式
            GameDataMgr.Instance.ChangeNowGameMode(GameMode.TrainingMode);
            //隐藏面板
            UIManager.Instance.HideAllPanel();
            //切换场景
            LoadScenePanel loadScenePanel = UIManager.Instance.ShowPanel<LoadScenePanel>();
            loadScenePanel.InitInfo("TrainingScene", "憩战家园", null, "GameScene05", "ImgScene/TrainingScene");
        });
        //模式3
        btnMode3.onClick.AddListener(() =>
        {
            //切换模式
            GameDataMgr.Instance.ChangeNowGameMode(GameMode.EndlessMode);
            //隐藏面板
            UIManager.Instance.HideAllPanel();
            //切换场景
            LoadScenePanel loadScenePanel = UIManager.Instance.ShowPanel<LoadScenePanel>();
            loadScenePanel.InitInfo("EndlessScene", "永恒村庄", null, "GameScene05", "ImgScene/EndlessScene");
        });
    }
}
