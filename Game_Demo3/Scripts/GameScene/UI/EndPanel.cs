using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndPanel : BasePanel
{
    // 字幕文字
    public Text txtCD;
    // 初始Y值
    public float originalY;
    // 字幕移动速度
    private float moveSpeed = 70f;
    // UI坐标组件
    private RectTransform rect;
    // 是否播放结束
    private bool isPlayEnd;

    protected override void Init()
    {
        Cursor.lockState = CursorLockMode.Locked;
        BKMusic.Instance.ChangeBKMusic("EndMusic");
        rect = txtCD.GetComponent<RectTransform>();
    }

    protected override void Update()
    {
        base.Update();
        // 移动字幕
        rect.position = new Vector3(rect.position.x, rect.position.y + moveSpeed * Time.deltaTime, rect.position.z);
        // 判断位置
        if (rect.position.y > -originalY && !isPlayEnd)
        {
            isPlayEnd = true;
            Cursor.lockState = CursorLockMode.None;
            UIManager.Instance.HideAllPanel();
            LoadScenePanel loadScenePanel = UIManager.Instance.ShowPanel<LoadScenePanel>();
            loadScenePanel.InitInfo("BeginScene", "开始场景");
        }
    }
}
