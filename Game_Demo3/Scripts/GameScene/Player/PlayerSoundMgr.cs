using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家移动状态
/// </summary>
public enum E_MoveState
{
    Run,
    Jump,
    Land,
    Roll
}

public class PlayerSoundMgr : MonoBehaviour
{
    //单例模式
    private static PlayerSoundMgr instance;
    public static PlayerSoundMgr Instance => instance;
    private void Awake()
    {
        instance = this;
    }

    //音频文件字典
    private Dictionary<E_MoveState, AudioClip> audioClipDic;
    //当前状态
    private E_MoveState nowMoveState;
    //播放音频组件
    private AudioSource audioSource;

    //跑步音频播放速度
    private float pitch;
    //切换音频标识
    private bool changeClip;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        //设置音量大小与是否静音
        audioSource.volume = GameDataMgr.Instance.MusicData.soundVolume;
        audioSource.mute = !GameDataMgr.Instance.MusicData.isOpenSound;
        //记录对应音频文件
        audioClipDic = new Dictionary<E_MoveState, AudioClip>()
        {
            { E_MoveState.Run, Resources.Load<AudioClip>("Music/PlayerSound/Run") },
            { E_MoveState.Roll, Resources.Load<AudioClip>("Music/PlayerSound/Roll") },
            { E_MoveState.Jump, Resources.Load<AudioClip>("Music/PlayerSound/Jump") },
            { E_MoveState.Land, Resources.Load<AudioClip>("Music/PlayerSound/Land") },
        };
    }

    private void Update()
    {
        //非游戏状态 直接停止播放 结束逻辑
        if (!GameDataMgr.Instance.isGaming)
        {
            audioSource.Stop();
            return;
        }

        //改变音频文件
        if (changeClip)
        {
            changeClip = false;
            audioSource.clip = audioClipDic[nowMoveState];
            audioSource.pitch = 1;
            audioSource.Play();
        }

        //自动进入跑步状态
        if (!audioSource.isPlaying && !GameDataMgr.Instance.nowPlayerObj.isJumping)
        {
            nowMoveState = E_MoveState.Run;
            audioSource.clip = audioClipDic[E_MoveState.Run];
            audioSource.pitch = pitch;
            audioSource.Play();
        }
        //更新跑步音频速度
        if (nowMoveState == E_MoveState.Run)
        {
            audioSource.pitch = pitch;
        }
    }

    //更新音频速度
    public void UpdatePicth(float pitch)
    {
        this.pitch = pitch;
    }

    //改变音频切片状态
    public void ChangeState(E_MoveState moveState)
    {
        nowMoveState = moveState;
        changeClip = true;
    }
}
