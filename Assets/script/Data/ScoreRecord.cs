using System;

/// <summary>单条分数记录：用于 JSON 配置表存储。</summary>
[Serializable]
[AutoRegisterConfig("scores")]
public class ScoreRecord : BasicDate
{
    public int score;
    public float duration;
    public string time;
    public string date;

    public ScoreRecord() { }

    public ScoreRecord(int s, float d, string t, string dt)
    {
        score = s;
        duration = d;
        time = t;
        date = dt;
        name = dt;  // 用日期做唯一 key
    }
}
