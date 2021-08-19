using HarmonyLib;
using Verse;

namespace ShowMeYourHands
{
    [StaticConstructorOnStartup]
    public static class YayoAnimationMain
    {
        static YayoAnimationMain()
        {
            var harmony = new Harmony("Mlie.ShowMeYourHands.YayoAnimationCompatibility");
            var original = AccessTools.Method("yayoAni.patch_DrawEquipmentAiming:Prefix");
            var prefix =
                typeof(PawnRenderer_DrawEquipmentAiming_DrawEquipmentAimingOverride).GetMethod("SaveWeaponLocation");
            harmony.Patch(original, new HarmonyMethod(prefix));
        }
    }
}