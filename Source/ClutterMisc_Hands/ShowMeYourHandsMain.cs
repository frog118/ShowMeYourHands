using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands
{
    [StaticConstructorOnStartup]
    internal static class ShowMeYourHandsMain
    {
        static ShowMeYourHandsMain()
        {
            var compProperties = new CompProperties {compClass = typeof(HandDrawer)};
            foreach (var thingDef in from race in DefDatabase<ThingDef>.AllDefsListForReading
                where race.race?.Humanlike == true
                select race)
            {
                thingDef.comps?.Add(compProperties);
            }

            var harmony = new Harmony("Mlie.ShowMeYourHands");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void LogMessage(string message, bool forced = false)
        {
            if (!forced && !ShowMeYourHandsMod.instance.Settings.VerboseLogging)
            {
                return;
            }

            Log.Message($"[ShowMeYourHands]: {message}");
        }
    }
}