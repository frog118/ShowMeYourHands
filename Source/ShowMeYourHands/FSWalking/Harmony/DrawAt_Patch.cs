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

        // looks weird with yayos animations, hands ands feet turned off for now
        if (pawn.GetPosture() != PawnPosture.Standing)
        {
            return;
        }

        //CompFace compFace = pawn.GetCompFace();
        if (!pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        };

        float angle = __instance.renderer.BodyAngle();

        Quaternion bodyQuat = Quaternion.AngleAxis(angle, Vector3.up);

        Building_Bed building_Bed = pawn.CurrentBed();

        bool showBody = true;
        if (building_Bed != null && pawn.RaceProps.Humanlike)
        {
            showBody = building_Bed.def.building.bed_showSleeperBody;
        }
        Vector3 bodyPos = (Vector3)AccessTools.Method(typeof(PawnRenderer), "GetBodyPos").Invoke(__instance.renderer, new object[] { loc, showBody });

        // hands and feet look weird with no body. skip it.
        if (!showBody)
        {
           // return;

           var f = loc.y;
           loc = bodyPos;
            loc.y = f;

           /*
            var fixingSleeperVector = new Vector3(0, 0, -0.4f).RotatedBy(angle);
            __state += fixingSleeperVector;
            drawLoc+=fixingSleeperVector;
            */
        }


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
            compAnim?.DrawFeet(bodyQuat, __state, false);
        }
        if (ShowMeYourHandsMod.instance.Settings.UseHands && pawn.carryTracker?.CarriedThing == null)
        {
            compAnim?.DrawHands(bodyQuat, loc);
        }
#pragma warning restore CS0162
    }

}