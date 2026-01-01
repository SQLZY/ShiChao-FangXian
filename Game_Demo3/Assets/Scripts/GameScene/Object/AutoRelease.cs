using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRelease : MonoBehaviour
{
    // 对象池回收时间
    public float releaseTime = 1f;

    // Start is called before the first frame update
    void Start()
    {
        Invoke("ReleaseMe", releaseTime);
    }

    private void OnEnable()
    {
        Invoke("ReleaseMe", releaseTime);
    }

    private void ReleaseMe()
    {
        ObjectPoolMgr.Instance.ReleaseObj(gameObject);
    }
}
