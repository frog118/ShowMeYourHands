using FacialStuff;
using FacialStuff.Animator;
using FacialStuff.Tweener;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowMeYourHands.FSWalking;

[HarmonyAfter("com.yayo.yayoAni")]
[HarmonyPatch(typeof(Pawn_DrawTracker), "DrawAt")]
class DrawAt_Patch
{
    static void Prefix(Pawn_DrawTracker __instance, ref Vector3 loc, out Vector3 __state)
    {
        Pawn pawn = __instance.renderer.graphics.pawn;
        //CompFace compFace = pawn.GetCompFace();
        CompBodyAnimator compAnim = pawn.GetCompAnim();

        loc.x += compAnim?.BodyAnim?.offCenterX ?? 0f;
        __state = loc;
        compAnim?.ApplyBodyWobble(ref loc, ref __state);
        compAnim?.TickDrawers();

    }

    static void Postfix(Pawn_DrawTracker __instance, Vector3 loc, Vector3 __state, Pawn ___pawn)
    {
        Pawn pawn = ___pawn;



        //CompFace compFace = pawn.GetCompFace();
        if (!pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        };

        float bodyAngle = __instance.renderer.BodyAngle();
        // adding the pdd angle offset. could be a bug, but looks ok
        float handAngle = bodyAngle - compAnim.Offset_Angle;

        bool isStanding = pawn.GetPosture() == PawnPosture.Standing;



        // add the offset to the hand as its tied to the body
        loc += compAnim.Offset_Pos;

        //keep the feet on the ground and steady. rotation and pos offset only in bed
        if (!isStanding)
        {
            __state += compAnim.Offset_Pos;
        }
        Quaternion footQuat = Quaternion.AngleAxis(isStanding ? 0f : handAngle, Vector3.up);

        Quaternion handQuat = Quaternion.AngleAxis(handAngle, Vector3.up);

        // do the tweening now
        if (compAnim.Props.bipedWithHands)
        {
            BodyAnimator.AnimatorTick();
        }

        // Tweener
        Vector3Tween eqTween = compAnim.Vector3Tweens[(int)TweenThing.Equipment];

        FloatTween angleTween = compAnim.AimAngleTween;
        Vector3Tween leftHand = compAnim.Vector3Tweens[(int)TweenThing.HandLeft];
        Vector3Tween rightHand = compAnim.Vector3Tweens[(int)TweenThing.HandRight];

        if (!Find.TickManager.Paused)
        {
            if (leftHand.State == TweenState.Running)
            {
                leftHand.Update(1f * Find.TickManager.TickRateMultiplier);
            }
            if (rightHand.State == TweenState.Running)
            {
                rightHand.Update(1f * Find.TickManager.TickRateMultiplier);
            }
            if (eqTween.State == TweenState.Running)
            {
                eqTween.Update(1f * Find.TickManager.TickRateMultiplier);
            }

            if (angleTween.State == TweenState.Running)
            {
                compAnim.AimAngleTween.Update(3f * Find.TickManager.TickRateMultiplier);
            }

            compAnim.CheckMovement();

        }

        // feet shouldn't rotate while standing. 
        if (ShowMeYourHandsMod.instance.Settings.UseFeet)
        {
            compAnim?.DrawFeet(footQuat, __state, loc);
        }
        if (ShowMeYourHandsMod.instance.Settings.UseHands && pawn.carryTracker?.CarriedThing == null)
        {
            compAnim?.DrawHands(handQuat, loc);
        }
#pragma warning restore CS0162
    }

}