using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggWinGame : MonoBehaviour
{
    public bool winGameShow;

    private void Awake()
    {
        if (GameDataMgr.Instance.PlayerData.isWinAllGame) gameObject.SetActive(winGameShow);
        else gameObject.SetActive(!winGameShow);
    }
}
