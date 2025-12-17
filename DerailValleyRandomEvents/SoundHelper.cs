using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class SoundHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    static readonly Dictionary<string, AudioClip> _cache = new();
    static GameObject _root;
    static AudioSource _oneShotSource;
    static string soundsPath = Path.Combine(Main.ModEntry.Path, "Dependencies");

    static SoundHelper()
    {
        CleanupHelper.Add(typeof(SoundHelper), () =>
        {
            GameObject.Destroy(_root);
            _root = null;
            _oneShotSource = null;
        });
    }

    static void Ensure()
    {
        _root = new GameObject("DerailValley_AsyncHelper");
        UnityEngine.Object.DontDestroyOnLoad(_root);

        _oneShotSource = _root.AddComponent<AudioSource>();
        _oneShotSource.spatialBlend = 1f;
        _oneShotSource.rolloffMode = AudioRolloffMode.Linear;
        _oneShotSource.maxDistance = 50f;
    }

    public static void PlaySound(string pathInsideDependencies, Vector3? position = null, bool spatial = false, float volume = 1f)
    {
        Ensure();

        var fullPath = Path.Combine(soundsPath, pathInsideDependencies);
        if (!File.Exists(fullPath))
        {
            Logger.Log($"Cannot play sound - does not exist: {fullPath}");
            return;
        }

        var soundPos = position ?? PlayerManager.PlayerTransform.position;

        AsyncHelper.StartCoroutine(LoadAndPlay(fullPath, soundPos, spatial, volume));
    }

    static IEnumerator LoadAndPlay(string fullPath, Vector3 position, bool spatial, float volume = 1f)
    {
        if (_cache.TryGetValue(fullPath, out var cached))
        {
            PlayClip(sourcePath: fullPath, cached, position, spatial, volume);
            yield break;
        }

        var url = "file://" + fullPath.Replace("\\", "/");

        Logger.Log($"Load {fullPath} => {url}");

        using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        yield return req.SendWebRequest();

        Logger.Log($"Load {url} done isNetworkError={req.isNetworkError} isHttpError={req.isHttpError}");

        if (req.isNetworkError || req.isHttpError)
            yield break;

        var clip = DownloadHandlerAudioClip.GetContent(req);
        if (clip == null)
            yield break;

        _cache[fullPath] = clip;

        Logger.Log($"Clip loaded: {clip}");

        PlayClip(sourcePath: fullPath, clip, position, spatial, volume);
    }

    static void PlayClip(string sourcePath, AudioClip clip, Vector3 position, bool spatial, float volume = 1f)
    {
        Logger.Log($"Play clip source={sourcePath} clip={clip} pos={position} spatial={spatial} volume={volume}");

        if (!spatial)
        {
            _oneShotSource.PlayOneShot(clip, volumeScale: volume);
            return;
        }

        // TODO: get this to work - setting to player's position doesnt do it

        var go = new GameObject("OneShotSound");
        go.transform.position = position;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = 50f;
        src.volume = volume;
        src.Play();

        Object.Destroy(go, clip.length + 0.1f);
    }

}
