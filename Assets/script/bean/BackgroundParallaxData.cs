using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 视差背景数据模块
/// 职责：纯数据存储、配置定义、结构声明
/// </summary>
[System.Serializable]
public class BackgroundParallaxData
{
    #region 序列化配置（Inspector可编辑）
    [Header("=== 核心绑定 ===")]
    [Tooltip("背景根父物体，所有子层（top/mid/under）作为其子物体")]
    public GameObject backgroundRoot;
    
    [Tooltip("渲染背景的主相机")]
    public Camera usedCamera;

    [Header("=== 视差配置 ===")]
    [Tooltip("基础滚动速度，越上层速度越快")]
    public float baseScrollSpeed = 3f;

    [Header("=== 边界配置 ===")]
    [Tooltip("新背景生成触发的X轴边界")]
    public float spawnBorderX = 0f;
    
    [Tooltip("背景回收的X轴边界（超出后回收到对象池）")]
    public float destroyBorderX = -64.3f;
    #endregion

    #region 数据结构定义
    /// <summary>
    /// 单个背景块的单元数据
    /// </summary>
    public struct BackgroundBlock
    {
        public GameObject gameObject;
        public Transform transform;
        public Vector3 localScale;
        public bool hasSpawned;
    }

    /// <summary>
    /// 单个背景层的独立数据
    /// </summary>
    public class BackgroundLayer
    {
        public Transform layerTransform;
        public List<BackgroundBlock> blocks;
        public Queue<GameObject> pool;
        public float layerSpeed;
        public float backgroundWidth;

        public BackgroundLayer()
        {
            blocks = new List<BackgroundBlock>();
            pool = new Queue<GameObject>();
        }
    }
    #endregion

    #region 运行时数据缓存
    public List<BackgroundLayer> backgroundLayers = new List<BackgroundLayer>();
    public Transform rootTransform;
    public Transform cameraTransform;
    #endregion
}