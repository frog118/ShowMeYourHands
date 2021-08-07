using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands
{
    [StaticConstructorOnStartup]
    public static class YayoAdaptedMain
    {
        static YayoAdaptedMain()
        {
            var harmony = new Harmony("Mlie.ShowMeYourHands.YayoAdaptedCompatibility");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}