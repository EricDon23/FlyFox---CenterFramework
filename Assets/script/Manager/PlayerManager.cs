using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] public Player player;
    private PlayerAction playerAction;
    private PolygonCollider2D _polygonCollider;
    private Rigidbody2D _rb2D;

    public PlayerAction PlayerAction => playerAction;

    

    void Start()
    {
        player = new Player();
        playerAction = new PlayerAction(player, gameObject);
        _polygonCollider = GetComponent<PolygonCollider2D>();
        _rb2D = GetComponent<Rigidbody2D>();

    }

    void FixedUpdate()
    {
        GlobalInputManager.UpdateInput();
        player.UpdateTick(Time.fixedDeltaTime);

        // ===================== 冲刺逻辑（带边界+无卡顿） =====================
        if (playerAction.isDashing && _rb2D != null)
        {
            // 统一物理移动：和普通移动兼容，无卡顿
            _rb2D.MovePosition(Vector2.MoveTowards(_rb2D.position, playerAction.currentDashTarget, player.DashSpeed* Time.fixedDeltaTime));
            
            // 到达目标：仅结束状态，不重置速度（避免卡顿）
            if (Vector2.Distance(_rb2D.position, playerAction.currentDashTarget) <= player.DASHSTOPTHRESHOLD)
            {
                playerAction.isDashing = false;
            }
            return;
        }

        // ===================== 原有普通移动逻辑（完全不变） =====================
        if (_polygonCollider == null)
        {
            playerAction.PlayerMove();
            playerAction.Dash(_polygonCollider);
            return;
        }

        Vector2 inputDir = new Vector2(GlobalInputManager.Horizontal, GlobalInputManager.Vertical).normalized;
        float moveDistance = player.MoveSpeed * Time.fixedDeltaTime;
        Vector3 moveDelta = inputDir * moveDistance;

        bool willOutBound = BaseUtils.WouldPolygonExceedBounds(_polygonCollider, moveDelta);
        
        if (!willOutBound)
        {
            playerAction.PlayerMove();
        }
        else
        {
            playerAction.StopMoveImmediately();
        }

        playerAction.Dash(_polygonCollider);
    }
    
}