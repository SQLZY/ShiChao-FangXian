using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class MapIconCamera : MonoBehaviour
{
    //单例模式
    private static MapIconCamera instance;
    public static MapIconCamera Instance => instance;
    private void Awake()
    {
        instance = this;
    }

    //关联自身相机脚本
    private Camera iconCamera;
    //相机高度
    private float CameraHeight = 150f;
    //小地图视野大小
    public float smallMapSize = 40f;
    //大地图视野大小
    public float bigMapSize = 80f;
    //玩家位置
    private Transform playerTarget;
    //保护区位置
    private Transform mainTowerTarget;
    //大地图中心位置重置(不使用保护区作为中央)
    public Transform centreBigMapPoint;

    // Start is called before the first frame update
    void Start()
    {
        //命名Map相机
        this.gameObject.name = "UIMapCamera";
        //移除AudioListener脚本
        Destroy(this.GetComponent<AudioListener>());
        //关联相机
        iconCamera = GetComponent<Camera>();
        //关联玩家
        playerTarget = GameDataMgr.Instance.nowPlayerObj.transform;
        //关联保护区(或重置的大地图中心点)
        if (centreBigMapPoint != null) mainTowerTarget = centreBigMapPoint;
        else mainTowerTarget = MainTowerObj.Instance.transform;
        //正交模式
        iconCamera.orthographic = true;
        //视野大小
        iconCamera.orthographicSize = smallMapSize;
        //地图层级
        iconCamera.cullingMask = (1 << LayerMask.NameToLayer("Environment") | 1 << LayerMask.NameToLayer("UIMapIcon"));
        //设置初始位置
        iconCamera.transform.SetParent(playerTarget, false);
        iconCamera.transform.position = playerTarget.position + Vector3.up * CameraHeight;
        iconCamera.transform.rotation = playerTarget.rotation * Quaternion.AngleAxis(90, Vector3.right);
        //设置地图图片
        iconCamera.targetTexture = Resources.Load<RenderTexture>("UI/MapImg/GameSceneMap");
        //设置相机深度
        iconCamera.depth = -5;
    }

    /// <summary>
    /// 改变相机渲染大小地图
    /// </summary>
    /// <param name="isBigMap">是否渲染大地图</param>
    public void ChangeCameraState(bool isBigMap)
    {
        if (isBigMap)
        {
            //设置正确位置
            iconCamera.transform.SetParent(mainTowerTarget, false);
            iconCamera.transform.position = mainTowerTarget.position + Vector3.up * CameraHeight;
            iconCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            //视野大小
            iconCamera.orthographicSize = bigMapSize;
            //设置地图图片
            iconCamera.targetTexture = Resources.Load<RenderTexture>("UI/MapImg/BigMap");
        }
        else
        {
            //设置正确位置
            iconCamera.transform.SetParent(playerTarget, false);
            iconCamera.transform.position = playerTarget.position + Vector3.up * CameraHeight;
            iconCamera.transform.rotation = playerTarget.rotation * Quaternion.AngleAxis(90, Vector3.right);
            //视野大小
            iconCamera.orthographicSize = smallMapSize;
            //设置地图图片
            iconCamera.targetTexture = Resources.Load<RenderTexture>("UI/MapImg/GameSceneMap");
        }
    }
}
