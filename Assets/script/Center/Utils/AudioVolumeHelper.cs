using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioVolumeHelper 
{
    /// <summary>0~1音量转为Mixer分贝，0→-80db，1→0db</summary>
    public static float VolumeToDb(float volume01)
    {
        // 防止Log10(0)报错
        volume01 = Mathf.Clamp01(volume01);
        // 公式：dB = 20 * log10(V/V0)，V0=1，V=volume01S
        return volume01 <= 0 ? -80f : 20f * Mathf.Log10(volume01);
    }
}
