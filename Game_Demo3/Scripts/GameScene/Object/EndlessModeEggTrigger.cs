using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessModeEggTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == GameDataMgr.Instance.nowPlayerObj.gameObject)
        {
            (SceneLevelMgr.Instance as EndlessModeSceneMgr).EndlessModeEggTrigger();
            Destroy(gameObject);
        }
    }
}
