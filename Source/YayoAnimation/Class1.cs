using FacialStuff;
using FacialStuff.Animator;
using FacialStuff.Tweener;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using yayoAni;

namespace ShowMeYourHands.FSWalking;

class RenderPawnAt_Patch
{
    static pawnDrawData pdd;

    public static void RenderPawnAt_Patch_Prefix(PawnRenderer __instance, Vector3 drawLoc, Rot4? rotOverride = null, bool neverAimWeapon = false)
    {
        Pawn pawn = __instance.graphics.pawn;

        //CompFace compFace = pawn.GetCompFace();
        if (!pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        };

        pdd = dataUtility.GetData(pawn);
        compAnim.CurrentRotation = pdd.fixed_rot ?? pawn.Rotation;
        compAnim.Offset_Angle = pdd.offset_angle;
        compAnim.Offset_Pos = pdd.offset_pos;


#pragma warning restore CS0162
    }

}