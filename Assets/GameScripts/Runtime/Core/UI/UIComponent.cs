using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIComponent : IUIBase
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

    public RectTransform uiRectTran
    {
        get => _rectTransform;
    }

    private RectTransform _rectTransform;
    public UILayer uiLayer
    {
        get;
        set;
    }

    public bool IsActive => uiGo.active;
    
    public UIWindow ParentWindow
    {
        get => _parentWindow;
    }
    private GameObject _uiGo;
    private UIWindow _parentWindow;

    public UIComponent(GameObject go,UIWindow parent)
    {
        _uiGo = go;
        _parentWindow = parent;
        _parentWindow?.AddChildComponent(this);
        _rectTransform = go.GetComponent<RectTransform>();
        
        Init(go);
        OnCreate();
    }
    
    public virtual void Init(GameObject go)
    {
        
    }

    public virtual void OnCreate()
    {

    }

    public virtual void OnDestroy()
    {
        _parentWindow?.RemoveChildComponent(this);
        GameObject.Destroy(uiGo);
    }

    public virtual void OnShow(UIOpenParam openParam)
    {
        uiGo.SetActive(true);
    }

    public virtual void OnHide()
    {
        uiGo.SetActive(false);
    }

    public virtual void OnUpdate()
    {
    }
}
