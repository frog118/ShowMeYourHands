using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands
{
    [StaticConstructorOnStartup]
    public static class ShowMeYourHandsMain
    {
        public static readonly Dictionary<Thing, Tuple<Vector3, float>> weaponLocations =
            new Dictionary<Thing, Tuple<Vector3, float>>();

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