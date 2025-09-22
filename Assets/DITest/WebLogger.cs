using UnityEngine;

public class WebLogger : BaseLogger
{
    public override void PrintLog(string log)
    {
        Debug.Log($" WebLogger Log: {log}");
    }
}
