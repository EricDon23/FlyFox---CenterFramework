using UnityEngine;

/// <summary>
/// 全局输入管理器（静态单例）
/// 作用：统一采集所有玩家输入，提供全局只读访问入口
/// 调用要求：必须在 Update 中执行 <see cref="UpdateInput"/>
/// </summary>
public static class GlobalInputManager
{
    private const string HORIZONTAL_AXIS = "Horizontal";
    private const string VERTICAL_AXIS = "Vertical";
    
    private static float _horizontal;
    private static float _vertical;
    private static bool _isLeftShift;
    private static bool _isQ;
    private static bool _isESC;
    private static bool _isMouseLeft;
    private static bool _isMouseRight;

    public static float Horizontal => _horizontal;
    public static float Vertical => _vertical;
    public static bool IsLeftShift => _isLeftShift;
    public static bool IsQ => _isQ;
    public static bool IsESC => _isESC;
    public static bool IsMouseLeft => _isMouseLeft;
    public static bool IsMouseRight => _isMouseRight;

    /// <summary>
    /// 全局输入更新（必须在 MonoBehaviour.Update 中调用）
    /// </summary>
    public static void UpdateInput()
    {
        // ✅ 修复：GetAxis → GetAxisRaw 无平滑、无惯性，松开立即=0
        _horizontal = Input.GetAxisRaw(HORIZONTAL_AXIS);
        _vertical = Input.GetAxisRaw(VERTICAL_AXIS);

        _isLeftShift = Input.GetKey(KeyCode.LeftShift);
        _isQ = Input.GetKey(KeyCode.Q);
        _isESC = Input.GetKey(KeyCode.Escape);
        _isMouseLeft = Input.GetMouseButton(0);
        _isMouseRight = Input.GetMouseButton(1);
    }

    static GlobalInputManager()
    {
        _horizontal = 0f;
        _vertical = 0f;
    }
}