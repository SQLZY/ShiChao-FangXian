using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeginPanel : BasePanel
{
    //开始
    public Button btnStart;
    //设置
    public Button btnSetting;
    //关于
    public Button btnAbout;
    //退出
    public Button btnExit;

    protected override void Init()
    {
        //恢复自由鼠标状态
        Cursor.lockState = CursorLockMode.None;

        btnStart.onClick.AddListener(() =>
        {
            Camera.main.GetComponent<CameraAnimator>().TurnLeftOrRight(() =>
            {
                //动画播放结束后显示选角面板
                UIManager.Instance.ShowPanel<ChooseHeroPanel>();
            }, true);
            //隐藏自己
            UIManager.Instance.HidePanel<BeginPanel>();
        });
        btnSetting.onClick.AddListener(() =>
        {
            //切换界面
            UIManager.Instance.ShowPanel<SettingPanel>();
            UIManager.Instance.HidePanel<BeginPanel>();
        });
        btnAbout.onClick.AddListener(() =>
        {
            //百科界面
            UIManager.Instance.ShowPanel<GuidePanel>();
        });
        btnExit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}
