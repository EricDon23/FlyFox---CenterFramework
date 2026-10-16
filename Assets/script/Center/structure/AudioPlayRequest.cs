using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频播放请求参数（AudioCenter.Play 的入参）。
/// </summary>
public class AudioPlayRequest
{
    /// <summary>音频资源 key（ResCenter 路径，如 res://xxx 或 Resources 路径）</summary>
    public string ResKey;

    /// <summary>音频分组（BGM / SFX / Voice）</summary>
    public AudioGroupType Group;

    /// <summary>音量（0~1，最终音量 = 组音量 × 此值）</summary>
    public float Volume = 1f;

    /// <summary>是否循环（仅 BGM 有效）</summary>
    public bool Loop;

    /// <summary>是否忽略暂停</summary>
    public bool IgnorePause;

    /// <summary>BGM 交叉淡入时间（秒，>0 启用淡入淡出）</summary>
    public float CrossFadeTime;

    public AudioPlayRequest(string resKey, AudioGroupType group)
    {
        ResKey = resKey;
        Group = group;
    }
}