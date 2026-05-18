using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【挂载在父物体上】关卡构建管理器
/// 负责：初始化数据、调用 LevelBuild 完成所有流程
/// </summary>
public class LevelBuildManager : MonoBehaviour
{
    [Header("=== 网格数据 ===")]
    public SpawnPositionData spawnData;

    [Header("=== 玩家血量（用于生成 berry） ===")]
    public float playerCurrentHp = 100f;
    public float playerMaxHp = 100f;

    [Header("=== 预制体映射（绑定对应物品） ===")]
    public GameObject feedback;
    private LevelBuild _levelBuild;

    void Start() {
        

        // 1. 初始化网格
        spawnData.InitializeGrid(spawnData.gridWidth, spawnData.gridHeight);
        // 2. 构建预制体映射表
        Dictionary<GridType, GameObject> prefabMap = new Dictionary<GridType, GameObject>
        {
            { GridType.cherry,  feedback},
            { GridType.Coin,  feedback },
            { GridType.gem,  feedback }
        };

        // 3. 初始化关卡构建器
        _levelBuild = new LevelBuild(spawnData, prefabMap);

        // 4. 填充数据
        _levelBuild.SetGridItemType(playerCurrentHp, playerMaxHp);

        // 5. 绑定预制体
        _levelBuild.SetGridPrefab();

        // 6. 构建场景（生成到当前脚本所在物体的子级）
        _levelBuild.BuildLevel(transform);
    }
}