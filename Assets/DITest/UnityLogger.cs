using UnityEngine;

public class UnityLogger : BaseLogger
{
    public override void PrintLog(string log)
    {
        Debug.Log($" Unity Log: {log}");
    }
}
