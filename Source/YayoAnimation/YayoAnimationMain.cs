using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public static class YayoAnimationMain
{
    static YayoAnimationMain()
    {
        Harmony harmony = new("Mlie.ShowMeYourHands.YayoAnimationCompatibility");
        MethodInfo original = AccessTools.Method("yayoAni.patch_DrawEquipmentAiming:Prefix");
        MethodInfo prefix =
            typeof(PawnRenderer_DrawEquipmentAiming_DrawEquipmentAimingOverride).GetMethod("SaveWeaponLocation");
        harmony.Patch(original, new HarmonyMethod(prefix));
    }
}