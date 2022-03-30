using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public static class YayoAdaptedMain
{
    static YayoAdaptedMain()
    {
        Harmony harmony = new("Mlie.ShowMeYourHands.YayoAdaptedCompatibility");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}