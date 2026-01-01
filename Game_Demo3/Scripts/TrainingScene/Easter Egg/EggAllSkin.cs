using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggAllSkin : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(GameDataMgr.Instance.PlayerData.buySkin.Count == GameDataMgr.Instance.SkinList.Count);
    }
}
