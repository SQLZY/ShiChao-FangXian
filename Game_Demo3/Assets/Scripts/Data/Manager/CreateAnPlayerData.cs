using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateAnPlayerData : MonoBehaviour
{
    public int money = 1000000;
    public int level = 50;

    [ContextMenu("Ö´ÐÐ·½·¨CreatePlayerData")]
    public void CreatePlayerData()
    {
        PlayerData playerData = new PlayerData();
        playerData.money = money;
        int index = 0;

        while (level > 0)
        {
            ++playerData.sceneLevelInfo[index];
            --level;
            if (playerData.sceneLevelInfo[index] == 10 && level > 0)
            {
                playerData.killBoss.Add(index + 13);
                ++index;
            }
        }

        JsonMgr.Instance.SaveDataWithAES("PlayerData", playerData, EncryptionKeyManager.GetDeviceBasedKey());
    }
}
