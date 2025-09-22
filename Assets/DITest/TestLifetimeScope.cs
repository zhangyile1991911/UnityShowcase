using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TestLifetimeScope : LifetimeScope
{
    // [SerializeField]private TestLifetime testLifetime;
    // [SerializeField] private Transform UIRoot;
    // [SerializeField] private TestPrefab Prefab;
    protected override void Configure(IContainerBuilder builder)
    {
        // builder.Register<WebLogger>(Lifetime.Singleton);
        builder.Register<BaseLogger, UnityLogger>(Lifetime.Singleton);
        // builder.RegisterComponent(testLifetime);

        builder.Register<PureCSharpClass>(Lifetime.Transient);
        //往Hierarchy中已经存在的TestPrefab中注入
        builder.RegisterComponentInHierarchy(typeof(DIUIRoot));

        // builder.RegisterComponentOnNewGameObject<TestPrefab>(Lifetime.Transient);
    }
}
