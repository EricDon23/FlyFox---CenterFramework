using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频中心：统一管理背景音乐(BGM)与音效(SFX/Voice)播放
/// 通过 ResCenter 加载 AudioClip，按 AudioGroupType 分组控制音量，支持 BGM 交叉淡入
/// 通过 AudioCenter.Instance 全局访问
/// </summary>
public class AudioCenter : BasicSingle<AudioCenter>
{
    // 音频组音量（0~1），Master 为总音量
    private readonly Dictionary<AudioGroupType, float> _groupVolumes = new Dictionary<AudioGroupType, float>
    {
        { AudioGroupType.Master, 1f },
        { AudioGroupType.BGM, 1f },
        { AudioGroupType.SFX, 1f },
        { AudioGroupType.Voice, 1f },
    };

    // 音频源宿主（不随场景销毁）
    private GameObject _audioRoot;
    private AudioSource _bgmSource;
    private ResHandle _bgmHandle;
    private int _bgmGeneration = 0;
    private readonly List<AudioSource> _sfxSources = new List<AudioSource>();

    /// <summary>
    /// 按请求参数播放音频
    /// </summary>
    /// <param name="request">音频播放请求</param>
    public void Play(AudioPlayRequest request)
    {
        EnsureRoot();

        ResHandle handle = ResCenter.Instance.LoadSync<AudioClip>(request.ResKey);
        if (handle == null || !(handle.AssetObj is AudioClip clip))
        {
            Debug.LogError($"[AudioCenter] 加载音频失败：{request.ResKey}");
            return;
        }

        float volume = GetVolume(request.Group) * request.Volume;

        if (request.Group == AudioGroupType.BGM)
        {
            PlayBGM(clip, volume, request, handle);
        }
        else
        {
            PlaySfx(clip, volume, request, handle);
        }
    }

    /// <summary>停止背景音乐</summary>
    public void StopBGM()
    {
        if (_bgmSource != null)
        {
            _bgmSource.Stop();
        }
        if (_bgmHandle != null)
        {
            _bgmHandle.Release();
            _bgmHandle = null;
        }
    }

    /// <summary>设置音频组音量（0~1）</summary>
    public void SetGroupVolume(AudioGroupType group, float volume)
    {
        _groupVolumes[group] = Mathf.Clamp01(volume);
    }

    /// <summary>清空所有音频，场景切换时调用</summary>
    public void ClearAll()
    {
        StopBGM();
        foreach (AudioSource source in _sfxSources)
        {
            source.Stop();
        }
    }

    private void EnsureRoot()
    {
        if (_audioRoot != null)
        {
            return;
        }
        _audioRoot = new GameObject("[AudioCenter]");
        Object.DontDestroyOnLoad(_audioRoot);
    }

    /// <summary>播放背景音乐（持有句柄，直到下一次切换/停止才释放）</summary>
    private void PlayBGM(AudioClip clip, float volume, AudioPlayRequest request, ResHandle handle)
    {
        // 每次切换 BGM 递增代数，使在途的交叉淡入协程失效，避免并发写入同一 _bgmSource
        _bgmGeneration++;

        if (_bgmSource == null)
        {
            _bgmSource = _audioRoot.AddComponent<AudioSource>();
        }
        _bgmSource.ignoreListenerPause = request.IgnorePause;

        ResHandle oldHandle = _bgmHandle;
        _bgmHandle = handle;

        // 无交叉淡入：直接切换，立即释放旧BGM
        if (request.CrossFadeTime <= 0f)
        {
            if (oldHandle != null)
            {
                oldHandle.Release();
            }
            _bgmSource.clip = clip;
            _bgmSource.loop = request.Loop;
            _bgmSource.volume = volume;
            _bgmSource.Play();
            return;
        }

        // 交叉淡入：旧BGM句柄由协程负责释放（含中途被打断路径）
        SingleMono.Instance.StartGlobalCoroutine(CrossFadeBGM(clip, volume, request, oldHandle));
    }

    /// <summary>播放音效/语音（一次性，播放结束自动释放句柄）</summary>
    private void PlaySfx(AudioClip clip, float volume, AudioPlayRequest request, ResHandle handle)
    {
        AudioSource source = GetIdleSfxSource();
        source.ignoreListenerPause = request.IgnorePause;
        source.PlayOneShot(clip, volume);

        // 音频播放结束后释放资源句柄，避免提前卸载
        SingleMono.Instance.StartGlobalCoroutine(ReleaseAfter(handle, clip.length));
    }

    private IEnumerator CrossFadeBGM(AudioClip clip, float volume, AudioPlayRequest request, ResHandle oldHandle)
    {
        int generation = _bgmGeneration;

        // 淡出旧BGM（旧句柄保留，直到淡出结束或被新BGM打断）
        if (_bgmSource.isPlaying)
        {
            float start = _bgmSource.volume;
            float t = 0f;
            while (t < request.CrossFadeTime)
            {
                if (generation != _bgmGeneration)
                {
                    // 被新 BGM 打断：旧 BGM 不再需要，立即释放旧句柄，防止泄漏
                    if (oldHandle != null) oldHandle.Release();
                    yield break;
                }
                t += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(start, 0f, t / request.CrossFadeTime);
                yield return null;
            }
            _bgmSource.Stop();
        }
        if (oldHandle != null)
        {
            oldHandle.Release();
        }

        // 若期间已被新 BGM 切换，放弃后续淡入（旧句柄已在上方释放）
        if (generation != _bgmGeneration) yield break;

        // 淡入新BGM
        _bgmSource.clip = clip;
        _bgmSource.loop = request.Loop;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        float fadeIn = 0f;
        while (fadeIn < request.CrossFadeTime)
        {
            if (generation != _bgmGeneration) yield break;
            fadeIn += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, volume, fadeIn / request.CrossFadeTime);
            yield return null;
        }
        _bgmSource.volume = volume;
    }

    private IEnumerator ReleaseAfter(ResHandle handle, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        handle.Release();
    }

    /// <summary>优先复用空闲的音效源，无空闲则扩容</summary>
    private AudioSource GetIdleSfxSource()
    {
        for (int i = 0; i < _sfxSources.Count; i++)
        {
            if (!_sfxSources[i].isPlaying)
            {
                return _sfxSources[i];
            }
        }

        AudioSource source = _audioRoot.AddComponent<AudioSource>();
        source.playOnAwake = false;
        _sfxSources.Add(source);
        return source;
    }

    private float GetVolume(AudioGroupType group)
    {
        float master = _groupVolumes.TryGetValue(AudioGroupType.Master, out float m) ? m : 1f;
        float groupVolume = _groupVolumes.TryGetValue(group, out float v) ? v : 1f;
        return master * groupVolume;
    }
}


