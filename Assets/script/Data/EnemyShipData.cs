using System;

/// <summary>敌机数据：从右侧生成，向左飞行，到达左侧销毁；被击毁得分。</summary>
[Serializable]
[AutoRegisterConfig("EnemyShip")]
public class EnemyShipData : BasicDate
{
    public int maxHealth = 1;
    public int currentHealth = 1;
    public float moveSpeed = 3f;
    public int scoreValue = 100;
    public float fireInterval = 2f;
    public float leftKillX = -10f;
}
