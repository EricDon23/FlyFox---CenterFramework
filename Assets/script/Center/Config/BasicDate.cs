using UnityEngine;

/// <summary>
/// 纯数据容器：只存储数据，不包含任何类或逻辑。
/// 配置类（带 [AutoRegisterConfig] 的具体数据）继承本类。
/// </summary>
[System.Serializable]
public class BasicDate
{
    public int index;
    public string name;
}
