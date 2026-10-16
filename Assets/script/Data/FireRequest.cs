using UnityEngine;

/// <summary>开火请求：封装子弹生成位置、方向、速度，作为单参数事件传递。</summary>
public struct FireRequest
{
    public Vector3 Position;
    public float DirectionX;
    public float Speed;

    public FireRequest(Vector3 pos, float dirX, float speed)
    {
        Position = pos;
        DirectionX = dirX;
        Speed = speed;
    }
}
