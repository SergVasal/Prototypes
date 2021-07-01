using UnityEngine;
using Zenject;

public class TestInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        //Container.Bind<Greeter>().AsSingle().NonLazy();

        Container.Bind<Greeter>().To<Greeter>().AsSingle().NonLazy();

        var foo = new Foo();
        Container.Bind<Foo>().FromInstance(foo);
        Container.QueueForInject(foo);
        Container.Bind<string>().FromInstance("Hello World!");
    }
}

public class Greeter : IInitializable
{
    [Inject]
    private string injectedString;

    public float Number { get; } = 5f;

    public Greeter(Foo foo)
    {
        foo.Start();
        Debug.Log($"Greeter Constructor injectedString: {injectedString}");

    }

    public void Initialize()
    {
        Debug.Log($"Initialize!!!");
    }
}

public class Foo
{
    [Inject]
    private string injectedString;

    public void Start()
    {
        Debug.Log($"Start Foo injectedString is null: {injectedString}");
    }

}
