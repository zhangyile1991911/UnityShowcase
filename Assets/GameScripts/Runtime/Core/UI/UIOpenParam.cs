using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIOpenParam
{
    public T Get<T>() where T : UIOpenParam
    {
        return this as T;
    }
}
