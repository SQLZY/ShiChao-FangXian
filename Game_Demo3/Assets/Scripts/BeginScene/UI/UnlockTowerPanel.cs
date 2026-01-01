using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnlockTowerPanel : BasePanel
{
    public List<Button> unlockBtns;
    public Dictionary<int, int> levelMoneyMapDic;
    public Button btnClose;

    protected override void Init()
    {
        //等级对应解锁金钱数据
        levelMoneyMapDic = new Dictionary<int, int>()
        {
            { 1, 0 },
            { 2, 1000 },
            { 3, 6000 },
            { 4, 24000 },
        };

        //为每个按钮添加监听事件
        for (int i = 0; i < unlockBtns.Count; ++i)
        {
            //当前等级解锁所属金钱
            int unlockMoney = levelMoneyMapDic[i + 1];
            //添加事件监听
            unlockBtns[i].onClick.AddListener(() =>
            {
                PlayerData playerData = GameDataMgr.Instance.PlayerData;
                if (playerData.money >= unlockMoney)
                {
                    TipPanel tipPanel = UIManager.Instance.ShowPanel<TipPanel>();
                    tipPanel.InitInfo($"是否确认解锁？\n拥有金钱<color=yellow>${playerData.money}</color>\n需要金钱${unlockMoney}", false);
                    tipPanel.InitAction((v) =>
                    {
                        if (v)
                        {
                            //解锁成功逻辑
                            playerData.money -= unlockMoney;
                            playerData.maxTowerLevel++;
                            GameDataMgr.Instance.SavePlayerData();
                            //更新解锁按钮
                            CheckAndUpdateUnlockInfo();
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
                    tipPanel.InitInfo($"金钱不足\n拥有金钱<color=yellow>${playerData.money}</color>\n需要金钱${unlockMoney}");
                }
            });
        }

        //返回按钮添加事件监听
        btnClose.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<UnlockTowerPanel>();
            UIManager.Instance.ShowPanel<ChooseHeroPanel>();
        });

        //初始化按钮信息
        CheckAndUpdateUnlockInfo();
    }

    private void CheckAndUpdateUnlockInfo()
    {
        int nowMaxLevel = GameDataMgr.Instance.PlayerData.maxTowerLevel;
        for (int i = 0; i < unlockBtns.Count; ++i)
        {
            if (i < nowMaxLevel)
            {
                unlockBtns[i].gameObject.SetActive(true);
                unlockBtns[i].interactable = false;
                unlockBtns[i].GetComponentInChildren<Text>().text = "已解锁";
                unlockBtns[i].GetComponentInChildren<Text>().color = Color.red;
            }
            else if (i == nowMaxLevel)
            {
                unlockBtns[i].gameObject.SetActive(true);
                unlockBtns[i].interactable = true;
                unlockBtns[i].GetComponentInChildren<Text>().text = "解锁";
                unlockBtns[i].GetComponentInChildren<Text>().color = Color.white;
            }
            else
            {
                unlockBtns[i].gameObject.SetActive(false);
            }
        }
    }
}
