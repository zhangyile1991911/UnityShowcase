using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using Object = UnityEngine.Object;


public class UIManager : SingletonModule<UIManager>
{
    // public static UIManager Instance => _instance;
    // private static UIManager _instance;

    private LRUCache<UIEnum, IUIBase> _uiCachedDic;

    private Transform _bottom;

    private Transform _center;

    private Transform _top;

    private Transform _guide;

    public Camera UICamera => _uiCamera;
    private Camera _uiCamera;
    public CanvasScaler RootCanvasScaler => _rootCanvasScaler;
    private CanvasScaler _rootCanvasScaler;

    public Canvas RootCanvas => _rootCanvas;
    private Canvas _rootCanvas;

    private UIDriver _uiDriver;
    public IUIBase Get(UIEnum uiName)
    {
        IUIBase ui = null;
        if (!_uiCachedDic.TryGetValue(uiName, out ui))
        {
            return null;
        }

        return ui;
    }

    private void OnOpenUI(IUIBase ui,Action<IUIBase> onComplete,UIOpenParam openParam,UILayer layer)
    {
        ui.OnShow(openParam);
        onComplete?.Invoke(ui);
    }
    
    public void OpenUI(UIEnum uiName,Action<IUIBase> onComplete,UIOpenParam openParam,UILayer layer = UILayer.Bottom,bool isPermanent=false)
    {
        IUIBase ui = null;
        if (_uiCachedDic.TryGetValue(uiName, out ui))
        {
            OnOpenUI(ui, onComplete, openParam,layer);
        }
        else
        {
            LoadUIAsync(uiName, loadUi=>
            {
                loadUi.OnCreate();
                OnOpenUI(loadUi,onComplete,openParam,layer);
            },layer,isPermanent).Forget();
        }
    }

    public void LoadUI(UIEnum uiName,Action<IUIBase> onComplete,UILayer layer = UILayer.Bottom,bool isPermanent=false)
    {
        IUIBase ui = null;
        if (!_uiCachedDic.TryGetValue(uiName, out ui))
        {
            LoadUIAsync(uiName,(loadUi)=>
            {
                loadUi.OnCreate();
                onComplete?.Invoke(loadUi);
            },layer,isPermanent).Forget();
        }
    }
    
    public UniTask OpenUI(UIEnum uiName,UIOpenParam openParam,UILayer layer = UILayer.Bottom,bool isPermanent=false)
    {
        IUIBase ui = null;
        if (_uiCachedDic.TryGetValue(uiName, out ui))
        {
            OnOpenUI(ui, null, openParam, layer);
            return default;
        }
        
        return UniTask.Create(async () =>
        { 
            await LoadUIAsync(uiName, (loadUi) =>
            {
                loadUi.OnCreate();
                OnOpenUI(loadUi, null, openParam, layer);
            }, layer, isPermanent);
            
        });
    }

    public void CloseUI(UIEnum uiName)
    {
        IUIBase ui = null;
        if (_uiCachedDic.TryGetValue(uiName, out ui))
        {
            ui.OnHide();
        }
    }

    public void DestroyUI(UIEnum uiName)
    {
        _uiCachedDic.Remove(uiName);
    }

    private Transform getParentNode(UILayer layer)
    {
        switch (layer)
        {
            case UILayer.Bottom:
                return _bottom;
            case UILayer.Center:
                return _center;
            case UILayer.Top:
                return _top;
            case UILayer.Guide:
                return _guide;
        }

        return null;
    }

    private async UniTask LoadUIAsync(UIEnum uiName, Action<IUIBase> onComplete,UILayer layer,bool isPermanent)
    {
        Type uiType = Type.GetType(uiName.ToString());
        var attributes = uiType.GetCustomAttributes(false);
        var uiPath = attributes
            .Where(one => one is UIAttribute)
            .Select(tmp=> (tmp as UIAttribute).ResPath).FirstOrDefault();
        
        var handle = YooAssets.LoadAssetAsync<GameObject>(uiPath);
        await handle.ToUniTask(_uiDriver);
        
        var uiPrefab = handle.AssetObject;
        var parentNode = getParentNode(layer);
        var uiGameObject = GameObject.Instantiate(uiPrefab,parentNode) as GameObject;
        uiGameObject.transform.localScale = Vector3.one;
        uiGameObject.transform.SetParent(parentNode,false);
        IUIBase ui = Activator.CreateInstance(uiType) as IUIBase;
        ui.uiLayer = layer;
        ui.Init(uiGameObject);
        onComplete?.Invoke(ui);
        _uiCachedDic.Add(uiName,ui,isPermanent);
    }

    public void DestroyUIComponent(UIComponent component)
    {
        
        component?.OnHide();
        component?.OnDestroy();
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    public async UniTask<T> CreateUIComponent<T>(UIOpenParam openParam,Transform node,UIWindow parent,bool show=true)where T : UIComponent
    {
        var componentType = typeof(T);
        
        var attributes = componentType.GetCustomAttributes(false);
        var uiPath = attributes
            .Where(one => one is UIAttribute)
            .Select(tmp=> (tmp as UIAttribute).ResPath).FirstOrDefault();
        
        var handle = YooAssets.LoadAssetAsync<GameObject>(uiPath);
        await handle.ToUniTask(_uiDriver);
        
        var uiPrefab = handle.AssetObject;
        
        var uiGameObject = Object.Instantiate(uiPrefab,node) as GameObject;
        uiGameObject.transform.localPosition = Vector3.zero;
        uiGameObject.transform.localScale = Vector3.one;
        T uiComponent = Activator.CreateInstance(componentType,new object[]{uiGameObject,parent}) as T;
        // uiComponent.OnCreate();
        if (show)
        {
            uiComponent.OnShow(openParam);    
        }
        else
        {
            uiComponent.OnHide();
        }
        
        return uiComponent;
    }

    public async UniTask<T> CreateTip<T>(UIOpenParam openParam)where T : UIComponent
    {
        var tip = CreateUIComponent<T>(openParam, _top, null) as T;
        return tip;
    }

    public override void OnCreate(object createParam)
    {
        _uiCachedDic = new LRUCache<UIEnum, IUIBase>(10);
        _uiCachedDic.OnRemove += (ui) =>
        {
            ui.OnHide();
            ui.OnDestroy();
        };
        //在场景里找到UIModule节点
        var uiModule = GameObject.Find("UIModule");

        var root = uiModule.transform.Find("UIRoot");
        _rootCanvasScaler = root.GetComponent<CanvasScaler>();
        _rootCanvas = root.GetComponent<Canvas>();
        _uiDriver = root.GetComponent<UIDriver>();

        
        var cam = GameObject.Find("UICamera");
        _uiCamera = cam.GetComponent<Camera>();
        
        _bottom = uiModule.transform.Find("UIRoot/Bottom");
        _center = uiModule.transform.Find("UIRoot/Center");
        _top = uiModule.transform.Find("UIRoot/Top");
        _guide = uiModule.transform.Find("UIRoot/Guide");
        GameObject.DontDestroyOnLoad(uiModule);
        base.OnCreate(this);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        // Debug.Log("UIManager::OnUpdate");
        // foreach (var VARIABLE in _uiCachedDic)
        // {
        //     
        // }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    
    public Vector2 WorldPositionToUI(Vector3 worldPosition)
    {
        //将世界坐标转换到UI坐标
        var screenPosition = _uiCamera.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(),screenPosition,_uiCamera,out var localPos);
        return localPos;
    }
    
}
