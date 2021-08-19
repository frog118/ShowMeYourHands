using System;
using UnityEngine;
using Verse;

namespace ShowMeYourHands
{
    public static class PawnRenderer_DrawEquipmentAiming_DrawEquipmentAimingOverride
    {
        public static void SaveWeaponLocation(ref Thing eq, ref Vector3 drawLoc, ref float aimAngle)
        {
            //ShowMeYourHandsMain.LogMessage($"Saving from dual wield {eq.def.defName}, {drawLoc}, {aimAngle}");
            ShowMeYourHandsMain.weaponLocations[eq] = new Tuple<Vector3, float>(drawLoc, aimAngle);
        }
    }
}