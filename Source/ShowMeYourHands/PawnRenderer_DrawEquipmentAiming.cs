using System;
using FacialStuff;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

// [HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming", typeof(Thing), typeof(Vector3), typeof(float))]
public class PawnRenderer_DrawEquipmentAiming
{
    //[HarmonyPrefix]
    //[HarmonyPriority(Priority.High)]
    //// JecsTools Oversized overrides always
    //// Gunplay overrides if animations is turned on
    //// [O21] Toolbox overrides if animations is turned on
    //// Yayo Combat 3 overrides if animations is turned on
    //[HarmonyBefore("jecstools.jecrell.comps.oversized",
    //    "rimworld.androitiers-jecrell.comps.oversized",
    //    "com.github.automatic1111.gunplay",
    //    "com.o21toolbox.rimworld.mod",
    //    "com.yayo.combat",
    //    "com.yayo.combat3")]
    public static void SaveWeaponLocation(PawnRenderer __instance, ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        //ShowMeYourHandsMain.LogMessage($"Saving from vanilla {eq.def.defName}, {drawLoc}, {aimAngle}");
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);

        Pawn pawn = __instance.graphics.pawn;

        //ShowMeYourHandsMain.LogMessage($"Saving from vanilla {eq.def.defName}, {drawLoc}, {aimAngle}");
        if (pawn == null || !pawn.GetCompAnim(out CompBodyAnimator compAnim))
        {
            return;
        };

        if (compAnim.CurrentRotation == Rot4.North && aimAngle == 143f)
        {
            aimAngle = 217f;
        }
        
        WhandCompProps extensions = eq.def.GetCompProperties<WhandCompProps>();
        if (extensions == null)
        {
            return;
        }
        bool flipped = (compAnim.CurrentRotation == Rot4.West || compAnim.CurrentRotation == Rot4.North);

        float size = compAnim.GetBodysizeScaling();
        // TODO: Options?

        // ShowMeYourHandsMain.LogMessage($"Changing angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");

        compAnim.CalculatePositionsWeapon(ref aimAngle, extensions, out Vector3 weaponOffset, flipped);
        drawLoc += weaponOffset * size;
        ShowMeYourHandsMain.LogMessage($"New angle and position {eq.def.defName}, {drawLoc}, {aimAngle}");

        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);

    }

}