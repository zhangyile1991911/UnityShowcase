using System;

[AttributeUsage(AttributeTargets.Class)]
public class UIAttribute : Attribute
{
    public int Id;
    public string ResPath;
    public UIAttribute(int id,string path)
    {
        Id = id;
        ResPath = path;
    }
}
