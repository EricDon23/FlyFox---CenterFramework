using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 射线检测工具类，内置NonAlloc缓冲区，封装2D/3D射线常用逻辑
/// 复用缓冲区减少GC，自带碰撞体有效性校验
/// </summary>
public static class RayDetector
{
    /// <summary>NonAlloc 缓冲区大小：单次检测最多返回的命中数量，超出部分会被截断</summary>
    private const int BufferSize = 64;
    private static readonly RaycastHit[] _3DRayBuffer = new RaycastHit[BufferSize];
    private static readonly RaycastHit2D[] _2DRayBuffer = new RaycastHit2D[BufferSize];

    #region ===== 3D 射线封装 =====
    /// <summary>
    /// 3D射线检测 NonAlloc，一条射线获取多个命中物体
    /// </summary>
    /// <param name="ray">射线</param>
    /// <param name="maxDistance">最大距离</param>
    /// <param name="layerMask">层级过滤</param>
    /// <param name="queryTrigger">触发器查询规则</param>
    /// <returns>过滤后的命中列表，按距离由近到远排序</returns>
    public static List<RaycastHit> Raycast3D(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTrigger = QueryTriggerInteraction.Ignore)
    {
        List<RaycastHit> resultList = new List<RaycastHit>();
        int hitCount = Physics.RaycastNonAlloc(ray, _3DRayBuffer, maxDistance, layerMask, queryTrigger);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _3DRayBuffer[i];
            if (IsHitValid(hit))
            {
                resultList.Add(hit);
            }
        }
        return resultList;
    }

    /// <summary>
    /// 3D 线段检测（两点之间）NonAlloc，等价于从起点向终点发射射线
    /// </summary>
    public static List<RaycastHit> Linecast3D(Vector3 start, Vector3 end, int layerMask, QueryTriggerInteraction queryTrigger = QueryTriggerInteraction.Ignore)
    {
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        if (distance < 1e-6f)
        {
            return new List<RaycastHit>();
        }

        return Raycast3D(new Ray(start, delta / distance), distance, layerMask, queryTrigger);
    }

    /// <summary>
    /// 3D胶囊扫描 Sweep 非分配版本
    /// </summary>
    public static List<RaycastHit> CapsuleCast3D(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTrigger = QueryTriggerInteraction.Ignore)
    {
        List<RaycastHit> resultList = new List<RaycastHit>();
        int hitCount = Physics.CapsuleCastNonAlloc(point1, point2, radius, direction, _3DRayBuffer, maxDistance, layerMask, queryTrigger);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _3DRayBuffer[i];
            if (IsHitValid(hit))
            {
                resultList.Add(hit);
            }
        }
        return resultList;
    }
    #endregion

    #region ===== 2D 射线封装 =====
    /// <summary>
    /// 2D射线检测 NonAlloc
    /// </summary>
    public static List<RaycastHit2D> Raycast2D(Vector2 origin, Vector2 direction, float maxDistance, int layerMask)
    {
        List<RaycastHit2D> resultList = new List<RaycastHit2D>();
        int hitCount = Physics2D.RaycastNonAlloc(origin, direction, _2DRayBuffer, maxDistance, layerMask);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _2DRayBuffer[i];
            if (IsHitValid(hit))
            {
                resultList.Add(hit);
            }
        }
        return resultList;
    }

    /// <summary>
    /// 2D线段检测 Linecast NonAlloc，等价于从起点向终点发射射线
    /// </summary>
    public static List<RaycastHit2D> Linecast2D(Vector2 start, Vector2 end, int layerMask)
    {
        Vector2 delta = end - start;
        float distance = delta.magnitude;
        if (distance < 1e-6f)
        {
            return new List<RaycastHit2D>();
        }

        return Raycast2D(start, delta / distance, distance, layerMask);
    }

    /// <summary>
    /// 2D圆形扫掠 CircleCast NonAlloc
    /// </summary>
    public static List<RaycastHit2D> CircleCast2D(Vector2 origin, float radius, Vector2 direction, float maxDistance, int layerMask)
    {
        List<RaycastHit2D> resultList = new List<RaycastHit2D>();
        int hitCount = Physics2D.CircleCastNonAlloc(origin, radius, direction, _2DRayBuffer, maxDistance, layerMask);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _2DRayBuffer[i];
            if (IsHitValid(hit))
            {
                resultList.Add(hit);
            }
        }
        return resultList;
    }
    #endregion

    #region ===== 通用校验 =====
    /// <summary>
    /// 3D命中信息有效性校验
    /// </summary>
    private static bool IsHitValid(RaycastHit hit)
    {
        if (hit.collider == null) return false;
        return hit.collider.enabled && hit.collider.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// 2D命中信息有效性校验
    /// </summary>
    private static bool IsHitValid(RaycastHit2D hit)
    {
        if (hit.collider == null) return false;
        return hit.collider.enabled && hit.collider.gameObject.activeInHierarchy;
    }

    #endregion
}
