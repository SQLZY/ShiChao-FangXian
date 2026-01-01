using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_IconType
{
    Monster,
    Player,
    Tower,
    MainTower,
    MonsterPoint,
    TowerPoint
}

/// <summary>
/// 为对象创建小地图UI图标
/// </summary>
public class AddMapIcon : MonoBehaviour
{
    private Dictionary<E_IconType, string> iconColors = new Dictionary<E_IconType, string>()
    {
        {E_IconType.Monster,"Red"},
        {E_IconType.Player,"Blue"},
        {E_IconType.Tower,"Yellow"},
        {E_IconType.MainTower,"Green"},
        {E_IconType.MonsterPoint,"Purple"},
        {E_IconType.TowerPoint,"White"}
    };

    public E_IconType iconType;
    public float iconScale = 1f;
    public float layerHeight = 1f;

    //当前的图标信息物体
    private GameObject nowSpriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        if (iconType == E_IconType.Player)
        {
            AddIcon("Sprites/PlayerIcon");
        }
        else
        {
            AddIcon("Sprites/BasicIcon");
        }
    }

    /// <summary>
    /// 添加对应地图图标Icon
    /// </summary>
    /// <param name="path">图片资源路径</param>
    private void AddIcon(string path)
    {
        GameObject icon = new GameObject();
        icon.name = $"MapIcon_{iconType}";
        SpriteRenderer spriteRenderer = icon.AddComponent<SpriteRenderer>();
        icon.transform.SetParent(transform);
        icon.transform.position = this.transform.position + Vector3.up * 5 * layerHeight;
        icon.transform.rotation = Quaternion.LookRotation(Vector3.down);
        icon.transform.localScale = Vector3.one * iconScale;
        spriteRenderer.sprite = Resources.Load<Sprite>(path);
        spriteRenderer.material = Resources.Load<Material>($"Materials/Material_{iconColors[iconType]}");
        icon.layer = LayerMask.NameToLayer("UIMapIcon");

        //记录当前图标组件
        nowSpriteRenderer = icon;
    }

    /// <summary>
    /// 改变小地图图标材质颜色
    /// </summary>
    /// <param name="iconType">目标材质颜色</param>
    public void ChangeIcon(E_IconType iconType)
    {
        //记录新图标材质类型
        this.iconType = iconType;
        //删除旧的图标图片
        Destroy(nowSpriteRenderer);
        nowSpriteRenderer = null;
        //应用新图标
        AddIcon("Sprites/BasicIcon");
    }
}
