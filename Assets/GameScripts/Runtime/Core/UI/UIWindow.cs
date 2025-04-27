using System;
using System.Collections.Generic;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

public class UIWindow : IUIBase
{
    public GameObject uiGo
    {
        get => _uiGo;
        set => _uiGo = value;
    }
    public Transform uiTran
    {
        get => _uiGo.transform;
    }
    private GameObject _uiGo;

    public UILayer uiLayer
    {
        get => _uiLayer;
        set => _uiLayer = value;
    }
    
    public bool IsActive => uiGo.active;

    private List<UIComponent> _childComponent = new List<UIComponent>(10);
    private List<AssetOperationHandle> _resHandles = new List<AssetOperationHandle>(10);
    protected CompositeDisposable handles = null;
    private UILayer _uiLayer;
    public virtual void Init(GameObject go)
    {
        
    }

    public virtual void OnCreate()
    {

    }

    public virtual void OnDestroy()
    {
        foreach (var h in _resHandles)
        {
            h.Release();
            h.Dispose();
        }
        //todo 清理子widget
        GameObject.Destroy(uiGo);
    }

    public virtual void OnShow(UIOpenParam openParam)
    {
        handles = new CompositeDisposable(10);
        uiGo.SetActive(true);
        for (int i = 0; i < _childComponent.Count; i++)
        {
            _childComponent[i]?.OnShow(openParam);
        }
    }

    public virtual void OnHide()
    {
        uiGo.SetActive(false);
        handles?.Dispose();
        handles?.Clear();
        for (int i = 0; i < _childComponent.Count; i++)
        {
            _childComponent[i]?.OnHide();
        }
    }

    public virtual void OnUpdate()
    {
        
    }

    public void AddChildComponent(UIComponent uiComponent)
    {
        if (uiComponent != null)
        {
            _childComponent.Add(uiComponent);    
        }
    }

    public void RemoveChildComponent(UIComponent uiComponent)
    {
        if (uiComponent != null)
        {
            _childComponent.Remove(uiComponent);    
        }
    }

    public async void LoadAssetAsync<T>(string resPath,Action<T> complete)where T : UnityEngine.Object
    {
        var handler = YooAssets.LoadAssetAsync<T>(resPath);
        _resHandles.Add(handler);
        await handler.ToUniTask();
        complete?.Invoke(handler.AssetObject as T);
    }
    
    public async UniTask LoadSpriteAsync(string resPath,Action<Sprite> complete)
    {
        var handler = YooAssets.LoadAssetAsync<Sprite>(resPath);
        _resHandles.Add(handler);
        await handler.ToUniTask();
        complete?.Invoke(handler.AssetObject as Sprite);
    }

    public void LoadSpriteAsync(string resPath, Image image)
    {
        var handler = YooAssets.LoadAssetAsync<Sprite>(resPath);
        _resHandles.Add(handler);
        handler.Completed += handle =>
        {
            image.sprite = handle.AssetObject as Sprite;
        };
    }

    public async UniTask<GameObject> LoadPrefabAsync(string prefabPath)
    {
        var handler = YooAssets.LoadAssetAsync<GameObject>(prefabPath);
        _resHandles.Add(handler);
        
        await handler.ToUniTask();
        return (GameObject)handler.AssetObject;
    }

    public void LoadPrefabAsync(string prefabPath,Action<GameObject> complete)
    {
        var handler = YooAssets.LoadAssetAsync<GameObject>(prefabPath);
        _resHandles.Add(handler);
        handler.Completed += param =>
        {
            complete?.Invoke(param.AssetObject as GameObject);
        };
    }
}
