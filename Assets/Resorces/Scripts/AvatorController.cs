using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatorController : MonoBehaviour
{
    public Animator animator;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    /// <summary>
    /// "Idle"=0, "Joy"=1, "Anger"=2, "Sadnes"=3, "Pleasure"=4
    /// </summary>
    /// <param name="AnimationState"></param>
    public void ChangeAnimation(string AnimationState)
    {
        int AvatorState = 0;
        switch (AnimationState)
        {
            case "Joy":
                AvatorState = 1;
                break;
            case "Anger":
                AvatorState = 2;
                break;
            case "Sadnes":
                AvatorState = 3;
                break;
            case "Pleasure":
                AvatorState = 4;
                break;
            default:
                break;
        }

        // アニメーションを一度再生したら、Idle状態に戻す
        // アニメーションの再生時間を取得
        animator.SetInteger("AnimationState", AvatorState);
        float animationTime = animator.GetCurrentAnimatorStateInfo(0).length;
        StartCoroutine(ResetToIdle(animationTime));        
    }


    private IEnumerator ResetToIdle(float animationTime)
    {
        yield return new WaitForSeconds(animationTime);
        animator.SetInteger("AnimationState", 0); // Idle に戻る
    }

}
