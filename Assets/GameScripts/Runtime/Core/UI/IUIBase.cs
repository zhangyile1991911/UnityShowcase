using UnityEngine;

public enum UILayer
{
    Bottom,
    Center,
    Top,
    Guide,
}

public interface IUIBase
{
    GameObject uiGo { get; set; }
    Transform uiTran { get; }
    UILayer uiLayer { get; set; }

    bool IsActive { get; }

    void Init(GameObject go);
    
    void OnCreate();

    void OnDestroy();
    
    void OnShow(UIOpenParam openParam);

    void OnHide();
    
    void OnUpdate();
}
