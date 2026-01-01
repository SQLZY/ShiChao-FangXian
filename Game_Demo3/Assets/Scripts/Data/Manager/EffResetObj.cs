using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 特效对象池回收时状态重置组件
/// </summary>
public class EffResetObj : MonoBehaviour, IResetState
{

    // 缓存初始组件参数
    private List<ParticleSystem> particleSystems;
    private AudioSource audioSource;
    private Animator animator;

    private void Awake()
    {
        // 预获取所有粒子系统（包含子对象）
        particleSystems = new List<ParticleSystem>(GetComponentsInChildren<ParticleSystem>(true));

        // 获取其他常用组件
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    public void ResetState()
    {
        //---------------- 粒子系统深度重置 ----------------
        foreach (var ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);  // 停止并清除所有粒子
            var main = ps.main;
            main.playOnAwake = false;  // 禁用自动播放
            ps.time = 0;               // 重置时间轴
            ps.Clear();                // 清除残留粒子

            // 重置子发射器（如果有）
            var subEmitters = ps.subEmitters;
            for (int i = 0; i < subEmitters.subEmittersCount; i++)
            {
                subEmitters.GetSubEmitterSystem(i).Stop(true);
            }
        }

        //---------------- 动画系统重置 ----------------
        if (animator != null)
        {
            animator.Rebind();          // 重置所有动画参数
            animator.Update(0f);        // 强制更新到初始状态
            animator.enabled = false;   // 禁用组件（下次使用前需要手动激活）
        }

        //---------------- 音频重置 ----------------
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0;       // 重置音频时间轴
            audioSource.enabled = false;// 禁用组件
        }
    }
}