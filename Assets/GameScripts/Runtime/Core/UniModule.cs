using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UniModule
{
    private class Wrapper
    {
        public int Priority { private set; get; }
        public IModule Module { private set; get; }

        public Wrapper(IModule module, int priority)
        {
            Module = module;
            Priority = priority;
        }
    }

    private static bool _isInitialize = false;
    private static GameObject _driver = null; //需要利用MonoBehaviour的update
    private static readonly List<Wrapper> _wrappers = new(100);
    private static MonoBehaviour _behaviour;
    private static bool _isDirty = false;

    public static void Initialize()
    {
        if (_isInitialize)
            throw new Exception($"{nameof(UniModule)} is initialized !");

        if (_isInitialize == false)
        {
            // 创建驱动器
            _isInitialize = true;
            _driver = new UnityEngine.GameObject($"[{nameof(UniModule)}]");
            _behaviour = _driver.AddComponent<UniModuleDriver>();
            UnityEngine.Object.DontDestroyOnLoad(_driver);
            // UniLogger.Log($"{nameof(UniModule)} initalize !");
        }
    }

    public static void Destroy()
    {
        if (_isInitialize)
        {
            DestroyAll();

            _isInitialize = false;
            if (_driver != null)
                GameObject.Destroy(_driver);
            // UniLogger.Log($"{nameof(UniModule)} destroy all !");
        }
    }

    internal static void Update()
    {
        // 如果需要重新排序
        if (_isDirty)
        {
            _isDirty = false;
            _wrappers.Sort((left, right) =>
            {
                if (left.Priority > right.Priority)
                    return -1;
                else if (left.Priority == right.Priority)
                    return 0;
                else
                    return 1;
            });
        }

        // 轮询所有模块
        for (int i = 0; i < _wrappers.Count; i++)
        {
            _wrappers[i].Module.OnUpdate();
        }
    }

    public static T GetModule<T>() where T : class, IModule
    {
        Type type = typeof(T);
        for (int i = 0; i < _wrappers.Count; i++)
        {
            if (_wrappers[i].Module.GetType() == type)
                return _wrappers[i].Module as T;
        }

        // UniLogger.Error($"Not found manager : {type}");
        return null;
    }

    public static bool Contains<T>() where T : class, IModule
    {
        Type type = typeof(T);
        for (int i = 0; i < _wrappers.Count; i++)
        {
            if (_wrappers[i].Module.GetType() == type)
                return true;
        }

        return false;
    }

    public static T CreateModule<T>(int priority = 0) where T : class, IModule
    {
        return CreateModule<T>(null, priority);
    }

    public static T CreateModule<T>(System.Object createParam, int priority = 0) where T : class, IModule
    {
        if (priority < 0)
            throw new Exception("The priority can not be negative");

        if (Contains<T>())
            throw new Exception($"Module is already existed : {typeof(T)}");

        // 如果没有设置优先级
        if (priority == 0)
        {
            int minPriority = GetMinPriority();
            priority = --minPriority;
        }

        T module = Activator.CreateInstance<T>();
        Wrapper wrapper = new Wrapper(module, priority);
        wrapper.Module.OnCreate(createParam);
        _wrappers.Add(wrapper);
        _isDirty = true;
        return module;
    }

    private static int GetMinPriority()
    {//找到最小的优先级
        int minPriority = 0;
        for (int i = 0; i < _wrappers.Count; i++)
        {
            if (_wrappers[i].Priority < minPriority)
                minPriority = _wrappers[i].Priority;
        }

        return minPriority; //小于等于零
    }
    
    private static void DestroyAll()
    {
        for (int i = 0; i < _wrappers.Count; i++)
        {
            _wrappers[i].Module.OnDestroy();
        }
        _wrappers.Clear();
    }
}
    
    
