using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DerailValleyRandomEvents;

public static class AsyncHelper
{
    static GameObject _root;
    static CoroutineRunner _runner;

    static AsyncHelper()
    {
        CleanupHelper.Add(typeof(AsyncHelper), () =>
        {
            _runner!.StopAllCoroutines();

            GameObject.Destroy(_root);
            _root = null;
            _runner = null;
        });
    }

    static void EnsureRunner()
    {
        if (_root != null)
            return;

        _root = new GameObject("DerailValley_AsyncHelper");
        UnityEngine.Object.DontDestroyOnLoad(_root);

        _runner = _root.AddComponent<CoroutineRunner>();
    }

    public static void StartCoroutine(IEnumerator coroutine)
    {
        EnsureRunner();

        _runner.StartCoroutine(coroutine);
    }

    public static void Schedule(Action task, float delay)
    {
        EnsureRunner();

        _runner.Schedule(task, delay);
    }
}

public class CoroutineRunner : MonoBehaviour
{
    public IEnumerator Schedule(Action task, float delay)
    {
        yield return new WaitForSeconds(delay);

        task.Invoke();
    }
}