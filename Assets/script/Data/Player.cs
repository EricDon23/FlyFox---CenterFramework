using System;
using UnityEngine;

/// <summary>
/// 玩家数据类：只存储最基础的数据 + 得失函数（get/set、扣血/加血等）。
/// 冲刺状态机（CanDash / TryStartDash / UpdateTick）已迁至 PlayerAction 逻辑层。
/// </summary>
[Serializable]
[AutoRegisterConfig("Player")]
public class Player : BasicDate
{
   [SerializeField]
   private int currentHealth = 3;//生命
   [SerializeField]
   private int maxHealth = 3;//最大生命
   [SerializeField]
   private float moveSpeed = 5f;//移动速度
   [SerializeField]
   private float dashSpeed = 10f;//冲刺速度
   [SerializeField]
   private const float DASH_STOP_THRESHOLD = 0.1f;//冲刺停止阈值
   // ===================== 全新冲刺规则配置 =====================
    [Header("冲刺核心设置")]
    [SerializeField] private int maxDashCount = 3;           // 默认3次冲刺（核心修改）
    [SerializeField] private float dashInterval = 0.1f;    // 0.1秒防误触延迟（核心修改）
    [SerializeField] private float dashRecoverTime = 1f;    // 单次冲刺恢复时间（可自行调整）

   // —— 只读属性（得失：get） ——
   public int Health => currentHealth;
   public int MaxHealth => maxHealth;
   public float MoveSpeed => moveSpeed;
   public float DashSpeed => dashSpeed;
   public int MaxDashCount => maxDashCount;
   public float DashInterval => dashInterval;
   public float DashRecoverTime => dashRecoverTime;
   public float DASHSTOPTHRESHOLD => DASH_STOP_THRESHOLD;

   // —— 得失函数（扣血/加血/设置） ——
   public void ApplyDamage(int amount)
   {
      currentHealth = Mathf.Max(currentHealth - amount, 0);
   }

   public void Heal(int amount)
   {
      currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
   }

   public void SetMaxHealth(int h) => maxHealth = Mathf.Max(1, h);
   public void SetMoveSpeed(float s) => moveSpeed = s;
   public void SetDashSpeed(float s) => dashSpeed = s;
   public void SetMaxDashCount(int count) => maxDashCount = Mathf.Max(0, count);
}