using System.Collections.Generic;
using FacialStuff;
using JetBrains.Annotations;
using System.Linq;
using UnityEngine;
using Verse;

namespace ShowMeYourHands.FSWalking;

internal static class DrawEquipmentAiming_Patch
{
    private static readonly float angleStanding = 143f;
    private static readonly float angleStandingFlipped = 217f;
    public static readonly Dictionary<Pawn, float> pawnBodySizes = new Dictionary<Pawn, float>();



    /*
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
        */
    /*
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
                                           new CodeInstruction(OpCodes.Ldarg_0),
                                           new CodeInstruction(OpCodes.Ldfld,
                                                               AccessTools.Field(typeof(PawnRenderer),
                                                                                 "pawn")), // pawn
                                           new CodeInstruction(OpCodes.Ldarg_1),           // Thing
                                           new CodeInstruction(OpCodes.Ldarga,   2),       // drawLoc
                                           new CodeInstruction(OpCodes.Ldloca_S, 1),       // weaponAngle
                                           //   new CodeInstruction(OpCodes.Ldarg_3), // aimAngle
                                           new CodeInstruction(OpCodes.Ldloca_S,
                                                               0), // Mesh, loaded as ref to not trigger I Love Big Guns
                                           new CodeInstruction(OpCodes.Call,
                                                               AccessTools.Method(typeof(DrawEquipmentAiming_Patch),
                                                                                  nameof(DoWeaponOffsets))),
                                           });
        instructionList[index].labels = labels;
        return instructionList;
    }*/

}