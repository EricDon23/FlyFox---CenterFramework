using System;
using UnityEngine;

[Serializable]
public class Player
{
   [SerializeField]
   private int currentHealth = 1;//生命
   [SerializeField]
   private int maxHealth = 3;//最大生命
   [SerializeField]
   private float moveSpeed = 5f;//移动速度
   [SerializeField]
   private float dashSpeed = 10f;//冲刺速度
   [SerializeField]
   private float dashCooldownDuration = 2f;//冲刺冷却时间
   [SerializeField]
   private float dashCooldownTimer = 0f;//当前冲刺冷却计时器
   [SerializeField]
   private const float DASH_STOP_THRESHOLD = 0.1f;//冲刺停止阈值
   // ===================== 全新冲刺规则配置 =====================
    [Header("冲刺核心设置")]
    [SerializeField] private int maxDashCount = 3;           // 默认3次冲刺（核心修改）
    [SerializeField] private int usedDashCount = 0;         // 已用冲刺次数
    [SerializeField] private float dashInterval = 0.1f;    // 0.1秒防误触延迟（核心修改）
    [SerializeField] private float dashRecoverTime = 1f;    // 单次冲刺恢复时间（可自行调整）
    
    // 计时器
    private float dashIntervalTimer;    // 防误触间隔计时器
    private float dashRecoverTimer;     // 单次冲刺恢复计时器


   public int Health => currentHealth;
   public int MaxHealth => maxHealth;
   public float MoveSpeed => moveSpeed;
   public float DashSpeed => dashSpeed;
   public int MaxDashCount => maxDashCount;
   public int UsedDashCount => usedDashCount;
   public float DashCooldownDuration => dashCooldownDuration;
   public float DashCooldownTimer => dashCooldownTimer;
   public float DASHSTOPTHRESHOLD => DASH_STOP_THRESHOLD;
   public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownTimer);
   /// <summary>
    /// 是否可以冲刺：有剩余次数 + 防误触延迟结束
    /// </summary>
    public bool CanDash()
    {
        // 1. 剩余次数必须 > 0
        bool hasDash = usedDashCount < maxDashCount;
        // 2. 防误触延迟已结束
        bool canPress = dashIntervalTimer <= 0f;
        
        return hasDash && canPress;
    }

    /// <summary>
    /// 执行冲刺：消耗次数 + 重置防误触 + 重置恢复计时
    /// </summary>
    public bool TryStartDash()
    {
        if (!CanDash()) return false;

        // 1. 消耗1次冲刺
        usedDashCount++;
        // 2. 重置防误触计时器（0.1秒内无法再次冲刺）
        dashIntervalTimer = dashInterval;
        // 3. 重置恢复计时器（重新开始计时，实现“用一次重置一次”）
        if (usedDashCount > 0)
        {
            dashRecoverTimer = dashRecoverTime;
        }

        return true;
    }

    /// <summary>
    /// 每帧更新：处理防误触 + 依次恢复冲刺次数（修复核心BUG）
    /// </summary>
    public void UpdateTick(float deltaTime)
    {
        // 1. 更新防误触计时器
        if (dashIntervalTimer > 0f)
        {
            dashIntervalTimer -= deltaTime;
        }

        // 2. 更新恢复计时器（修复：每次恢复后自动重启，实现依次恢复）
        if (usedDashCount > 0)
        {
            // 计时未结束：继续倒计时
            if (dashRecoverTimer > 0f)
            {
                dashRecoverTimer -= deltaTime;
            }
            // 计时结束：恢复1次冲刺，然后重启计时器（如果还有剩余已用次数）
            else
            {
                usedDashCount--;
                // 如果还有已用次数，立刻重启计时器，准备恢复下一次
                if (usedDashCount > 0)
                {
                    dashRecoverTimer = dashRecoverTime;
                }
                else
                {
                    // 所有次数都恢复了，停止计时
                    dashRecoverTimer = 0f;
                }
            }
        }
    }

   // 修复：语法错误，补全Mathf.Max参数
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
   public void SetDashCooldown(float seconds) => dashCooldownDuration = Mathf.Max(0f, seconds);
   public void SetMaxDashCount(int count) => maxDashCount = Mathf.Max(0, count);
}