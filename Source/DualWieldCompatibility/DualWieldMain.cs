using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands
{
    [StaticConstructorOnStartup]
    public static class DualWieldMain
    {
        static DualWieldMain()
        {
            var harmony = new Harmony("Mlie.ShowMeYourHands.DualWieldCompatibility");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}