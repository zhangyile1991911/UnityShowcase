
public interface IModule
{
    /// <summary>
    /// 创建模块
    /// </summary>
    void OnCreate(System.Object createParam);

    /// <summary>
    /// 更新模块
    /// </summary>
    void OnUpdate();

    /// <summary>
    /// 销毁模块
    /// </summary>
    void OnDestroy();
}

public abstract class SingletonModule<T> : IModule where T : class
{
    public static T Instance => _instance;
    
    protected static T _instance;
    
    public bool IsCreate { get; protected set; }
    public bool IsDestroy { get; protected set; }
    public virtual void OnCreate(System.Object createParam)
    {
        IsCreate = true;
        IsDestroy = false;
        _instance = createParam as T;
    }

    /// <summary>
    /// 更新模块
    /// </summary>
    public virtual void OnUpdate()
    {
        
    }

    /// <summary>
    /// 销毁模块
    /// </summary>
    public virtual void OnDestroy()
    {
        IsDestroy = true;
        _instance = null;
    }
}
