using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowSkins : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(GameDataMgr.Instance.PlayerData.buySkin.Contains(i + 1));
        }
    }
}
