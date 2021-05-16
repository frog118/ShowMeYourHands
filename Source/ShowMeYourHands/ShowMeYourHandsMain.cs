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

        public static Harmony harmony;

        public static readonly List<string> knownPatches = new List<string>
        {
            // This mod
            "Mlie.ShowMeYourHands",
            // Yayos Combat 3
            "com.yayo.combat",
            "com.yayo.combat3",
            // Dual Wield
            "Roolo.DualWield",
            // Vanilla Expanded Framework
            "OskarPotocki.VFECore",
            // Vanilla Weapons Expanded - Laser
            "com.ogliss.rimworld.mod.VanillaWeaponsExpandedLaser",
            // JecsTools
            "jecstools.jecrell.comps.oversized",
            "rimworld.androitiers-jecrell.comps.oversized",
            "jecstools.jecrell.comps.installedpart",
            // Gunplay
            "com.github.automatic1111.gunplay",
            // [O21] Toolbox
            "com.o21toolbox.rimworld.mod"
        };

        static ShowMeYourHandsMain()
        {
            var compProperties = new CompProperties {compClass = typeof(HandDrawer)};
            foreach (var thingDef in from race in DefDatabase<ThingDef>.AllDefsListForReading
                where race.race?.Humanlike == true
                select race)
            {
                thingDef.comps?.Add(compProperties);
            }

            harmony = new Harmony("Mlie.ShowMeYourHands");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void LogMessage(string message, bool forced = false, bool warning = false)
        {
            if (warning)
            {
                Log.Warning($"[ShowMeYourHands]: {message}");
                return;
            }

            if (!forced && !ShowMeYourHandsMod.instance.Settings.VerboseLogging)
            {
                return;
            }

            Log.Message($"[ShowMeYourHands]: {message}");
        }
    }
}