using System.Reflection;
using HarmonyLib;
using ShowMeYourHands.FSWalking;
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

        harmony.Patch(
            AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt)),
            new HarmonyMethod(typeof(RenderPawnAt_Patch), nameof(RenderPawnAt_Patch.RenderPawnAt_Patch_Prefix)));
    }
}