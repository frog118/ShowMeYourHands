using System;
using HarmonyLib;
using UnityEngine;
using Verse;
using yayoCombat;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(PawnRenderer_override), "DrawEquipmentAiming")]
public static class PawnRenderer_DrawEquipmentAiming_DrawEquipmentAimingOverride
{
    [HarmonyPrefix]
    public static void SaveWeaponLocation(ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
    {
        //ShowMeYourHandsMain.LogMessage($"Saving from dual wield {eq.def.defName}, {drawLoc}, {aimAngle}");
        ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
    }
}