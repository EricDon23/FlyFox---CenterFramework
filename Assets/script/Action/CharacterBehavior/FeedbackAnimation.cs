using UnityEngine;

// 挂在【item-feedback-1】物体上
public class FeedbackAnimation : MonoBehaviour
{
    // 动画结束后自动调用（和动画事件绑定）
    void OnAnimationFinish()
    {
        // 动画播完，直接隐藏物体
        gameObject.SetActive(false);
    }
}