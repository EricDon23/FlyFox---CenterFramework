using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 主动物理范围检测器，内置NonAlloc缓冲区，封装重复的范围检测逻辑
/// 支持 2D / 3D Overlap系列检测，内置命中过滤、对象有效性校验
/// </summary>
public static class ActiveCollisionDetector
{
    /// <summary>NonAlloc 缓冲区大小：单次检测最多返回的碰撞体数量，超出部分会被截断</summary>
    private const int BufferSize = 64;
    private static readonly Collider[] _3DCollisionBuffer = new Collider[BufferSize];
    private static readonly Collider2D[] _2DCollisionBuffer = new Collider2D[BufferSize];

    #region ===== 3D 封装方法 =====
    /// <summary>
    /// 3D球形范围检测 NonAlloc
    /// </summary>
    /// <param name="center">世界中心点</param>
    /// <param name="radius">球半径</param>
    /// <param name="layerMask">层级筛选</param>
    /// <param name="triggerInteraction">触发器处理规则</param>
    /// <returns>过滤后的有效碰撞体列表（本次检测快照，不会被下一次检测覆盖）</returns>
    public static List<Collider> OverlapSphere3D(Vector3 center, float radius, int layerMask, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
    {
        List<Collider> resultList = new List<Collider>();

        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, _3DCollisionBuffer, layerMask, triggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _3DCollisionBuffer[i];
            if (IsColliderValid(col))
            {
                resultList.Add(col);
            }
        }
        return resultList;
    }

    /// <summary>
    /// 3D盒体范围检测 NonAlloc
    /// </summary>
    public static List<Collider> OverlapBox3D(Vector3 center, Vector3 halfExtents, Quaternion rot, int layerMask, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
    {
        List<Collider> resultList = new List<Collider>();
        int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, _3DCollisionBuffer, rot, layerMask, triggerInteraction);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _3DCollisionBuffer[i];
            if (IsColliderValid(col))
            {
                resultList.Add(col);
            }
        }
        return resultList;
    }

    /// <summary>
    /// 3D胶囊体范围检测 NonAlloc
    /// </summary>
    public static List<Collider> OverlapCapsule3D(Vector3 point0, Vector3 point1, float radius, int layerMask, QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore)
    {
        List<Collider> resultList = new List<Collider>();
        int hitCount = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, _3DCollisionBuffer, layerMask, triggerInteraction);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _3DCollisionBuffer[i];
            if (IsColliderValid(col))
            {
                resultList.Add(col);
            }
        }
        return resultList;
    }
    #endregion

    #region ===== 2D 封装方法 =====
    /// <summary>
    /// 2D圆形范围检测 NonAlloc
    /// </summary>
    public static List<Collider2D> OverlapCircle2D(Vector2 center, float radius, int layerMask)
    {
        List<Collider2D> resultList = new List<Collider2D>();
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, radius, _2DCollisionBuffer, layerMask);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _2DCollisionBuffer[i];
            if (IsColliderValid(col))
            {
                resultList.Add(col);
            }
        }
        return resultList;
    }

    /// <summary>
    /// 2D盒形范围检测 NonAlloc
    /// </summary>
    public static List<Collider2D> OverlapBox2D(Vector2 center, Vector2 size, float angle, int layerMask)
    {
        List<Collider2D> resultList = new List<Collider2D>();
        int hitCount = Physics2D.OverlapBoxNonAlloc(center, size, angle, _2DCollisionBuffer, layerMask);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _2DCollisionBuffer[i];
            if (IsColliderValid(col))
            {
                resultList.Add(col);
            }
        }
        return resultList;
    }
    /// <summary>
    /// 使用已有2D碰撞体自身形状执行重叠检测（无GC，等价OverlapCollider NonAlloc）
    /// </summary>
    /// <param name="sourceCol">作为检测范围的碰撞体实例</param>
    /// <param name="contactFilter">过滤规则（layer、触发器）</param>
    /// <returns>过滤后的碰撞体列表</returns>
    public static List<Collider2D> OverlapCollider2D(Collider2D sourceCol, ContactFilter2D contactFilter)
    {
        List<Collider2D> resultList = new List<Collider2D>();
        if (sourceCol == null)
        {
            Debug.LogWarning("[ActiveCollisionDetector] OverlapCollider2D 源碰撞体为空");
            return resultList;
        }
        int hitCount = sourceCol.OverlapCollider(contactFilter, _2DCollisionBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _2DCollisionBuffer[i];
            if (IsColliderValid(col))
            {
                resultList.Add(col);
            }
        }
        return resultList;
    }

    #endregion

    #region ===== 通用校验工具 =====
    /// <summary>
    /// 校验3D碰撞体有效：物体存活、碰撞体启用
    /// </summary>
    private static bool IsColliderValid(Collider col)
    {
        return col != null && col.enabled && col.gameObject.activeInHierarchy;
    }
    /// <summary>
    /// 校验2D碰撞体有效
    /// </summary>
    private static bool IsColliderValid(Collider2D col)
    {
        return col != null && col.enabled && col.gameObject.activeInHierarchy;
    }
    #endregion
}
