public static class Global
{
    // 保留所有原字段和属性名
    private static float _globalGameDuration;
    private static float _startTime;
    private static int _sessionID;
    private static int level;
    private static float _sessionStartTime;
    private static float _sessionDuration;
    private static float _sessionPausedTime;
    private static float _totalPausedDuration;
    private static int _sessionBaseScore;
    private static float _sessionTimeBonus;
    private static int _finalSessionScore;
    private static GameEnum _currentState = GameEnum.Menu;

    // 保留所有原属性的读写权限（完全兼容原有调用）
    public static float GameDuration
    {
        set { _globalGameDuration = value; }
        get { return _globalGameDuration; }
    }
    public static float StartTime
    {
        set { _startTime = value; }
        get { return _startTime; }
    }
    public static int SessionID
    {
        set { _sessionID = value; }
        get { return _sessionID; }
    }
    public static int Level
    {
        set { level = value; }
        get { return level; }
    }
    public static float SessionStartTime
    {
        set { _sessionStartTime = value; }
        get { return _sessionStartTime; }
    }
    public static float SessionPausedTime
    {
        set { _sessionPausedTime = value; }
        get { return _sessionPausedTime; }
    }
    public static float SessionDuration
    {
        set { _sessionDuration = value; }
        get { return _sessionDuration; }
    }
    public static float TotalPausedDuration
    {
        set { _totalPausedDuration = value; }
        get { return _totalPausedDuration; }
    }
    public static int SessionBaseScore
    {
        set { _sessionBaseScore = value; }
        get { return _sessionBaseScore; }
    }
    public static float SessionTimeBonus
    {
        set { _sessionTimeBonus = value; }
        get { return _sessionTimeBonus; }
    }
    public static int FinalSessionScore
    {
        set { _finalSessionScore = value; }
        get { return _finalSessionScore; }
    }
    public static GameEnum CurrentState
    {
        set { _currentState = value; }
        get { return _currentState; }
    }
}