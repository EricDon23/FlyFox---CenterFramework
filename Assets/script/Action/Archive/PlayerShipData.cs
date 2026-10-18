using System;

/// <summary>玩家飞机数据：横版飞行射击，玩家在左侧区域上下左右移动并自动射击。</summary>
[Serializable]
[AutoRegisterConfig("PlayerShip")]
public class PlayerShipData : BasicDate
{
    public int maxHealth = 3;
    public int currentHealth = 3;
    public float moveSpeed = 6f;
    public float fireInterval = 0.25f;
    public float bulletSpeed = 12f;
    public float leftBound = -8f;
    public float rightBound = 2f;
    public float topBound = 4f;
    public float bottomBound = -4f;

    public void ApplyDamage(int amount)
    {
        currentHealth = Math.Max(0, currentHealth - amount);
    }
}
