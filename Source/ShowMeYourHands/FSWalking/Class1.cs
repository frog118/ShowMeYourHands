using FacialStuff;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(PawnRenderer), "DrawEquipment")]
public static class PawnRenderer_DrawEquipment
{
// Verse.PawnRenderer

private static bool SwingWeapon(Pawn pawn)
{
    if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
    {
        return false;
    }

    if (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon)
    {
        return true;
    }
    if (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon)
    {
        return true;
    }
    Lord lord = pawn.GetLord();
    if (lord != null && lord.LordJob != null && lord.LordJob.AlwaysShowWeapon)
    {
        return true;
    }
    return false;
}

public static void Prefix(PawnRenderer __instance, ref Vector3 rootLoc, Rot4 pawnRotation, PawnRenderFlags flags, Pawn ___pawn)
    {
        if (!SwingWeapon(___pawn))
        {
            return;

        }

        if (___pawn.stances.curStance is Stance_Busy stance_Busy && !stance_Busy.neverAimWeapon &&
            stance_Busy.focusTarg.IsValid && (flags & PawnRenderFlags.NeverAimWeapon) == 0)
        {
            return;
        }

        if (!___pawn.GetCompAnim(out CompBodyAnimator anim))
        {
            return;

        }

        if (anim.IsMoving)
        {
            Vector3 svec = rootLoc;
            rootLoc = anim.lastMainHandPosition;
            rootLoc.y = svec.y;
        }

    }
}