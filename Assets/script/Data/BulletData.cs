using System;

/// <summary>子弹数据：直线飞行，出界销毁；由发射方决定方向。</summary>
[Serializable]
[AutoRegisterConfig("Bullet")]
public class BulletData : BasicDate
{
    public float speed = 10f;
    public int damage = 1;
    public float directionX = 1f; // 1=向右（玩家弹），-1=向左（敌机弹）
    public float killX = 12f;    // 右边界
    public float killLeftX = -12f;
}
