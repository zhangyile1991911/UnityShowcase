using System.Reflection.Emit;
using UnityEngine;
using VContainer;

public class TestLifetime : MonoBehaviour
{
    public Transform UIRoot;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // TestLifetimeScope tlts = gameObject.AddComponent<TestLifetimeScope>();
        // UnityLogger logger = (UnityLogger)tlts.Container.Resolve(typeof(UnityLogger));
        // logger.PrintLog(" ttesss");
    }
    
    [Inject]
    public void Constructor(BaseLogger log)
    {
        log.PrintLog("constructor");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
