using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands
{
    [HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming", typeof(Thing), typeof(Vector3), typeof(float))]
    public class PawnRenderer_DrawEquipmentAiming
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static void SaveWeaponLocation(ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
        {
            //ShowMeYourHandsMain.LogMessage($"Saving from vanilla {eq.def.defName}, {drawLoc}, {aimAngle}");
            ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
        }
    }
}