using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapPanel : BasePanel
{
    public RawImage mapImg;
    public Text txtMonsterNum;

    protected override void Init()
    {
        mapImg.texture = Resources.Load<RenderTexture>("UI/MapImg/BigMap");
    }

    protected override void Update()
    {
        base.Update();
        //非游戏状态 结束逻辑
        if (!GameDataMgr.Instance.isGaming) return;

        //按键关闭地图
        if (Input.GetKeyDown(KeyCode.M))
        {
            UIManager.Instance.HidePanel<MapPanel>();
            //切换渲染小地图
            MapIconCamera.Instance.ChangeCameraState(false);
            UIManager.Instance.GetPanel<GamePanel>().mapBk.gameObject.SetActive(true);
        }
        //更新怪物数量信息
        txtMonsterNum.text = SceneLevelMgr.Instance.nowMonsterNum.ToString();
    }
}
