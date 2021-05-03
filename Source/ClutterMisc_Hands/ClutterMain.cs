using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace WHands
{
    [StaticConstructorOnStartup]
    internal static class ClutterMain
    {
        public static readonly List<ThingDef> doneWeapons = new List<ThingDef>();

        static ClutterMain()
        {
            var harmony = new Harmony("Mlie.WHands");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}