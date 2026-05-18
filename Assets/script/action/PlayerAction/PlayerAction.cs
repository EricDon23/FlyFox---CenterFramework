using UnityEngine;

public class PlayerAction
{
    private Player player;
    private GameObject gameObject;
    private Transform _playerTransform;
    private Rigidbody2D _rb2D;

    public bool isDashing = false;
    public Vector2 currentDashTarget;
    private const float INPUT_THRESHOLD = 0.0001f;

    public PlayerAction(Player player, GameObject gameObject)
    {
        this.player = player;
        this.gameObject = gameObject;
        _playerTransform = gameObject.transform;
        _rb2D = gameObject.GetComponent<Rigidbody2D>();
    }

    // 普通移动（完全保留原有逻辑）
   public void PlayerMove()
   {
        Vector2 inputDir = new Vector2(GlobalInputManager.Horizontal, GlobalInputManager.Vertical);

        if (inputDir.sqrMagnitude < INPUT_THRESHOLD)
        {
            if (_rb2D != null) _rb2D.velocity = Vector2.zero;
            return;
        }

        inputDir.Normalize();
        float moveDistance = player.MoveSpeed * Time.fixedDeltaTime;

        if (_rb2D != null)
        {
            Vector2 targetPos = _rb2D.position + inputDir * moveDistance;
            _rb2D.MovePosition(targetPos);
        }
        else
        {
            _playerTransform.Translate((Vector3)inputDir * moveDistance);
        }
    }

    // 立即停止（完全保留）
    public void StopMoveImmediately()
    {
        if (_rb2D != null)
        {
            _rb2D.velocity = Vector2.zero;
            _rb2D.angularVelocity = 0;
        }
    }

    // 冲刺函数（完全保留原有逻辑，仅清理无用代码）
    // 合并冲刺逻辑：去除冗余，一体化实现 输入检测+目标计算+边界校验+冲刺启动
    public void Dash(PolygonCollider2D polygonCollider = null)
    {
        // 1. 基础拦截：冲刺中 / 无冲刺权限 / 未按冲刺键 → 直接退出
        if (isDashing || !player.CanDash() || !GlobalInputManager.IsLeftShift)
            return;

        // 2. 获取输入方向并校验（无效输入直接退出）
        Vector2 inputDir = new Vector2(GlobalInputManager.Horizontal, GlobalInputManager.Vertical).normalized;
        const float INPUT_THRESHOLD = 0.1f;
        if (inputDir.sqrMagnitude < INPUT_THRESHOLD)
            return;

        // 3. 计算冲刺目标点
        const float dashDistance = 2f;
        Vector2 dashTarget = (Vector2)_playerTransform.position + inputDir * dashDistance;

        // 4. 边界校验：冲刺会越界则拦截
        if (polygonCollider != null)
        {
            Vector3 dashDelta = dashTarget - (Vector2)gameObject.transform.position;
            if (BaseUtils.WouldPolygonExceedBounds(polygonCollider, dashDelta))
            {
                Debug.Log("冲刺越界，已拦截！");
                return;
            }
        }

        // 5. 所有校验通过 → 启动冲刺
        player.TryStartDash();
        currentDashTarget = dashTarget;
        isDashing = true;
    }
     public void feedbackAnimator(GameObject gameObject)
    {
        // 1. 缓存子物体
        Transform feedbackTrans = gameObject.transform.Find("item-feedback-1");
        if (feedbackTrans == null)
        {
            Debug.LogWarning("未找到反馈动画物体：item-feedback-1");
            return;
        }

        // 2. 激活物体
        feedbackTrans.gameObject.SetActive(true);

        // 3. 获取并播放 Animator 动画（只用Animator，不混用旧组件）
        Animator animator = feedbackTrans.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("反馈物体上未挂载Animator组件！");
            feedbackTrans.gameObject.SetActive(false);
            return;
        }
        // 4. 强制初始化Animator（解决SetActive后未加载完成的问题）
        animator.Rebind();       // 重置Animator，强制加载所有状态
        animator.Update(0);      // 立即更新一帧，完成初始化

        // 5. 安全校验状态是否存在（避免字符串拼写错误）
        int stateHash = Animator.StringToHash("Item-feedback-Animation");
        if (animator.HasState(0, stateHash)) // 检查默认层（索引0）是否有该状态
        {
            // 从第0帧开始播放，避免动画从中间开始
            animator.Play(stateHash, 0, 0f);
        }
        else
        {
            Debug.LogError($"Animator中未找到状态：Item-feedback-Animation，请检查状态名/Controller！");
            feedbackTrans.gameObject.SetActive(false);
        }

        
    }
}