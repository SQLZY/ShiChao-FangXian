using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerState : MonoBehaviour
{
    //玩家是否进入
    public bool IsEnter { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == GameDataMgr.Instance.nowPlayerObj.gameObject)
        {
            IsEnter = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == GameDataMgr.Instance.nowPlayerObj.gameObject)
        {
            IsEnter = false;
        }
    }
}
