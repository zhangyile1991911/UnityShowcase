using UnityEngine;
using VContainer;

public class TestPrefab : MonoBehaviour
{
    [Inject]
    void Constructor(BaseLogger baseLogger)
    {
        baseLogger.PrintLog("TestPrefab::Constructor");
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
