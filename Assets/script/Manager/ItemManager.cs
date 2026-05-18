using Unity.VisualScripting;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public ScoreAction _scoreAction;
    [SerializeField] private Fruit _scorefruit;

    
   
   void Start()
    {
        _scoreAction = new ScoreAction(_scorefruit,_scorefruit.Heal);
        // 初始化分数逻辑
        _scoreAction.DevelopScore();
        Debug.Log($"[ItemManager] 当前等级: {Global.Level}");

        // 激活子物体
        if (_scorefruit.Type != GridType.Empty)
        {
            Transform sonTrans = transform.Find(_scorefruit.Type.ToString());
            if (sonTrans != null) sonTrans.gameObject.SetActive(true);
        }
        
    
        // 安全校验
        if (_scorefruit == null)
        {
            Debug.LogError("[ItemManager] 未配置Score数据！", this);
            return;
        }

        Debug.Log($"[ItemManager] 基础分：{_scorefruit.BaseScore} | 当前分：{_scorefruit.CurrentScore}");

        // 自动配置碰撞体
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null) collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 基础判断
        if (!other.CompareTag("Player") || _scoreAction == null) return;

        PlayerManager playerManager = other.GetComponent<PlayerManager>();
        if (playerManager == null || playerManager.player == null) return;

        Debug.Log($"[ItemManager] 玩家触碰物品");

        // 核心逻辑：执行得分/治疗判断
        bool addScore = _scoreAction.GetCurrentScore(playerManager.player);
        // 非加分 = 治疗玩家
        if (!addScore)
        {
            int heal = _scoreAction.GetHealAmount();
            playerManager.player.Heal(heal);
            Debug.Log($"[ItemManager] 恢复血量：{heal}");
        }

        // 播放动画 + 销毁物品
        playerManager.PlayerAction.feedbackAnimator(playerManager.gameObject);
        Destroy(gameObject);
    }
    public void SetFruit(GridType type)
    {
        _scorefruit.Type = type;
        
    }
}