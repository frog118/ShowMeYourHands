using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using FacialStuff;
using FacialStuff.Tweener;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace ShowMeYourHands.FSWalking
{
    internal class Class1
    {
        private static readonly float angleStanding = 143f;
        private static readonly float angleStandingFlipped = 217f;

        public static void DrawEquipmentAiming_Prefix(PawnRenderer __instance, Thing eq, Vector3 drawLoc,
                                              ref float aimAngle)
        {
            Pawn pawn = __instance.graphics.pawn;

            // Flip the angle for north



            if (!pawn.GetCompAnim(out CompBodyAnimator animator))
            {
                return;
            }
            if (pawn.Rotation == Rot4.North && aimAngle == angleStanding)
            {
                aimAngle = angleStandingFlipped;
            }
            if (Find.TickManager.TicksGame == animator.LastAngleTick)
            {
                aimAngle = animator.LastAimAngle;
                return;
            }

            animator.LastAngleTick = Find.TickManager.TicksGame;

            float angleChange;

            float startAngle = animator.LastAimAngle;
            float endAngle = aimAngle;

            FloatTween tween = animator.AimAngleTween;
            switch (tween.State)
            {
                case TweenState.Running:
                    startAngle = tween.EndValue;
                    endAngle = aimAngle;
                    aimAngle = tween.CurrentValue;
                    break;
            }

            angleChange = CalcShortestRot(startAngle, endAngle);
            if (Mathf.Abs(angleChange) > 6f)
            {
                // no tween for flipping
                bool x = Mathf.Abs(animator.LastAimAngle - angleStanding) < 3f &&
                         Mathf.Abs(aimAngle - angleStandingFlipped) < 3f;
                bool y = Mathf.Abs(animator.LastAimAngle - angleStandingFlipped) < 3f &&
                         Mathf.Abs(aimAngle - angleStanding) < 3f;
                bool z = Math.Abs(Mathf.Abs(aimAngle - animator.LastAimAngle) - 180f) < 12f;

                if (!x && !y && !z)
                {
                    //     if (Math.Abs(aimAngleTween.EndValue - weaponAngle) > 6f)

                    tween.Start(startAngle, startAngle + angleChange, Mathf.Abs(angleChange),
                                ScaleFuncs.QuinticEaseOut);
                    aimAngle = startAngle;
                }
            }

            animator.LastAimAngle = aimAngle;
        }
        // If the return value is positive, then rotate to the left. Else,
        // rotate to the right.
        private static float CalcShortestRot(float from, float to)
        {
            // If from or to is a negative, we have to recalculate them.
            // For an example, if from = -45 then from(-45) + 360 = 315.
            if (@from < 0)
            {
                @from += 360;
            }

            if (to < 0)
            {
                to += 360;
            }

            // Do not rotate if from == to.
            if (@from == to ||
                @from == 0 && to == 360 ||
                @from == 360 && to == 0)
            {
                return 0;
            }

            // Pre-calculate left and right.
            float left = (360 - @from) + to;
            float right = @from - to;

            // If from < to, re-calculate left and right.
            if (@from < to)
            {
                if (to > 0)
                {
                    left = to - @from;
                    right = (360 - to) + @from;
                }
                else
                {
                    left = (360 - to) + @from;
                    right = to - @from;
                }
            }

            // Determine the shortest direction.
            return ((left <= right) ? left : (right * -1));
        }

        public static IEnumerable<CodeInstruction> DrawEquipmentAiming_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator ilGen)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            int index = instructionList.FindIndex(x => x.opcode == OpCodes.Ldloc_0);
            List<Label> labels = instructionList[index].labels;
            instructionList[index].labels = new List<Label>();
            instructionList.InsertRange(index, new List<CodeInstruction>
            {
                                               // DoCalculations(Pawn pawn, Thing eq, ref Vector3 drawLoc, ref float weaponAngle, float aimAngle)
                                               new(OpCodes.Ldarg_0),
                                               new(OpCodes.Ldfld,
                                                                   AccessTools.Field(typeof(PawnRenderer),
                                                                                     "pawn")), // pawn
                                               new(OpCodes.Ldarg_1),           // Thing
                                               new(OpCodes.Ldarga,   2),       // drawLoc
                                               new(OpCodes.Ldloca_S, 1),       // weaponAngle
                                               //   new CodeInstruction(OpCodes.Ldarg_3), // aimAngle
                                               new(OpCodes.Ldloca_S,
                                                                   0), // Mesh, loaded as ref to not trigger I Love Big Guns
                                               new(OpCodes.Call,
                                                                   AccessTools.Method(typeof(Class1),
                                                                                      nameof(DoWeaponOffsets))),
                                               });
            instructionList[index].labels = labels;
            return instructionList;
        }
        public const TweenThing equipment = TweenThing.Equipment;

        public static void DoWeaponOffsets(Pawn pawn, Thing eq, ref Vector3 drawLoc, ref float weaponAngle,
                                   ref Mesh weaponMesh)
        {
            WhandCompProps extensions = eq.def.GetCompProperties<WhandCompProps>();

            bool flipped = weaponMesh == MeshPool.plane10Flip;

            if ((pawn == null) || (!pawn.GetCompAnim(out CompBodyAnimator animator)) || (extensions == null))
            {
                return;
            }

            float sizeMod = 1f;

            //  if (Controller.settings.IReallyLikeBigGuns) { sizeMod = 2.0f; }
            //     else if (Controller.settings.ILikeBigGuns)
            //  {
            //      sizeMod = 1.4f;
            //  }
            //  else
            //  {
            //      sizeMod = 1f;
            //  }

            if (Find.TickManager.TicksGame == animator.LastPosUpdate[(int)equipment])
            {
                drawLoc = animator.LastPosition[(int)equipment];
                weaponAngle = animator.LastWeaponAngle;
            }
            else
            {
                animator.LastPosUpdate[(int)equipment] = Find.TickManager.TicksGame;


                // weapon angle and position offsets based on current attack keyframes sequence
                /*
                DoAttackAnimationOffsetsWeapons(pawn, ref weaponAngle, ref weaponPosOffset, flipped, animator,
                                                out bool noTween);
                */

                Vector3Tween eqTween = animator.Vector3Tweens[(int)equipment];
                bool noTween = false;
                if (pawn.pather.MovedRecently(5))
                {
                    noTween = true;
                }

                switch (eqTween.State)
                {
                    case TweenState.Running:
                        if (noTween || animator.IsMoving)
                        {
                            eqTween.Stop(StopBehavior.ForceComplete);
                        }

                        drawLoc = eqTween.CurrentValue;
                        break;

                    case TweenState.Paused:
                        break;

                    case TweenState.Stopped:
                        if (noTween || (animator.IsMoving))
                        {
                            break;
                        }

                        ScaleFunc scaleFunc = ScaleFuncs.SineEaseOut;

                        Vector3 start = animator.LastPosition[(int)equipment];
                        float distance = Vector3.Distance(start, drawLoc);
                        float duration = Mathf.Abs(distance * 50f);
                        if (start != Vector3.zero && duration > 12f)
                        {
                            start.y = drawLoc.y;
                            eqTween.Start(start, drawLoc, duration, scaleFunc);
                            drawLoc = start;
                        }

                        break;
                }

                // // fix the reset to default pos is target is changing
                // bool isAimAngle = (Math.Abs(aimAngle - angleStanding) <= 0.1f);
                // bool isAimAngleFlipped = (Math.Abs(aimAngle - angleStandingFlipped) <= 0.1f);
                //
                // if (aiming && (isAimAngle || isAimAngleFlipped))
                // {
                //     // use the last known position to avoid 1 frame flipping when target changes
                //     drawLoc = animator.lastPosition[(int)equipment];
                //     weaponAngle = animator.lastWeaponAngle;
                // }
                // else
                {
                    animator.LastPosition[(int)equipment] = drawLoc;
                    animator.LastWeaponAngle = weaponAngle;
                    animator.MeshFlipped = flipped;
                }
            }

            // Now the remaining hands if possible
            if (animator.Props.bipedWithHands && ShowMeYourHandsMod.instance.Settings.UseHands)
            {
                SetPositionsForHandsOnWeapons(
                                              drawLoc,
                                              flipped,
                                              weaponAngle,
                                              extensions, animator, sizeMod);
            }
             static void SetPositionsForHandsOnWeapons(Vector3 weaponPosition, bool flipped, float weaponAngle,
            [CanBeNull]
                                                         WhandCompProps compWeaponExtensions,
                                                 CompBodyAnimator animator, float sizeMod)
            {
                // Prepare everything for DrawHands, but don't draw
                if (compWeaponExtensions == null)
                {
                    return;
                }

                animator.FirstHandPosition = compWeaponExtensions.MainHand;
                animator.SecondHandPosition = compWeaponExtensions.SecHand;

                // Only put the second hand on when aiming or not moving => free left hand for running
                //  bool leftOnWeapon = true;// aiming || !animator.IsMoving;

                if (animator.FirstHandPosition != Vector3.zero)
                {
                    float x = animator.FirstHandPosition.x;
                    float y = animator.FirstHandPosition.y;
                    float z = animator.FirstHandPosition.z;
                    if (flipped)
                    {
                        x *= -1f;
                        y *= -1f;
                    }

                    //if (pawn.Rotation == Rot4.North)
                    //{
                    //    y *= -1f;
                    //}
                    x *= sizeMod;
                    z *= sizeMod;
                    animator.FirstHandPosition =
                    weaponPosition + new Vector3(x, y, z).RotatedBy(weaponAngle);
                }

                if (animator.HasLeftHandPosition)
                {
                    float x2 = animator.SecondHandPosition.x;
                    float y2 = animator.SecondHandPosition.y;
                    float z2 = animator.SecondHandPosition.z;
                    if (flipped)
                    {
                        x2 *= -1f;
                        y2 *= -1f;
                    }

                    x2 *= sizeMod;
                    z2 *= sizeMod;

                    //if (pawn.Rotation == Rot4.North)
                    //{
                    //    y2 *= -1f;
                    //}

                    animator.SecondHandPosition = weaponPosition + new Vector3(x2, y2, z2).RotatedBy(weaponAngle);
                }

                // Swap left and right hand position when flipped

                animator.WeaponQuat = Quaternion.AngleAxis(weaponAngle, Vector3.up);
            }

        }

    }
}
