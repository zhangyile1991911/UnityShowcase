using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class UIFieldRule
{
    public string prefixName;
    public string typeName;
}

[Serializable,CreateAssetMenu(menuName = "UI/CreateAutoCreateInfoConfig")]
public class UIAutoCreateInfoConfig : ScriptableObject
{
    public List<UIFieldRule> uiInfoList;

    public string UIWindowTemplatePath;
    public string UIControlTemplatePath;
    public string UIComponentTemplatePath;
    public string UIComponentClassTemplatePath;
    public string WindowScriptPath;
    public string ComponentScriptPath;
    public string WindowPrefabPath;
    public string ComponentPrefabPath;
}

