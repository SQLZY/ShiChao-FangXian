using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    //音乐音效开关
    public Toggle togMusic;
    public Toggle togSound;
    //音乐音效大小
    public Slider sliderMusic;
    public Slider sliderSound;
    //关闭界面按钮
    public Button btnClose;

    //战斗设置相关
    //四种准星颜色
    public Toggle[] togAimColors;
    //准星中心点开关
    public Toggle togAimCentreOpen;
    public Toggle togAimCentreClose;
    //动态准星开关
    public Toggle togAimDynamicOpen;
    public Toggle togAimDynamicClose;

    //四个状态操控方式
    public Toggle togRunStayMode;
    public Toggle togRunClickMode;
    public Toggle togCrouchStayMode;
    public Toggle togCrouchClickMode;
    public Toggle togAimStayMode;
    public Toggle togAimClickMode;
    public Toggle togFreeStayMode;
    public Toggle togFreeClickMode;

    //鼠标两个灵敏度和数值显示文本
    public Slider sliderNormalSen;
    public Slider sliderFreeSen;
    public Text txtNormalValue;
    public Text txtFreeValue;

    //两个重置按钮
    public Button btnResetVoice;
    public Button btnResetFight;

    protected override void Init()
    {
        //音乐音效开关事件监听
        togMusic.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.MusicData.isOpenMusic = v;
            BKMusic.Instance.UpdateBKMusic();
        });
        togSound.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.MusicData.isOpenSound = v;
            GameDataMgr.Instance.UIAudioSourceObj.mute = !v;
        });
        //音乐音效大小事件监听
        sliderMusic.onValueChanged.AddListener(v =>
        {
            GameDataMgr.Instance.MusicData.musicVolume = v;
            BKMusic.Instance.UpdateBKMusic();
        });
        sliderSound.onValueChanged.AddListener(v =>
        {
            GameDataMgr.Instance.MusicData.soundVolume = v;
            // UI音效调小音量
            GameDataMgr.Instance.UIAudioSourceObj.volume = v * 0.25f;
        });
        //关闭按钮事件监听
        btnClose.onClick.AddListener(() =>
        {
            //存储改变的音乐数据
            GameDataMgr.Instance.SavaMusicData();
            //存储改变的战斗设置
            GameDataMgr.Instance.SaveFightSettingsData();
            //切换界面
            UIManager.Instance.ShowPanel<BeginPanel>();
            UIManager.Instance.HidePanel<SettingPanel>();
        });

        //四种准星颜色
        for (int i = 0; i < togAimColors.Length; i++)
        {
            int nowIndex = i;
            togAimColors[i].onValueChanged.AddListener((v) =>
            {
                if (v) GameDataMgr.Instance.FightSettingsData.aimStarColor = nowIndex + 1;
            });
        }
        //准星中心点开关
        togAimCentreOpen.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.FightSettingsData.aimStarCentre = v;
        });
        //动态准星开关
        togAimDynamicOpen.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.FightSettingsData.aimStarDynamic = v;
        });

        //四个操控模式
        togRunStayMode.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.FightSettingsData.runControlMode = !v;
        });
        togCrouchStayMode.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.FightSettingsData.crouchControlMode = !v;
        });
        togAimStayMode.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.FightSettingsData.aimControlMode = !v;
        });
        togFreeStayMode.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.FightSettingsData.freeControlMode = !v;
        });

        //两个灵敏度滚动条
        sliderNormalSen.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.FightSettingsData.normalSen = v;
            txtNormalValue.text = (10 * v).ToString("F1");
        });
        sliderFreeSen.onValueChanged.AddListener((v) =>
        {
            GameDataMgr.Instance.FightSettingsData.freeSen = v;
            txtFreeValue.text = (10 * v).ToString("F1");
        });

        //两个重置按钮
        btnResetVoice.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.ResetMusicData();
            UpdateVoiceInfo();
        });
        btnResetFight.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.ResetFightSettingsData();
            UpdateFightInfo();
        });
    }

    public override void ShowMe()
    {
        base.ShowMe();
        //根据存储的音乐数据 初始化界面显示
        UpdateVoiceInfo();
        //根据存储的战斗设置 初始化界面显示
        UpdateFightInfo();
    }

    //根据存储的音乐数据 更新界面显示
    private void UpdateVoiceInfo()
    {
        MusicData musicData = GameDataMgr.Instance.MusicData;
        togMusic.isOn = musicData.isOpenMusic;
        togSound.isOn = musicData.isOpenSound;
        sliderMusic.value = musicData.musicVolume;
        sliderSound.value = musicData.soundVolume;
    }

    //根据存储的战斗设置 更新界面显示
    private void UpdateFightInfo()
    {
        FightSettingsData fightSettingsData = GameDataMgr.Instance.FightSettingsData;
        togAimColors[fightSettingsData.aimStarColor - 1].isOn = true;
        togAimCentreOpen.isOn = fightSettingsData.aimStarCentre;
        togAimCentreClose.isOn = !fightSettingsData.aimStarCentre;
        togAimDynamicOpen.isOn = fightSettingsData.aimStarDynamic;
        togAimDynamicClose.isOn = !fightSettingsData.aimStarDynamic;

        togRunStayMode.isOn = !fightSettingsData.runControlMode;
        togRunClickMode.isOn = fightSettingsData.runControlMode;
        togCrouchStayMode.isOn = !fightSettingsData.crouchControlMode;
        togCrouchClickMode.isOn = fightSettingsData.crouchControlMode;
        togAimStayMode.isOn = !fightSettingsData.aimControlMode;
        togAimClickMode.isOn = fightSettingsData.aimControlMode;
        togFreeStayMode.isOn = !fightSettingsData.freeControlMode;
        togFreeClickMode.isOn = fightSettingsData.freeControlMode;

        sliderNormalSen.value = fightSettingsData.normalSen;
        sliderFreeSen.value = fightSettingsData.freeSen;
        txtNormalValue.text = (fightSettingsData.normalSen * 10f).ToString("F1");
        txtFreeValue.text = (fightSettingsData.freeSen * 10f).ToString("F1");
    }
}
