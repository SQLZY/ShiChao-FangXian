using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateTowersBK : MonoBehaviour
{
    private TowerItem[] towerItems;
    private int activeCount;
    private float singleWidth;

    // Start is called before the first frame update
    void Awake()
    {
        towerItems = GetComponentsInChildren<TowerItem>();
        singleWidth = GetComponent<GridLayoutGroup>().cellSize.x;
    }

    // Update is called once per frame
    void Update()
    {
        //计算活跃状态UI炮台Item数量
        activeCount = 0;
        foreach (TowerItem item in towerItems)
        {
            if (item.gameObject.activeSelf == true)
            {
                activeCount++;
            }
        }
        //自适应背景区域宽度
        (transform as RectTransform).sizeDelta = new Vector2(activeCount * singleWidth,
                                                            (transform as RectTransform).sizeDelta.y);
    }
}
