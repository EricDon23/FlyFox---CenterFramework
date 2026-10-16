using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 自动键盘锁定管理器（无需手动调用，游戏启动自动运行）
/// 功能：自动开启大写锁定 + 全程监控强制保持英文输入法
/// </summary>
public static class KeyboardLockManager
{
    // 游戏启动时自动初始化（无需手动调用）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
#if UNITY_STANDALONE_WIN
        // 只在Windows平台运行
        CreateMonitorObject();
        Debug.Log("[KeyboardLockManager] 自动键盘锁定已启动");
#endif
    }

    // 创建隐藏的监控对象，全程后台运行
    private static void CreateMonitorObject()
    {
        GameObject monitorObj = new GameObject("KeyboardLockMonitor");
        monitorObj.hideFlags = HideFlags.HideAndDontSave; // 隐藏且不销毁
        monitorObj.AddComponent<KeyboardLockMonitor>();
        GameObject.DontDestroyOnLoad(monitorObj);
    }

    // 内部监控脚本（每帧检查）
    private class KeyboardLockMonitor : MonoBehaviour
    {
        // Windows API导入
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll")]
        private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

        // 常量定义
        private const int VK_CAPITAL = 0x14;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const string ENGLISH_KEYBOARD_LAYOUT = "00000409"; // 英文键盘布局ID
        private const uint KLF_ACTIVATE = 0x00000001;

        private IntPtr _englishLayout = IntPtr.Zero;

        void Awake()
        {
            // 初始化时先设置一次，并缓存英文布局句柄
            EnsureCapsLockOn();
            _englishLayout = LoadKeyboardLayout(ENGLISH_KEYBOARD_LAYOUT, KLF_ACTIVATE);
            EnsureEnglishInput();
        }

        void Update()
        {
            // 全程监控：每帧检查并强制保持
            EnsureCapsLockOn();
            EnsureEnglishInput();
        }

        // 强制保持大写锁定开启
        private void EnsureCapsLockOn()
        {
            if (!IsCapsLockOn())
            {
                ToggleCapsLock();
                Debug.Log("[KeyboardLockMonitor] 检测到大写关闭，已重新开启");
            }
        }

        // 强制保持英文输入法
        private void EnsureEnglishInput()
        {
            if (_englishLayout == IntPtr.Zero)
            {
                _englishLayout = LoadKeyboardLayout(ENGLISH_KEYBOARD_LAYOUT, KLF_ACTIVATE);
            }

            IntPtr currentLayout = GetKeyboardLayout(0);
            if (currentLayout != _englishLayout)
            {
                ActivateKeyboardLayout(_englishLayout, KLF_ACTIVATE);
                Debug.Log("[KeyboardLockMonitor] 检测到非英文输入法，已切换为英文");
            }
        }

        // 检查大写状态
        private bool IsCapsLockOn()
        {
            return (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
        }

        // 切换大写状态
        private void ToggleCapsLock()
        {
            keybd_event((byte)VK_CAPITAL, 0x45, 0, IntPtr.Zero);
            keybd_event((byte)VK_CAPITAL, 0x45, KEYEVENTF_KEYUP, IntPtr.Zero);
        }
    }
}