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

    public static void DrawEquipmentAiming_Postfix(Thing eq, Vector3 drawLoc, float aimAngle, Pawn ___pawn)
    {
        WhandCompProps extensions = eq.def.GetCompProperties<WhandCompProps>();

        if ((___pawn == null) || (extensions == null) || (!___pawn.GetCompBodyAnimator(out CompBodyAnimator animator)) )
        {
            return;
        }

        bool flipped = ___pawn.Rotation == Rot4.West || aimAngle is > 200f and < 340f;

        float sizeMod = animator.GetBodysizeScaling(out _);

        // Now the remaining hands if possible
        if (animator.Props.bipedWithHands && ShowMeYourHandsMod.instance.Settings.UseHands)
        {
            SetPositionsForHandsOnWeapons(___pawn, flipped, extensions, animator);
        }
    }

    private static int LastDrawn;
    private static Vector3 MainHand;
    private static Vector3 OffHand;

    public static void SetPositionsForHandsOnWeapons(Pawn pawn, bool flipped,
        [CanBeNull] WhandCompProps compWeaponExtensions,
        CompBodyAnimator animator)
    {
        // Prepare everything for DrawHands, but don't draw
        if (compWeaponExtensions == null || pawn == null)
        {
            return;
        }

        ThingWithComps mainHandWeapon = pawn.equipment.Primary;
        if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
        {
            if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
            {
                Log.ErrorOnce(
                    $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                    mainHandWeapon.def.GetHashCode());
            }

            return;
        }
        WhandCompProps compProperties = mainHandWeapon.def.GetCompProperties<WhandCompProps>();
        if (compProperties != null)
        {
            MainHand = compProperties.MainHand;
            OffHand = compProperties.SecHand;
        }
        else
        {
            OffHand = Vector3.zero;
            MainHand = Vector3.zero;
        }

        ThingWithComps offHandWeapon = null;
        if (pawn.equipment.AllEquipmentListForReading.Count == 2)
        {
            offHandWeapon = (from weapon in pawn.equipment.AllEquipmentListForReading
                where weapon != mainHandWeapon
                select weapon).First();
            WhandCompProps offhandComp = offHandWeapon?.def.GetCompProperties<WhandCompProps>();
            if (offhandComp != null)
            {
                OffHand = offhandComp.MainHand;
            }
        }

        float aimAngle = 0f;
        bool aiming = false;

        if (pawn.stances.curStance is Stance_Busy { neverAimWeapon: false, focusTarg.IsValid: true } stance_Busy)
        {
            Vector3 a = stance_Busy.focusTarg.HasThing
                ? stance_Busy.focusTarg.Thing.DrawPos
                : stance_Busy.focusTarg.Cell.ToVector3Shifted();

            if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                aimAngle = (a - pawn.DrawPos).AngleFlat();
            }

            aiming = true;
        }


        if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
        {
            if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
            {
                Log.ErrorOnce(
                    $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                    mainHandWeapon.def.GetHashCode());
            }

            return;
        }

        Vector3 mainWeaponLocation = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item1;
        float mainHandAngle = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2;
        Vector3 offhandWeaponLocation = Vector3.zero;
        float offHandAngle = mainHandAngle;
        float mainMeleeExtra = 0f;
        float offMeleeExtra = 0f;
        bool mainMelee = false;
        bool offMelee = false;

        if (offHandWeapon != null)
        {
            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(offHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {offHandWeapon.def.label} from the mod {offHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        offHandWeapon.def.GetHashCode());
                }
            }
            else
            {
                offhandWeaponLocation = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item1;
                offHandAngle = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item2;
            }
        }

        bool idle = false;
        if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
        {
            idle = true;
        }

        mainHandAngle -= 90f;
        offHandAngle -= 90f;
        if (pawn.Rotation == Rot4.West || aimAngle is > 200f and < 340f)
        {
            flipped = true;
        }

        if (mainHandWeapon.def.IsMeleeWeapon)
        {
            mainMelee = true;
            mainMeleeExtra = 0.0001f;
            if (idle && offHandWeapon != null) //Dual wield idle vertical
            {
                if (pawn.Rotation == Rot4.South)
                {
                    mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                }
            }
            else
            {
                if (flipped)
                {
                    mainHandAngle -= 180f;
                    mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                }
            }
        }
        else
        {
            if (flipped)
            {
                mainHandAngle -= 180f;
            }
        }

        if (offHandWeapon?.def?.IsMeleeWeapon == true)
        {
            offMelee = true;
            offMeleeExtra = 0.0001f;
            if (idle && pawn.Rotation == Rot4.North) //Dual wield north
            {
                offHandAngle -= offHandWeapon.def.equippedAngleOffset;
            }
            else
            {
                if (flipped)
                {
                    offHandAngle -= 180f;
                    offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    offHandAngle += offHandWeapon.def.equippedAngleOffset;
                }
            }
        }
        else
        {
            if (flipped)
            {
                offHandAngle -= 180f;
            }
        }

        mainHandAngle %= 360f;
        offHandAngle %= 360f;

        float drawSize = 1f;
        LastDrawn = GenTicks.TicksAbs;

        if (ShowMeYourHandsMod.instance.Settings.RepositionHands && mainHandWeapon.def.graphicData != null &&
            mainHandWeapon?.def?.graphicData?.drawSize.x != 1f)
        {
            drawSize = mainHandWeapon.def.graphicData.drawSize.x;
        }

        if (!pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
        {
            float bodySize = 1f;
            if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
            {
                if (pawn.RaceProps != null)
                {
                    bodySize = pawn.RaceProps.baseBodySize;
                }

                if (ShowMeYourHandsMain.BabysAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                {
                    bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] { pawn });
                }
            }

            pawnBodySizes[pawn] = 0.8f * bodySize;
        }

        // ToDo Integrate: y >= 0 ? matSingle : offSingle

        // Only put the second hand on when aiming or not moving => free left hand for running
        //  bool leftOnWeapon = true;// aiming || !animator.IsMoving;

        if (MainHand != Vector3.zero)
        {
            float x = MainHand.x * drawSize;
            float z = MainHand.z * drawSize;
            float y = MainHand.y < 0 ? -0.0001f : 0.001f;

            if (flipped)
            {
                x *= -1;
            }

            if (pawn.Rotation == Rot4.North && !mainMelee && !aiming)
            {
                z += 0.1f;
            }

            animator.FirstHandPosition = mainWeaponLocation + AdjustRenderOffsetFromDir(pawn, mainHandWeapon as ThingWithComps);
            animator.FirstHandPosition += new Vector3(x, y + mainMeleeExtra, z).RotatedBy(mainHandAngle);
            animator.FirstHandQuat = Quaternion.AngleAxis(mainHandAngle, Vector3.up);
        }


        if (OffHand != Vector3.zero)
        {
            float x2 = OffHand.x * drawSize;
            float z2 = OffHand.z * drawSize;
            float y2 = OffHand.y < 0 ? -0.0001f : 0.001f;


            if (offHandWeapon != null)
            {
                drawSize = 1f;

                if (ShowMeYourHandsMod.instance.Settings.RepositionHands && offHandWeapon.def.graphicData != null &&
                    offHandWeapon.def?.graphicData?.drawSize.x != 1f)
                {
                    drawSize = offHandWeapon.def.graphicData.drawSize.x;
                }

                x2 = OffHand.x * drawSize;
                z2 = OffHand.z * drawSize;

                if (flipped)
                {
                    x2 *= -1;
                }

                if (idle && !offMelee)
                {
                    if (pawn.Rotation == Rot4.South)
                    {
                        z2 += 0.05f;
                    }
                    else
                    {
                        z2 -= 0.05f;
                    }
                }


                animator.SecondHandPosition = offhandWeaponLocation+ AdjustRenderOffsetFromDir(pawn, offHandWeapon as ThingWithComps);
                animator.SecondHandPosition += new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle);

                animator.SecondHandQuat = Quaternion.AngleAxis(offHandAngle, Vector3.up);
            }

        }
    }

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

    private static Vector3 AdjustRenderOffsetFromDir(Pawn pawn, ThingWithComps weapon)
    {
        if (!ShowMeYourHandsMain.OversizedWeaponLoaded && !ShowMeYourHandsMain.EnableOversizedLoaded)
        {
            return Vector3.zero;
        }

        switch (pawn.Rotation.AsInt)
        {
            case 0:
                return ShowMeYourHandsMain.northOffsets.TryGetValue(weapon.def, out Vector3 northValue)
                    ? northValue
                    : Vector3.zero;

            case 1:
                return ShowMeYourHandsMain.eastOffsets.TryGetValue(weapon.def, out Vector3 eastValue)
                    ? eastValue
                    : Vector3.zero;

            case 2:
                return ShowMeYourHandsMain.southOffsets.TryGetValue(weapon.def, out Vector3 southValue)
                    ? southValue
                    : Vector3.zero;

            case 3:
                return ShowMeYourHandsMain.westOffsets.TryGetValue(weapon.def, out Vector3 westValue)
                    ? westValue
                    : Vector3.zero;

            default:
                return Vector3.zero;
        }
    }
}