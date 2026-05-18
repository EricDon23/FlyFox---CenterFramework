using UnityEngine;

/// <summary>
/// 视差背景挂载模块
/// 职责：继承MonoBehaviour，挂载到物体，调用数据和行为模块
/// </summary>
public class BackgroundParallaxManager : MonoBehaviour
{
    [Header("=== 数据配置 ===")]
    public BackgroundParallaxData data = new BackgroundParallaxData();

    private BackgroundParallaxLogic _logic;

    
    void Start()
    {
        data.backgroundRoot = gameObject;
        _logic = new BackgroundParallaxLogic(data);
        _logic.InitializeComponents();
        _logic.InitializeAllLayers();
    }

    void FixedUpdate()
    {
        if (data.rootTransform == null || data.cameraTransform == null)
            return;

        _logic.UpdateCameraYFollow();
        _logic.UpdateAllLayersScroll();
    }

    void Reset()
    {
        data.usedCamera = Camera.main;
        data.spawnBorderX = 0f;
        data.destroyBorderX = -64.3f;
    }
    
}