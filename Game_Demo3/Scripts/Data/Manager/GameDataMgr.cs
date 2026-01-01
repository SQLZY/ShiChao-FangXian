using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏模式
/// </summary>
public enum GameMode
{
    BasicMode,
    TrainingMode,
    EndlessMode,
}

public class GameDataMgr
{
    private static GameDataMgr instance = new GameDataMgr();
    public static GameDataMgr Instance => instance;
    private GameDataMgr()
    {
        //初始化全部数据

        //玩家游戏产生的数据 使用基于设备的随机密钥
        musicData = JsonMgr.Instance.LoadDataWithAES<MusicData>("MusicData", EncryptionKeyManager.GetDeviceBasedKey());
        fightSettingsData = JsonMgr.Instance.LoadDataWithAES<FightSettingsData>("FightSettingsData", EncryptionKeyManager.GetDeviceBasedKey());
        playerData = JsonMgr.Instance.LoadDataWithAES<PlayerData>("PlayerData", EncryptionKeyManager.GetDeviceBasedKey());

        //配置文件相关数据 使用基于数据类名的随机密钥
        heroList = JsonMgr.Instance.LoadDataWithAES<List<HeroInfo>>("HeroInfo", EncryptionKeyManager.GetDefaultKey("HeroInfo"));
        skinList = JsonMgr.Instance.LoadDataWithAES<List<SkinInfo>>("SkinInfo", EncryptionKeyManager.GetDefaultKey("SkinInfo"));
        sceneList = JsonMgr.Instance.LoadDataWithAES<List<SceneInfo>>("SceneInfo", EncryptionKeyManager.GetDefaultKey("SceneInfo"));
        monsterList = JsonMgr.Instance.LoadDataWithAES<List<MonsterInfo>>("MonsterInfo", EncryptionKeyManager.GetDefaultKey("MonsterInfo"));
        sceneLevelMonsterList = JsonMgr.Instance.LoadDataWithAES<List<SceneLevelMonsterInfo>>("SceneLevelMonsterInfo", EncryptionKeyManager.GetDefaultKey("SceneLevelMonsterInfo"));
        towerList = JsonMgr.Instance.LoadDataWithAES<List<TowerInfo>>("TowerInfo", EncryptionKeyManager.GetDefaultKey("TowerInfo"));
        //控制信息相关数据 使用基于数据类名的随机密钥
        allControlInfo.gunControlInfo = JsonMgr.Instance.LoadDataWithAES<GunControlInfo>("GunControlInfo", EncryptionKeyManager.GetDefaultKey("GunControlInfo"));
        allControlInfo.skinAwardControlInfo = JsonMgr.Instance.LoadDataWithAES<SkinAwardControlInfo>("SkinAwardControlInfo", EncryptionKeyManager.GetDefaultKey("SkinAwardControlInfo"));
        allControlInfo.playerControlInfo = JsonMgr.Instance.LoadDataWithAES<PlayerControlInfo>("PlayerControlInfo", EncryptionKeyManager.GetDefaultKey("PlayerControlInfo"));
        allControlInfo.monsterControlInfo = JsonMgr.Instance.LoadDataWithAES<MonsterControlInfo>("MonsterControlInfo", EncryptionKeyManager.GetDefaultKey("MonsterControlInfo"));

        // 初始化音效/特效组件队列
        audioSources = new Queue<GameObject>();
        effGameObjects = new Queue<GameObject>();
    }

    #region 场景数据相关
    private List<SceneInfo> sceneList;
    public List<SceneInfo> SceneList => sceneList;
    #endregion

    #region 音乐音效相关
    private MusicData musicData;
    public MusicData MusicData => musicData;
    //音效组件队列
    private Queue<GameObject> audioSources;
    //特效组件队列
    private Queue<GameObject> effGameObjects;
    //场景UI音效播放源
    public AudioSource UIAudioSourceObj;

    /// <summary>
    /// 存储音效数据
    /// </summary>
    public void SavaMusicData()
    {
        JsonMgr.Instance.SaveDataWithAES("MusicData", musicData, EncryptionKeyManager.GetDeviceBasedKey());
    }

    /// <summary>
    /// 重置音效设置数据
    /// </summary>
    public void ResetMusicData()
    {
        musicData = new MusicData();
        SavaMusicData();
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="path">音效路径</param>
    public void PlaySound(string path, float volumeRatio = 1f, Transform soundPos = null)
    {
        //如果对象池不存在 注册对象池
        if (!ObjectPoolMgr.Instance.HasPool("PlaySound"))
        {
            GameObject playSound = Resources.Load<GameObject>("Music/PlaySound");
            ObjectPoolMgr.Instance.RegisterPool("PlaySound", playSound);
        }
        if (!ObjectPoolMgr.Instance.HasPool("PlaySound3D"))
        {
            GameObject playSound = Resources.Load<GameObject>("Music/PlaySound3D");
            ObjectPoolMgr.Instance.RegisterPool("PlaySound3D", playSound);
        }

        AudioSource audioSource;
        //对象池中获取播放音效组件
        if (soundPos == null)
        {
            // 2D音效
            audioSource = ObjectPoolMgr.Instance.Get("PlaySound", Vector3.zero, Quaternion.identity).GetComponent<AudioSource>();
        }
        else
        {
            // 3D音效
            audioSource = ObjectPoolMgr.Instance.Get("PlaySound3D", soundPos.position, Quaternion.identity, soundPos).GetComponent<AudioSource>();
        }
        //暂停音效
        audioSource.Stop();
        //获取对应路径音效文件 设置音量大小
        audioSource.clip = Resources.Load<AudioClip>(path);
        audioSource.loop = false;
        audioSource.volume = musicData.soundVolume * volumeRatio;
        audioSource.mute = !musicData.isOpenSound;
        //播放音效
        audioSource.Play();
        //队列入栈
        audioSources.Enqueue(audioSource.gameObject);
        //延时函数执行回收方法
        PlaySoundObj.Instance.Invoke("ReleasePlaySound", 1f);
    }

    //对象池回收音效组件
    public void ReleasePlaySound()
    {
        GameObject obj = audioSources.Dequeue();
        if (obj) ObjectPoolMgr.Instance.ReleaseObj(obj);
    }

    /// <summary>
    /// 播放特效
    /// </summary>
    /// <param name="path">特效路径</param>
    /// <param name="pos">位置</param>
    /// <param name="quaternion">旋转角度</param>
    public void PlayEff(string path, Vector3 pos, Quaternion quat, Transform parent = null)
    {
        GameObject effObj = Resources.Load<GameObject>(path);
        if (!ObjectPoolMgr.Instance.HasPool(effObj.name))
        {
            if (!effObj.GetComponent<EffResetObj>())
            {
                effObj.AddComponent<EffResetObj>();
            }
            ObjectPoolMgr.Instance.RegisterPool(effObj.name, effObj, true);
        }
        GameObject obj = ObjectPoolMgr.Instance.Get(effObj.name, pos, quat, parent);
        obj.GetComponent<ParticleSystem>().Play();
        //特效对象入栈
        effGameObjects.Enqueue(obj);
        //延时函数执行回收方法
        PlaySoundObj.Instance.Invoke("ReleasePlayEff", 0.8f);
    }

    //对象池回收特效组件
    public void ReleasePlayEff()
    {
        GameObject obj = effGameObjects.Dequeue();
        if (obj != null)
        {
            ObjectPoolMgr.Instance.ReleaseObj(obj);
        }
    }
    #endregion

    #region 战斗设置相关
    private FightSettingsData fightSettingsData;
    public FightSettingsData FightSettingsData => fightSettingsData;

    /// <summary>
    /// 存储战斗设置
    /// </summary>
    public void SaveFightSettingsData()
    {
        JsonMgr.Instance.SaveDataWithAES("FightSettingsData", fightSettingsData, EncryptionKeyManager.GetDeviceBasedKey());
    }

    /// <summary>
    /// 重置战斗设置数据
    /// </summary>
    public void ResetFightSettingsData()
    {
        fightSettingsData = new FightSettingsData();
        SaveFightSettingsData();
    }
    #endregion

    #region 角色数据相关
    private List<HeroInfo> heroList;
    public List<HeroInfo> HeroList => heroList;
    //当前选择的角色数据
    public HeroInfo nowSelHero;
    //当前场景上的玩家
    public PlayerObj nowPlayerObj;
    //当前是否处于游戏进行状态
    public bool isGaming;
    //当前游戏模式
    public GameMode nowGameMode;

    /// <summary>
    /// 切换当前选择的游戏模式
    /// </summary>
    /// <param name="gameMode">游戏模式</param>
    public void ChangeNowGameMode(GameMode gameMode)
    {
        nowGameMode = gameMode;
    }
    #endregion

    #region 皮肤数据相关
    private List<SkinInfo> skinList;
    public List<SkinInfo> SkinList => skinList;
    #endregion

    #region 怪物数据相关
    private List<MonsterInfo> monsterList;
    public List<MonsterInfo> MonsterList => monsterList;
    #endregion

    #region 玩家数据相关
    private PlayerData playerData;
    public PlayerData PlayerData => playerData;

    /// <summary>
    /// 存储玩家数据
    /// </summary>
    public void SavePlayerData()
    {
        JsonMgr.Instance.SaveDataWithAES("PlayerData", playerData, EncryptionKeyManager.GetDeviceBasedKey());
    }
    /// <summary>
    /// 重置玩家存档数据
    /// </summary>
    public void ResetPlayerData()
    {
        playerData = new PlayerData();
        SavePlayerData();
    }
    #endregion

    #region 控制信息相关
    private AllControlInfo allControlInfo = new AllControlInfo();
    public AllControlInfo AllControlInfo => allControlInfo;
    #endregion

    #region 场景出怪相关
    private List<SceneLevelMonsterInfo> sceneLevelMonsterList;
    public List<SceneLevelMonsterInfo> SceneLevelMonsterList => sceneLevelMonsterList;
    //当前关卡出怪信息
    public SceneLevelMonsterInfo nowSelSceneLevel;
    #endregion

    #region 防御塔数据相关
    private List<TowerInfo> towerList;
    public List<TowerInfo> TowerList => towerList;
    #endregion
}
