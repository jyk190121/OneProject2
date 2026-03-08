using UnityEngine;

public class AnimController
{
    Animator anim;

    //readonly int hashMove = Animator.StringToHash("IsMove");
    readonly int hashAttack = Animator.StringToHash("Attack");
    //readonly float hashMove = Animator.StringToHash("Move");
    readonly int hashDam = Animator.StringToHash("Damage");
    readonly int hashDie = Animator.StringToHash("Die");

    public AnimController(Animator anim)
    {
        this.anim = anim;
    }

    public void PlayMove(float dir)
    {
        anim.SetFloat("Move" , dir);
    }

    public void PlayAttack()
    {
        anim.SetTrigger(hashAttack);
    }
    public void PlayDam()
    {
        anim.SetTrigger(hashDam);
    }
    public void PlayDie()
    {
        anim.SetTrigger(hashDie);
    }
}
