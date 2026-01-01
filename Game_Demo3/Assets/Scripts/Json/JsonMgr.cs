using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum Json_Type
{
    JsonUtlity,
    LitJson,
}

public class JsonMgr
{
    private static JsonMgr instance = new JsonMgr();
    public static JsonMgr Instance => instance;
    private JsonMgr() { }

    public void SaveData(string path, object ob, Json_Type type = Json_Type.LitJson)
    {
        if (ob == null) { return; }
        string savePath = Application.persistentDataPath + "/" + path + ".json";
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
        File.WriteAllText(savePath, jsonStr);
    }

    public T LoadData<T>(string path, Json_Type type = Json_Type.LitJson) where T : new()
    {
        //数据读取文件路径
        string savePath = Application.streamingAssetsPath + "/" + path + ".json";
        if (!File.Exists(savePath))
        {
            savePath = Application.persistentDataPath + "/" + path + ".json";
        }
        if (!(File.Exists(savePath)))
        {
            return new T();
        }
        string jsonStr = File.ReadAllText(savePath);
        switch (type)
        {
            case Json_Type.JsonUtlity:
                return JsonUtility.FromJson<T>(jsonStr);
            case Json_Type.LitJson:
                return JsonMapper.ToObject<T>(jsonStr);
            default:
                return new T();
        }
    }

    /// <summary>
    /// 存储加密Json数据
    /// </summary>
    public void SaveDataWithAES(string path, object ob, string key, Json_Type type = Json_Type.LitJson)
    {
        if (ob == null) { return; }
        string savePath = Application.persistentDataPath + "/" + path + ".json";
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

    /// <summary>
    /// 读取加密Json数据
    /// </summary>
    public T LoadDataWithAES<T>(string path, string key, Json_Type type = Json_Type.LitJson) where T : new()
    {
        //数据读取文件路径
        string savePath = Application.streamingAssetsPath + "/" + path + ".json";
        if (!File.Exists(savePath))
        {
            savePath = Application.persistentDataPath + "/" + path + ".json";
        }
        if (!(File.Exists(savePath)))
        {
            return new T();
        }
        string jsonStr = File.ReadAllText(savePath);

        // AES解密
        jsonStr = AesUtility.Decrypt(jsonStr, key);

        switch (type)
        {
            case Json_Type.JsonUtlity:
                return JsonUtility.FromJson<T>(jsonStr);
            case Json_Type.LitJson:
                return JsonMapper.ToObject<T>(jsonStr);
            default:
                return new T();
        }
    }
}
