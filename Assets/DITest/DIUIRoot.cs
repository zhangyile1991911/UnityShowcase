using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DIUIRoot : MonoBehaviour
{
    IObjectResolver _diResolver;
    public GameObject TestPrefab;
    [Inject]
    public void Construct(IObjectResolver diResolver)
    {
        _diResolver = diResolver;
        Debug.Log("DIUIRoot constructed");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _diResolver.Instantiate(TestPrefab, transform.position, Quaternion.identity);
            // Instantiate(TestPrefab,transform.position,Quaternion.identity);
            PureCSharpClass pureCSharpClass = _diResolver.Resolve<PureCSharpClass>();
            pureCSharpClass.Test();
        }
    }
}
