using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundObj : MonoBehaviour
{
    private static PlaySoundObj instance;
    public static PlaySoundObj Instance => instance;
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void ReleasePlaySound()
    {
        GameDataMgr.Instance.ReleasePlaySound();
    }

    private void ReleasePlayEff()
    {
        GameDataMgr.Instance.ReleasePlayEff();
    }
}
