using System;
using System.Reflection;

/// <summary>
/// 纯 C# 单例基类：EventCenter / FactoryCenter / ConfigCenter 等继承。
/// Instance 懒加载，反射调用无参私有构造；重复创建直接抛异常。
/// </summary>
public abstract class BasicSingle<T> where T : BasicSingle<T>
{
    private static T instance;

    protected BasicSingle()
    {
        if (instance != null)
        {
            throw new InvalidOperationException("单例已存在，不能重复创建。");
        }
        instance = (T)this;
    }

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                CreateInstance();
            }
            return instance;
        }
    }

    private static void CreateInstance()
    {
        ConstructorInfo ctor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            Type.EmptyTypes,
            null);
        if (ctor == null)
        {
            throw new InvalidOperationException($"{typeof(T).Name} 必须提供无参构造函数，并且不能是 public。");
        }
        instance = (T)ctor.Invoke(null);
    }
}
