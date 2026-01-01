using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BKMusic : MonoBehaviour
{
    private static BKMusic instance;
    public static BKMusic Instance => instance;
    //音乐组件
    private AudioSource audioSource;
    //是否正在播放基础背景音乐
    private bool isBeginMusic = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            audioSource = GetComponent<AudioSource>();
            UpdateBKMusic();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //更新背景音乐数据
    public void UpdateBKMusic()
    {
        audioSource.mute = !GameDataMgr.Instance.MusicData.isOpenMusic;
        audioSource.volume = GameDataMgr.Instance.MusicData.musicVolume * 0.7f;
    }

    //更换背景音乐
    public void ChangeBKMusic(string musicName)
    {
        audioSource.clip = Resources.Load<AudioClip>($"Music/BKMusic/{musicName}");
        audioSource.Play();
        isBeginMusic = false;
    }

    //切换基础背景音乐
    public void ChangeBeginMusic()
    {
        if (!isBeginMusic)
        {
            audioSource.clip = Resources.Load<AudioClip>($"Music/BKMusic/BeginMusic");
            audioSource.Play();
            isBeginMusic = true;
        }
    }
}
