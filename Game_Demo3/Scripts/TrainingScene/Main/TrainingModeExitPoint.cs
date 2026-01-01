using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainingModeExitPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //更新游戏进行状态
            GameDataMgr.Instance.isGaming = false;
            //清空玩家信息记录
            GameDataMgr.Instance.nowPlayerObj = null;
            //隐藏面板
            UIManager.Instance.HideAllPanel();
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
        }
    }
}
