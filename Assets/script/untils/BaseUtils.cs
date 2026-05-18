using UnityEngine;

/// <summary>
/// 静态通用工具类
/// </summary>
public static class BaseUtils 
{
    /// <summary>
    /// 预判多边形碰撞体物体移动后是否超出主相机屏幕
    /// </summary>
    /// <param name="polygon">目标多边形碰撞体</param>
    /// <param name="movement">位移向量（无位移传 Vector3.zero）</param>
    /// <returns>true=越界, false=安全</returns>
    public static bool WouldPolygonExceedBounds(PolygonCollider2D polygon, Vector3 movement) 
    {

        // 空值校验
        if (polygon == null || Camera.main == null)
            return false;

        Vector2[] localPoints = polygon.points;
        Transform targetTransform = polygon.transform;
        Camera mainCam = Camera.main;

        // 遍历所有顶点检测
        foreach (Vector2 localPoint in localPoints)
        {
            Vector3 worldPos = targetTransform.TransformPoint(localPoint) + movement;
            Vector3 viewportPos = mainCam.WorldToViewportPoint(worldPos);

            // 任意顶点超出屏幕，返回true
            if (viewportPos.x < 0 || viewportPos.x > 1 || 
                viewportPos.y < 0 || viewportPos.y > 1)
            {
                Debug.Log("超出屏幕：" + worldPos);
                return true;
            }
        }
        return false;
    }
     // ===================== 工具方法 =====================
    public static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
}