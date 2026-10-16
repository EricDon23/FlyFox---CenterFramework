using System;
using System.Collections.Generic;
using UnityEngine;

public class InputCenter : BasicSingle<InputCenter>
{
    // 映射表：逻辑按键名 → 物理按键名称字符串
    private Dictionary<string, string> inputMap = new Dictionary<string, string>();
    // 映射表：逻辑轴名 → 物理轴名称字符串（如 "Horizontal"）
    private Dictionary<string, string> axisMap = new Dictionary<string, string>();

    /// <summary>
    /// 设置按键映射（新增/覆盖）
    /// name: 逻辑按键名（业务使用，例如"Jump"）
    /// keyName: 物理按键名，对应KeyCode的字符串，例如"Space"、"A"
    /// </summary>
    public void SetKeyMap(string name, string keyName)
    {
        inputMap[name] = keyName;
    }

    /// <summary>
    /// 移除按键映射
    /// </summary>
    public void RemoveKeyMap(string name)
    {
        if(inputMap.ContainsKey(name))
        {
            inputMap.Remove(name);
        }
    }

    /// <summary>
    /// 获取【本帧按下瞬间】逻辑按键
    /// </summary>
    public bool GetKeyDown(string name)
    {
        if (!InputEnable) return false;
        if (!inputMap.TryGetValue(name, out string keyStr))
        {
            Debug.LogWarning($"未配置按键映射：{name}");
            return false;
        }
        // 字符串转KeyCode
        if (Enum.TryParse<KeyCode>(keyStr, out KeyCode keyCode))
        {
            return Input.GetKeyDown(keyCode);
        }
        Debug.LogError($"无法识别KeyCode：{keyStr}");
        return false;
    }

    /// <summary>
    /// 获取【持续按住】逻辑按键
    /// </summary>
    public bool GetKeyHold(string name)
    {
        if (!InputEnable) return false;
        if (!inputMap.TryGetValue(name, out string keyStr))
        {
            Debug.LogWarning($"未配置按键映射：{name}");
            return false;
        }
        if (Enum.TryParse<KeyCode>(keyStr, out KeyCode keyCode))
        {
            return Input.GetKey(keyCode);
        }
        Debug.LogError($"无法识别KeyCode：{keyStr}");
        return false;
    }

    /// <summary>
    /// 获取【本帧抬起瞬间】逻辑按键
    /// </summary>
    public bool GetKeyUp(string name)
    {
        if (!InputEnable) return false;
        if (!inputMap.TryGetValue(name, out string keyStr))
        {
            Debug.LogWarning($"未配置按键映射：{name}");
            return false;
        }
        if (Enum.TryParse<KeyCode>(keyStr, out KeyCode keyCode))
        {
            return Input.GetKeyUp(keyCode);
        }
        Debug.LogError($"无法识别KeyCode：{keyStr}");
        return false;
    }

    // 可选：全局开关，暂停时屏蔽输入
    public bool InputEnable = true;

    /// <summary>
    /// 设置轴映射（新增/覆盖）
    /// name: 逻辑轴名（业务使用，例如 "Horizontal"）
    /// axisName: 物理轴名称字符串，对应 Input.GetAxisRaw 的参数，例如 "Horizontal"、"Vertical"
    /// </summary>
    public void SetAxisMap(string name, string axisName)
    {
        axisMap[name] = axisName;
    }

    /// <summary>
    /// 移除轴映射
    /// </summary>
    public void RemoveAxisMap(string name)
    {
        if (axisMap.ContainsKey(name))
        {
            axisMap.Remove(name);
        }
    }

    /// <summary>
    /// 获取轴原始值（无平滑、松开立即归零），受全局输入开关约束
    /// </summary>
    public float GetAxisRaw(string name)
    {
        if (!InputEnable) return 0f;
        if (!axisMap.TryGetValue(name, out string axisStr))
        {
            Debug.LogWarning($"未配置轴映射：{name}");
            return 0f;
        }
        return Input.GetAxisRaw(axisStr);
    }

    /// <summary>
    /// 便捷获取二维移动向量（默认水平/垂直轴）
    /// </summary>
    public Vector2 GetMoveVector(string horizontalName = "Horizontal", string verticalName = "Vertical")
    {
        return new Vector2(GetAxisRaw(horizontalName), GetAxisRaw(verticalName));
    }
}
