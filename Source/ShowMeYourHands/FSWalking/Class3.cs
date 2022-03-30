using FacialStuff;
using FacialStuff.Animator;
using FacialStuff.Tweener;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowMeYourHands.FSWalking
{

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

            float angle = __instance.renderer.BodyAngle();

            Quaternion bodyQuat = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 drawLoc = loc;
            Building_Bed building_Bed = pawn.CurrentBed();

            bool showBody = true;
            if (building_Bed != null && pawn.RaceProps.Humanlike)
            {
                showBody = building_Bed.def.building.bed_showSleeperBody;
            }

            // hands and feet look weird with no body. skip it.
            if (!showBody)
            {
                return;
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
            // todo update or not?
            /*
            if (this.ThePawn.IsChild())
            {
                float angle = this.ThePawn.Drawer.renderer.BodyAngle();
                Quaternion bodyQuat = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 rootLoc = this.ThePawn.Drawer.DrawPos;
                if (Controller.settings.UseHands && this.ThePawn.carryTracker?.CarriedThing == null)
                {
                    this.DrawHands(bodyQuat, rootLoc, null, false, this.ThePawn.GetBodysizeScaling());
                }

                if (Controller.settings.UseFeet)
                {
                    this.DrawFeet(bodyQuat, rootLoc, false);
                }
            }
            */

            if (ShowMeYourHandsMod.instance.Settings.UseFeet)
            {
                compAnim?.DrawFeet(bodyQuat, __state, false);
            }
            if (ShowMeYourHandsMod.instance.Settings.UseHands && pawn.carryTracker?.CarriedThing == null)
            {
                compAnim?.DrawHands(bodyQuat, drawLoc);
            }
        }

    }

    [HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
    class BodyAngle_Patch
    {
        static void Postfix(PawnRenderer __instance, ref float __result, Pawn ___pawn)
        {
            if (!___pawn.GetCompAnim(out CompBodyAnimator compAnim))
            {
                return;
            }

            //if (compAnim != null)
            {
                if (compAnim.IsMoving)
                {
                    __result += compAnim.BodyAngle;
                    //return false;
                }
            }
            //return true;
        }

    }

}
