using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISoundEvent : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    //鼠标进入音效开关
    public bool enterSound = true;
    //鼠标点击音效开关
    public bool clickSound = true;
    //音效组件
    private AudioSource UIAudioSourceObj;
    //音效文件
    private AudioClip UISoundClip1;
    private AudioClip UISoundClip2;

    //自身Button组件
    private Button button;

    //初始化音效相关
    private void Awake()
    {
        UIAudioSourceObj = GameDataMgr.Instance.UIAudioSourceObj;
        if (!UIAudioSourceObj)
        {
            GameObject UISoundGameObject = new GameObject("UIAudioSourceObj");
            UIAudioSourceObj = UISoundGameObject.AddComponent<AudioSource>();
            GameDataMgr.Instance.UIAudioSourceObj = UIAudioSourceObj;
            UIAudioSourceObj.mute = !GameDataMgr.Instance.MusicData.isOpenSound;
            // UI音效调小音量
            UIAudioSourceObj.volume = GameDataMgr.Instance.MusicData.soundVolume * 0.25f;
            DontDestroyOnLoad(UISoundGameObject);
        }

        UISoundClip1 = Resources.Load<AudioClip>("Music/MouseSound/Sound1");
        UISoundClip2 = Resources.Load<AudioClip>("Music/MouseSound/Sound2");

        // 关联自身Button(如果有)
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Button禁用 无点击音效
        if (button && !button.interactable) return;

        if (clickSound)
        {
            UIAudioSourceObj.PlayOneShot(UISoundClip1);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enterSound)
        {
            UIAudioSourceObj.PlayOneShot(UISoundClip2);
        }
    }
}
