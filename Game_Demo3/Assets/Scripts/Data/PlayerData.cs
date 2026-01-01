using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家数据
/// </summary>
public class PlayerData
{
    //金钱
    public int money = 0;
    //皮肤消费金额 单位为3K
    public int consumeMoney = 0;
    //角色购买
    public List<int> buyHero = new List<int>();
    //皮肤购买
    public List<int> buySkin = new List<int>();
    //击败Boss列表
    public List<int> killBoss = new List<int>();
    //累计杀敌数量
    public int killMonsterCount = 0;

    //当前选择皮肤信息
    public SkinInfo nowSelSkinInfo;

    //玩家基础生命值
    public int playerBasicHp = 50;
    //保护区基础生命值
    public int mainTowerBasicHp = 100;
    //关卡初始金钱
    public int basicMoney = 50;

    //最高炮塔等级
    public int maxTowerLevel = 1;

    //各个场景关卡等级 0代表未解锁状态
    public int[] sceneLevelInfo = new int[] { 1, 0, 0, 0, 0 };
    //无尽模式最大坚持波数
    public int endlessModeMaxWave = 0;
    //通关状态
    public bool isWinAllGame = false;
}
