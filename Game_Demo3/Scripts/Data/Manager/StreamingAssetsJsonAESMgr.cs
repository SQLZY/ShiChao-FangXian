using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StreamingAssetsJsonAESMgr : MonoBehaviour
{
    public bool AESHeroList;
    public bool AESSkinList;
    public bool AESSceneList;
    public bool AESMonsterList;
    public bool AESSceneLevelMonsterList;
    public bool AESTowerList;
    public bool AESGunControlInfo;
    public bool AESSkinAwardControlInfo;
    public bool AESPlayerControlInfo;
    public bool AESMonsterControlInfo;

    //加密全部流文件夹Json文件
    [ContextMenu("执行方法AESAllStreamingAssetsJson")]
    public void AESAllStreamingAssetsJson()
    {
        ////配置文件相关数据 使用基于数据类名的随机密钥
        //List<HeroInfo> heroList = JsonMgr.Instance.LoadData<List<HeroInfo>>("HeroInfo");
        //List<SkinInfo> skinList = JsonMgr.Instance.LoadData<List<SkinInfo>>("SkinInfo");
        //List<SceneInfo> sceneList = JsonMgr.Instance.LoadData<List<SceneInfo>>("SceneInfo");
        //List<MonsterInfo> monsterList = JsonMgr.Instance.LoadData<List<MonsterInfo>>("MonsterInfo");
        //List<SceneLevelMonsterInfo> sceneLevelMonsterList = JsonMgr.Instance.LoadData<List<SceneLevelMonsterInfo>>("SceneLevelMonsterInfo");
        //List<TowerInfo> towerList = JsonMgr.Instance.LoadData<List<TowerInfo>>("TowerInfo");
        ////控制信息相关数据 使用基于数据类名的随机密钥
        //AllControlInfo allControlInfo = new AllControlInfo();
        //allControlInfo.gunControlInfo = JsonMgr.Instance.LoadData<GunControlInfo>("GunControlInfo");
        //allControlInfo.skinAwardControlInfo = JsonMgr.Instance.LoadData<SkinAwardControlInfo>("SkinAwardControlInfo");
        //allControlInfo.playerControlInfo = JsonMgr.Instance.LoadData<PlayerControlInfo>("PlayerControlInfo");
        //allControlInfo.monsterControlInfo = JsonMgr.Instance.LoadData<MonsterControlInfo>("MonsterControlInfo");

        ////加密存储覆盖全部数据
        //SaveDataWithAESInStreamingAssets("HeroInfo", heroList, EncryptionKeyManager.GetDefaultKey("HeroInfo"));
        //SaveDataWithAESInStreamingAssets("SkinInfo", skinList, EncryptionKeyManager.GetDefaultKey("SkinInfo"));
        //SaveDataWithAESInStreamingAssets("SceneInfo", sceneList, EncryptionKeyManager.GetDefaultKey("SceneInfo"));
        //SaveDataWithAESInStreamingAssets("MonsterInfo", monsterList, EncryptionKeyManager.GetDefaultKey("MonsterInfo"));
        //SaveDataWithAESInStreamingAssets("SceneLevelMonsterInfo", sceneLevelMonsterList, EncryptionKeyManager.GetDefaultKey("SceneLevelMonsterInfo"));
        //SaveDataWithAESInStreamingAssets("TowerInfo", towerList, EncryptionKeyManager.GetDefaultKey("TowerInfo"));
        //SaveDataWithAESInStreamingAssets("GunControlInfo", allControlInfo.gunControlInfo, EncryptionKeyManager.GetDefaultKey("GunControlInfo"));
        //SaveDataWithAESInStreamingAssets("SkinAwardControlInfo", allControlInfo.skinAwardControlInfo, EncryptionKeyManager.GetDefaultKey("SkinAwardControlInfo"));
        //SaveDataWithAESInStreamingAssets("PlayerControlInfo", allControlInfo.playerControlInfo, EncryptionKeyManager.GetDefaultKey("PlayerControlInfo"));
        //SaveDataWithAESInStreamingAssets("MonsterControlInfo", allControlInfo.monsterControlInfo, EncryptionKeyManager.GetDefaultKey("MonsterControlInfo"));





        if (AESHeroList)
        {
            List<HeroInfo> heroList = JsonMgr.Instance.LoadData<List<HeroInfo>>("HeroInfo");
            SaveDataWithAESInStreamingAssets("HeroInfo", heroList, EncryptionKeyManager.GetDefaultKey("HeroInfo"));

        }
        if (AESSkinList)
        {
            List<SkinInfo> skinList = JsonMgr.Instance.LoadData<List<SkinInfo>>("SkinInfo");
            SaveDataWithAESInStreamingAssets("SkinInfo", skinList, EncryptionKeyManager.GetDefaultKey("SkinInfo"));

        }
        if (AESSceneList)
        {
            List<SceneInfo> sceneList = JsonMgr.Instance.LoadData<List<SceneInfo>>("SceneInfo");
            SaveDataWithAESInStreamingAssets("SceneInfo", sceneList, EncryptionKeyManager.GetDefaultKey("SceneInfo"));
        }
        if (AESMonsterList)
        {
            List<MonsterInfo> monsterList = JsonMgr.Instance.LoadData<List<MonsterInfo>>("MonsterInfo");
            SaveDataWithAESInStreamingAssets("MonsterInfo", monsterList, EncryptionKeyManager.GetDefaultKey("MonsterInfo"));
        }
        if (AESSceneLevelMonsterList)
        {
            List<SceneLevelMonsterInfo> sceneLevelMonsterList = JsonMgr.Instance.LoadData<List<SceneLevelMonsterInfo>>("SceneLevelMonsterInfo");
            SaveDataWithAESInStreamingAssets("SceneLevelMonsterInfo", sceneLevelMonsterList, EncryptionKeyManager.GetDefaultKey("SceneLevelMonsterInfo"));
        }
        if (AESTowerList)
        {
            List<TowerInfo> towerList = JsonMgr.Instance.LoadData<List<TowerInfo>>("TowerInfo");
            SaveDataWithAESInStreamingAssets("TowerInfo", towerList, EncryptionKeyManager.GetDefaultKey("TowerInfo"));

        }

        AllControlInfo allControlInfo = new AllControlInfo();
        if (AESGunControlInfo)
        {
            allControlInfo.gunControlInfo = JsonMgr.Instance.LoadData<GunControlInfo>("GunControlInfo");
            SaveDataWithAESInStreamingAssets("GunControlInfo", allControlInfo.gunControlInfo, EncryptionKeyManager.GetDefaultKey("GunControlInfo"));
        }
        if (AESSkinAwardControlInfo)
        {
            allControlInfo.skinAwardControlInfo = JsonMgr.Instance.LoadData<SkinAwardControlInfo>("SkinAwardControlInfo");
            SaveDataWithAESInStreamingAssets("SkinAwardControlInfo", allControlInfo.skinAwardControlInfo, EncryptionKeyManager.GetDefaultKey("SkinAwardControlInfo"));
        }
        if (AESPlayerControlInfo)
        {
            allControlInfo.playerControlInfo = JsonMgr.Instance.LoadData<PlayerControlInfo>("PlayerControlInfo");
            SaveDataWithAESInStreamingAssets("PlayerControlInfo", allControlInfo.playerControlInfo, EncryptionKeyManager.GetDefaultKey("PlayerControlInfo"));
        }
        if (AESMonsterControlInfo)
        {
            allControlInfo.monsterControlInfo = JsonMgr.Instance.LoadData<MonsterControlInfo>("MonsterControlInfo");
            SaveDataWithAESInStreamingAssets("MonsterControlInfo", allControlInfo.monsterControlInfo, EncryptionKeyManager.GetDefaultKey("MonsterControlInfo"));
        }
    }

    /// <summary>
    /// 存储加密Json数据
    /// </summary>
    public void SaveDataWithAESInStreamingAssets(string path, object ob, string key, Json_Type type = Json_Type.LitJson)
    {
        if (ob == null) { return; }
        string savePath = Application.streamingAssetsPath + "/" + path + ".json";
        string jsonStr = "";
        switch (type)
        {
            case Json_Type.JsonUtlity:
                jsonStr = JsonUtility.ToJson(ob);
                break;
            case Json_Type.LitJson:
                jsonStr = JsonMapper.ToJson(ob);
                break;
        }

        // AES加密
        jsonStr = AesUtility.Encrypt(jsonStr, key);

        File.WriteAllText(savePath, jsonStr);
    }
}
