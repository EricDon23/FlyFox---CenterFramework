using UnityEngine;
/// <summary>
/// 游戏常用数学工具，同时支持2D Vector2、3D Vector3
/// 【原则：原生已有API不重复封装，只实现业务组合/原生缺失的计算】
/// 【注意】3D默认正面=forward轴(Z轴)；2D默认正面=right轴(X轴)，可自行更换参考轴
/// </summary>
public static class MathOfGame
{
    #region 距离平方
    /// <summary>3D 距离平方，高性能，用于范围比较，不开根号</summary>
    public static float DistanceSqr(Vector3 a, Vector3 b)
    {
        Vector3 delta = a - b;
        return delta.sqrMagnitude;
    }
    /// <summary>2D 距离平方，高性能，用于范围比较</summary>
    public static float DistanceSqr(Vector2 a, Vector2 b)
    {
        Vector2 delta = a - b;
        return delta.sqrMagnitude;
    }
    #endregion

    #region 2D 叉积
    /// <summary>
    /// 2D叉积，返回标量
    /// >0 目标在向量左侧，<0在右侧，=0共线
    /// </summary>
    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
    #endregion

    #region ===== 夹角计算 0~180° =====
    /// <summary>
    /// 3D：计算两个向量的夹角（0~180°）
    /// 建议向量提前归一化
    /// </summary>
    public static float GetAngle(Vector3 a, Vector3 b)
    {
        float dot = Vector3.Dot(a, b);
        dot = Mathf.Clamp(dot, -1f, 1f);
        return Mathf.Acos(dot) * Mathf.Rad2Deg;
    }
    /// <summary>
    /// 2D：计算两个向量的夹角（0~180°）
    /// </summary>
    public static float GetAngle(Vector2 a, Vector2 b)
    {
        float dot = Vector2.Dot(a, b);
        dot = Mathf.Clamp(dot, -1f, 1f);
        return Mathf.Acos(dot) * Mathf.Rad2Deg;
    }

    #endregion

    #region ===== 3D 方位判断 =====
    #region 底层向量版（传入方向向量+位移向量，高性能批量用）
    /// <summary>
    /// 3D底层：前后判断，返回点积值
    /// >0 前方；<0 后方；=0 垂直
    /// </summary>
    public static float GetForwardSign(Vector3 aForward, Vector3 dirToB)
    {
        return Vector3.Dot(aForward, dirToB);
    }
    /// <summary>
    /// 3D底层：左右判断，返回点积值
    /// >0 右侧；<0 左侧；=0 正前/正后
    /// </summary>
    public static float GetHorizontalSideSign(Vector3 aRight, Vector3 dirToB)
    {
        return Vector3.Dot(aRight, dirToB);
    }
    /// <summary>
    /// 3D底层：上下判断，返回点积值
    /// >0 上方；<0 下方；=0 平齐
    /// </summary>
    public static float GetVerticalSign(Vector3 aUp, Vector3 dirToB)
    {
        return Vector3.Dot(aUp, dirToB);
    }
    #endregion

    #region Transform快捷重载（传入两个Transform，直接出结果，业务用）
    /// <summary>
    /// 3D：判断B是否在A的正面一侧（forward轴正向）
    /// </summary>
    public static bool GetForwardSign(Transform a, Transform b)
    {
        Vector3 dir = b.position - a.position;
        return GetForwardSign(a.forward, dir) > 0f;
    }
    /// <summary>
    /// 3D：判断B是否在A的右侧一侧（right轴正向）
    /// </summary>
    public static bool GetHorizontalSideSign(Transform a, Transform b)
    {
        Vector3 dir = b.position - a.position;
        return GetHorizontalSideSign(a.right, dir) > 0f;
    }
    /// <summary>
    /// 3D：判断B是否在A的上方一侧（up轴正向）
    /// </summary>
    public static bool GetVerticalSign(Transform a, Transform b)
    {
        Vector3 dir = b.position - a.position;
        return GetVerticalSign(a.up, dir) > 0f;
    }
    #endregion

    #region 3D综合信息计算
    /// <summary>
    /// 3D综合计算目标相对主体的位置信息
    /// </summary>
    /// <param name="aPos">主体世界坐标</param>
    /// <param name="bPos">目标世界坐标</param>
    /// <param name="aForward">主体前向</param>
    /// <param name="aUp">主体向上</param>
    /// <param name="distance">输出直线距离</param>
    /// <param name="angleYaw">输出水平偏航角（0~180°）</param>
    /// <param name="anglePitch">输出俯仰角（0~180°）</param>
    /// <returns>归一化的指向向量</returns>
    public static Vector3 CalcRelativeInfo3D(Vector3 aPos, Vector3 bPos, Vector3 aForward, Vector3 aUp,
        out float distance, out float angleYaw, out float anglePitch)
    {
        Vector3 delta = bPos - aPos;
        distance = delta.magnitude;
        Vector3 dirToB = Normalize(delta);
        
        // 水平投影，计算偏航角
        Vector3 dirHorizontal = Normalize(Vector3.ProjectOnPlane(dirToB, aUp));
        angleYaw = GetAngle(aForward, dirHorizontal);
        // 俯仰角
        anglePitch = GetAngle(dirToB, dirHorizontal);
        
        return dirToB;
    }
    #endregion
    #endregion

    #region ===== 2D 方位判断 =====
    #region 底层向量版
    /// <summary>
    /// 2D底层：前后判断，返回点积值
    /// >0 前方；<0 后方
    /// </summary>
    public static float GetForwardSign(Vector2 aForward, Vector2 dirToB)
    {
        return Vector2.Dot(aForward, dirToB);
    }
    /// <summary>
    /// 2D底层：上下判断，返回点积值
    /// >0 上方；<0 下方
    /// </summary>
    public static float GetVerticalSign(Vector2 aUp, Vector2 dirToB)
    {
        return Vector2.Dot(aUp, dirToB);
    }
    #endregion

    #region Transform快捷重载（2D专用，默认正面=right轴）
    /// <summary>
    /// 2D：判断B是否在A的正面一侧（默认以right轴为正面，适配大多数2D项目）
    /// </summary>
    public static bool GetForwardSign2D(Transform a, Transform b)
    {
        Vector2 dir = (Vector2)(b.position - a.position);
        return GetForwardSign((Vector2)a.right, dir) > 0f;
    }
    /// <summary>
    /// 2D：判断B是否在A的上方一侧（up轴）
    /// </summary>
    public static bool GetVerticalSign2D(Transform a, Transform b)
    {
        Vector2 dir = (Vector2)(b.position - a.position);
        return GetVerticalSign((Vector2)a.up, dir) > 0f;
    }
    #endregion

    #region 2D综合信息计算
    /// <summary>
    /// 2D综合计算目标相对主体位置信息
    /// </summary>
    public static Vector2 CalcRelativeInfo2D(Vector2 aPos, Vector2 bPos, Vector2 aForward,
        out float distance, out float angle)
    {
        Vector2 delta = bPos - aPos;
        distance = delta.magnitude;
        Vector2 dirToB = Normalize(delta);
        angle = GetAngle(aForward, dirToB);
        return dirToB;
    }
    #endregion
    #endregion

    #region ===== 向量基础工具 =====
    /// <summary>安全归一化，零向量保护</summary>
    public static Vector3 Normalize(Vector3 v)
    {
        if (v.sqrMagnitude < 1e-6f) return Vector3.zero;
        return v.normalized;
    }
    /// <summary>安全归一化 2D</summary>
    public static Vector2 Normalize(Vector2 v)
    {
        if (v.sqrMagnitude < 1e-6f) return Vector2.zero;
        return v.normalized;
    }
    /// <summary>2D向量投影：b在a上的投影向量（原生Vector2无Project）</summary>
    public static Vector2 Project(Vector2 a, Vector2 b)
    {
        float dot = Vector2.Dot(b, a);
        float magSqr = a.sqrMagnitude;
        if (magSqr < 1e-6f) return Vector2.zero;
        return a * (dot / magSqr);
    }
    #endregion

    #region ===== 带符号夹角 -180~180° =====
    /// <summary>
    /// 2D带符号夹角，范围[-180, 180]
    /// 正数：逆时针；负数：顺时针
    /// </summary>
    public static float GetSignedAngle(Vector2 a, Vector2 b)
    {
        float angle = GetAngle(a, b);
        float crossVal = Cross(a, b);
        return crossVal < 0 ? -angle : angle;
    }
    #endregion

    #region ===== 角度循环工具 =====
    /// <summary>将角度限制在 [0, 360)</summary>
    public static float RepeatAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }
    #endregion
}
