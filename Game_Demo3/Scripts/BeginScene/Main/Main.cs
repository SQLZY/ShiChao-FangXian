using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        // 独占全屏模式
        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;

        // 设置游戏分辨率为当前屏幕分辨率
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);

        // 设置游戏帧率上限
        Application.targetFrameRate = 240;

        // 初始场景自动显示开始面板
        UIManager.Instance.ShowPanel<BeginPanel>();
    }
}
