using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChooseScenePanel : BasePanel
{
    //场景提示信息与图片
    public Text txtInfo;
    public Image imgScene;
    //左右切换按钮
    public Button btnLeft;
    public Button btnRight;
    //开始与返回按钮
    public Button btnStart;
    public Button btnBack;
    //关卡等级信息
    public Text txtLevelInfo;
    //解锁状态信息
    public Text txtUnlockInfo;

    //当前场景信息/索引值/场景难度
    private SceneInfo nowSceneInfo;
    private int nowIndex;
    private int level;

    protected override void Init()
    {
        //根据玩家通关进度更新最新场景索引
        nowIndex = -1;
        foreach (int v in GameDataMgr.Instance.PlayerData.sceneLevelInfo)
        {
            if (v != 0) nowIndex++;
        }

        //初始化场景信息
        ChangeScene();

        //左按键
        btnLeft.onClick.AddListener(() =>
        {
            //更新索引
            --nowIndex;
            if (nowIndex < 0)
            {
                nowIndex = GameDataMgr.Instance.SceneList.Count - 1;
            }
            ChangeScene();
        });
        //右按键
        btnRight.onClick.AddListener(() =>
        {
            //更新索引
            ++nowIndex;
            if (nowIndex >= GameDataMgr.Instance.SceneList.Count)
            {
                nowIndex = 0;
            }
            ChangeScene();
        });
        //开始按键
        btnStart.onClick.AddListener(() =>
        {
            //记录当前选择场景的关卡难度
            GameDataMgr.Instance.nowSelSceneLevel = GameDataMgr.Instance.SceneLevelMonsterList[nowIndex * 10 + level - 1];
            //隐藏当前面板
            UIManager.Instance.HidePanel<ChooseScenePanel>();
            //切换场景
            LoadScenePanel loadScenePanel = UIManager.Instance.ShowPanel<LoadScenePanel>();
            loadScenePanel.InitInfo(nowSceneInfo);
        });
        //返回按键
        btnBack.onClick.AddListener(() =>
        {
            //切换面板
            UIManager.Instance.ShowPanel<ChooseModePanel>();
            UIManager.Instance.HidePanel<ChooseScenePanel>();
        });
    }

    /// <summary>
    /// 更新显示信息
    /// </summary>
    private void ChangeScene()
    {
        //切换场景更新信息
        nowSceneInfo = GameDataMgr.Instance.SceneList[nowIndex];
        txtInfo.text = "场景:\n" + nowSceneInfo.name + "\n\n" + nowSceneInfo.tips;
        imgScene.sprite = Resources.Load<Sprite>(nowSceneInfo.imgRes);
        //更新当前场景难度信息
        level = GameDataMgr.Instance.PlayerData.sceneLevelInfo[nowIndex];
        txtLevelInfo.text = level.ToString();
        //更新解锁提示信息
        if (level == 0)
        {
            txtUnlockInfo.text = $"当前场景未解锁\n请先通关<color=Red>{GameDataMgr.Instance.SceneList[nowIndex - 1].name}</color>";
            btnStart.gameObject.SetActive(false);
        }
        else
        {
            txtUnlockInfo.text = "";
            btnStart.gameObject.SetActive(true);
        }
    }
}
