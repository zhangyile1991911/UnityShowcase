using UnityEngine;
using VContainer;

public class PureCSharpClass
{
    [Inject]
    BaseLogger logger;

    
    public PureCSharpClass()
    {
        
    }

    public void Test()
    {
        logger.PrintLog("PureCSharpClass constructor");
    }
}
