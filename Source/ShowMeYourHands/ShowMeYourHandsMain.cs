using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowMeYourHands
{
    [StaticConstructorOnStartup]
    public static class ShowMeYourHandsMain
    {
        public static readonly Dictionary<Thing, Tuple<Vector3, float>> weaponLocations =
            new Dictionary<Thing, Tuple<Vector3, float>>();

        public static readonly Harmony harmony;

        public static bool BabysAndChildrenLoaded;

        public static MethodInfo GetBodySizeScaling;

        public static bool OversizedWeaponLoaded;

        public static bool EnableOversizedLoaded;

        public static BodyPartDef HandDef;

        public static Dictionary<HediffDef, Color> HediffColors;

        public static readonly List<string> knownPatches = new List<string>
        {
            // This mod
            "Mlie.ShowMeYourHands",
            // Yayos Combat 3
            // Replaces weapon drawer
            "com.yayo.combat",
            "com.yayo.combat3",
            "com.yayo.yayoAni",
            // Dual Wield
            // Replaces weapon drawer if dual wielding
            "Roolo.DualWield",
            // Vanilla Expanded Framework
            "OskarPotocki.VFECore",
            // Vanilla Weapons Expanded - Laser
            // Modifies weapon position for lasers
            "com.ogliss.rimworld.mod.VanillaWeaponsExpandedLaser",
            // JecsTools
            "jecstools.jecrell.comps.oversized",
            "rimworld.androitiers-jecrell.comps.oversized",
            "jecstools.jecrell.comps.installedpart",
            "rimworld.Ogliss.comps.oversized",
            "rimworld.jecrellpelador.comps.oversizedbigchoppa",
            // Adeptus Mechanicus, not sure what
            "com.ogliss.rimworld.mod.AdeptusMechanicus",
            // Faction Colors, not sure what
            "rimworld.ohu.factionColors.main",
            // Enable oversized weapons
            "rimworld.carnysenpai.enableoversizedweapons",
            // Modifies weapon position
            "com.github.automatic1111.gunplay",
            // Red Scare Framework
            // Modifies weapon position, not sure why
            "Chismar.RedScare",
            // [O21] Toolbox
            // Modifies weapon position for lasers
            "com.o21toolbox.rimworld.mod",
            // Rimlaser
            // Modifies weapon position for lasers
            "com.github.automatic1111.rimlaser",
            // Combat extended
            "CombatExtended.HarmonyCE"
        };

        static ShowMeYourHandsMain()
        {
            BabysAndChildrenLoaded = ModLister.GetActiveModWithIdentifier("babies.and.children.continued") != null;
            if (BabysAndChildrenLoaded)
            {
                var type = AccessTools.TypeByName("BabiesAndChildren.GraphicTools");
                if (type != null)
                {
                    GetBodySizeScaling = type.GetMethod("GetBodySizeScaling");
                }

                LogMessage("BabiesAndChildren loaded, will compensate for children hand size");
            }

            OversizedWeaponLoaded = AccessTools.TypeByName("CompOversizedWeapon") != null;
            if (OversizedWeaponLoaded)
            {
                LogMessage("OversizedWeapon loaded, will compensate positioning");
            }

            EnableOversizedLoaded = ModLister.GetActiveModWithIdentifier("CarnySenpai.EnableOversizedWeapons") != null;
            var compProperties = new CompProperties { compClass = typeof(HandDrawer) };
            foreach (var thingDef in from race in DefDatabase<ThingDef>.AllDefsListForReading
                where race.race?.Humanlike == true
                select race)
            {
                thingDef.comps?.Add(compProperties);
            }

            HandDef = DefDatabase<BodyPartDef>.GetNamedSilentFail("Hand");

            var partsHediffs =
                DefDatabase<HediffDef>.AllDefsListForReading.Where(def =>
                    def.hediffClass == typeof(Hediff_AddedPart) && def.spawnThingOnRemoved != null);
            HediffColors = new Dictionary<HediffDef, Color>();
            foreach (var partsHediff in partsHediffs)
            {
                var techLevel = partsHediff.spawnThingOnRemoved.techLevel;
                HediffColors[partsHediff] = GetColorFromTechLevel(techLevel);
            }

            LogMessage($"Cached {HediffColors.Count} hediffs colors");

            harmony = new Harmony("Mlie.ShowMeYourHands");

            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (ModLister.GetActiveModWithIdentifier("MalteSchulze.RIMMSqol") == null)
            {
                return;
            }

            LogMessage(
                "RIMMSqol loaded, will remove their destructive Prefixes for the rotation-methods");
            var original = typeof(Pawn_RotationTracker).GetMethod("FaceCell");
            harmony.Unpatch(original, HarmonyPatchType.Prefix, "RIMMSqol");
            original = typeof(Pawn_RotationTracker).GetMethod("Face");
            harmony.Unpatch(original, HarmonyPatchType.Prefix, "RIMMSqol");
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


        private static Color GetColorFromTechLevel(TechLevel techLevel)
        {
            switch (techLevel)
            {
                case TechLevel.Neolithic:
                    return ThingDefOf.WoodLog.stuffProps.color;
                case TechLevel.Industrial:
                    return ThingDefOf.Steel.stuffProps.color;
                case TechLevel.Spacer:
                    return ThingDefOf.Silver.stuffProps.color;
                case TechLevel.Ultra:
                    return ThingDefOf.Gold.stuffProps.color;
                case TechLevel.Archotech:
                    return ThingDefOf.Plasteel.stuffProps.color;
                default:
                    return ThingDefOf.Steel.stuffProps.color;
            }
        }
    }
}