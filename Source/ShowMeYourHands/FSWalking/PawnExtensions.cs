using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using JetBrains.Annotations;
using RimWorld;
using ShowMeYourHands;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using static System.Byte;

namespace FacialStuff
{
    [StaticConstructorOnStartup]
    public static class PawnExtensions
    {
        public static readonly Dictionary<Pawn, float> pawnBodySizes = new Dictionary<Pawn, float>();
        public const string PathHumanlike = "Things/Pawn/Humanlike/";
        public const string PathAnimals = "Things/Pawn/Animal/";
        public const string STR_Foot = "_Foot";
        public const string STR_Hand = "_Hand";
        private static Dictionary<Thing, Color> colorDictionary;

        public static bool ShowWeaponOpenly(this Pawn pawn)
        {
            return pawn.carryTracker?.CarriedThing == null && pawn.equipment?.Primary != null &&
                   (pawn.Drafted ||
                    (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon) ||
                    (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon));
        }

        public static void CheckForAddedOrMissingPartsAndSetColors(this Pawn pawn)
        {
            //   string log = "Checking for parts on " + pawn.LabelShort + " ...";
            /*
            if (!ShowMeYourHandsMod.instance.Settings.ShowExtraParts)
            {
                //      log += "\n" + "No extra parts in options, return";
                //      Log.Message(log);
                return false;
            }
*/
            if (pawn == null)
            {
                return;

            }
            Color skinColor = Color.white;
            bool animalOverride = pawn.story == null;
            if (animalOverride)
            {
                PawnKindLifeStage curKindLifeStage = pawn.ageTracker?.CurKindLifeStage;

                if (curKindLifeStage != null)
                    if (curKindLifeStage.bodyGraphicData != null)
                        skinColor = curKindLifeStage.bodyGraphicData.color;
            }
            else
            {
                skinColor = pawn.story.SkinColor;
            }
            if (pawn.GetCompAnim(out CompBodyAnimator anim))
            {
                anim.BodyStat.HandLeft = PartStatus.Natural;
                anim.BodyStat.HandRight = PartStatus.Natural;
                anim.BodyStat.FootLeft = PartStatus.Natural;
                anim.BodyStat.FootRight = PartStatus.Natural;

                anim.HandColorLeft =
                    anim.HandColorRight = anim.FootColorLeft = anim.FootColorRight = skinColor;
                if (animalOverride)
                {
                    return;
                }
            }
            else
            {
                return;
            }
            bool handLeftHasColor = false;
            bool handRightHasColor = false;
            bool footLeftHasColor = false;
            bool footRightHasColor = false;
            if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts ||
                ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
            {
                List<BodyPartRecord> allParts = pawn.RaceProps?.body?.AllParts;

                if (!allParts.NullOrEmpty())
                {
                    if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts)
                    {
                        List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs?.Where(x => x.def != null && x != null && !x.def.defName.NullOrEmpty()).ToList();
                        if (hediffs != null)
                            foreach (Hediff diff in hediffs.Where(diff => diff?.def == HediffDefOf.MissingBodyPart))
                            {
                                // Log.Message("Checking missing part "+diff.def.defName);
                                if (allParts.NullOrEmpty() || diff?.def == null)
                                {
                                    Log.Message("Body list or hediff.def is null or empty");
                                    continue;
                                }

                                if (!diff.Visible)
                                {
                                    continue;
                                }

                                if (diff.def?.defName == null)
                                {
                                    continue;
                                }

                                if (diff.def == HediffDefOf.MissingBodyPart)
                                {
                                    if (diff.Part.def == BodyPartDefOf.Arm || diff.Part.def == BodyPartDefOf.Hand ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Shoulder") ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Clavicle") ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Humerus") ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Radius"))
                                    {
                                        if (diff.Part.customLabel.Contains("left"))
                                        {
                                            anim.BodyStat.HandLeft = PartStatus.Missing;
                                        }

                                        if (diff.Part.customLabel.Contains("right"))
                                        {
                                            anim.BodyStat.HandRight = PartStatus.Missing;
                                        }
                                    }

                                    if (diff.Part.def == BodyPartDefOf.Leg ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Femur") ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Tibia") ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Foot"))
                                    {
                                        if (diff.Part.customLabel.Contains("left"))
                                        {
                                            anim.BodyStat.FootLeft = PartStatus.Missing;
                                        }

                                        if (diff.Part.customLabel.Contains("right"))
                                        {
                                            anim.BodyStat.FootRight = PartStatus.Missing;
                                        }
                                    }
                                }

                                //BodyPartRecord rightArm = body.Find(x => x.def == DefDatabase<BodyPartDef>.GetNamed("RightShoulder"));
                            }
                    }

                    if (ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
                    {
                        List<Hediff_AddedPart> addedhediffs = pawn?.health?.hediffSet?.GetHediffs<Hediff_AddedPart>()
                            .Where(x => x?.def != null && !x.def.defName.NullOrEmpty()).ToList();

                        if (addedhediffs != null)
                            foreach (Hediff_AddedPart diff in addedhediffs)
                            {
                                if (allParts.NullOrEmpty() || diff?.def == null)
                                {
                                    Log.Message("Body list or hediff.def is null or empty");
                                    continue;
                                }

                                if (!diff.Visible)
                                {
                                    continue;
                                }

                                if (diff.def?.defName == null)
                                {
                                    continue;
                                }

                                AddedBodyPartProps addedPartProps = diff.def?.addedPartProps;
                                if (addedPartProps == null)
                                {
                                    continue;
                                }

                                if (diff.Part.def == BodyPartDefOf.Arm || diff.Part.def == BodyPartDefOf.Hand ||
                                    diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Shoulder") ||
                                    diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Clavicle") ||
                                    diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Humerus") ||
                                    diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Radius"))
                                {
                                    if (diff.Part.customLabel.Contains("left"))
                                    {
                                        anim.BodyStat.HandLeft = PartStatus.Artificial;
                                        if (ShowMeYourHandsMain.HediffColors.ContainsKey(diff.def))
                                        {
                                            anim.HandColorLeft = ShowMeYourHandsMain.HediffColors[diff.def];
                                            handLeftHasColor = true;
                                        }
                                    }

                                    if (diff.Part.customLabel.Contains("right"))
                                    {
                                        anim.BodyStat.HandRight = PartStatus.Artificial;
                                        if (ShowMeYourHandsMain.HediffColors.ContainsKey(diff.def))
                                        {
                                            anim.HandColorRight = ShowMeYourHandsMain.HediffColors[diff.def];
                                            handRightHasColor = true;
                                        }
                                    }
                                }

                                if (diff.Part.def == BodyPartDefOf.Leg ||
                                    diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Femur") ||
                                    diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Tibia") ||
                                    diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Foot"))
                                {
                                    if (diff.Part.customLabel.Contains("left"))
                                    {
                                        anim.BodyStat.FootLeft = PartStatus.Artificial;
                                        if (ShowMeYourHandsMain.HediffColors.ContainsKey(diff.def))
                                        {
                                            anim.FootColorLeft = ShowMeYourHandsMain.HediffColors[diff.def];
                                            footLeftHasColor = true;
                                        }
                                    }

                                    if (diff.Part.customLabel.Contains("right"))
                                    {
                                        anim.BodyStat.FootLeft = PartStatus.Artificial;
                                        if (ShowMeYourHandsMain.HediffColors.ContainsKey(diff.def))
                                        {
                                            anim.FootColorRight = ShowMeYourHandsMain.HediffColors[diff.def];
                                            footRightHasColor = true;
                                        }
                                    }
                                }

                                //BodyPartRecord rightArm = body.Find(x => x.def == DefDatabase<BodyPartDef>.GetNamed("RightShoulder"));

                                // Missing parts first, hands and feet can be replaced by arms/legs
                                //  Log.Message("Checking missing parts.");
                            }
                    }
                }
            }

            if (ShowMeYourHandsMod.instance.Settings.MatchArmorColor)
            {
                IEnumerable<Apparel> handApparel = from apparel in pawn.apparel.WornApparel
                                                   where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                                                   select apparel;
                IEnumerable<Apparel> footApparel = from apparel in pawn.apparel.WornApparel
                                                   where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Feet")
                                                   select apparel;

                //ShowMeYourHandsMain.LogMessage($"Found gloves on {pawn.NameShortColored}: {string.Join(",", handApparel)}");

                if (!handApparel.EnumerableNullOrEmpty())
                {
                    Thing outerApparel = null;
                    int highestDrawOrder = 0;
                    foreach (Apparel thing in handApparel)
                    {
                        int thingOutmostLayer =
                            thing.def.apparel.layers.OrderByDescending(def => def.drawOrder).First().drawOrder;
                        if (outerApparel != null && highestDrawOrder >= thingOutmostLayer)
                        {
                            continue;
                        }

                        highestDrawOrder = thingOutmostLayer;
                        outerApparel = thing;
                    }

                    if (outerApparel != null)
                    {
                        if (colorDictionary == null)
                        {
                            colorDictionary = new Dictionary<Thing, Color>();
                        }

                        if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
                        {
                            CompColorable comp = outerApparel.TryGetComp<CompColorable>();
                            if (comp.Active)
                            {
                                if (!handLeftHasColor)
                                {
                                    anim.HandColorLeft = comp.Color;
                                    anim.BodyStat.HandLeft = anim.BodyStat.HandLeft != PartStatus.Missing
                                        ? PartStatus.Apparel
                                        : anim.BodyStat.HandLeft;
                                    handLeftHasColor = true;
                                }

                                if (!handRightHasColor)
                                {
                                    anim.HandColorRight = comp.Color;
                                    anim.BodyStat.HandRight = anim.BodyStat.HandRight != PartStatus.Missing
                                        ? PartStatus.Apparel
                                        : anim.BodyStat.HandRight;
                                    handRightHasColor = true;
                                }
                            }
                        }

                        if (colorDictionary.ContainsKey(outerApparel))
                        {
                            if (!handLeftHasColor)
                            {
                                anim.HandColorLeft = colorDictionary[outerApparel];
                                anim.BodyStat.HandLeft = anim.BodyStat.HandLeft != PartStatus.Missing
                                    ? PartStatus.Apparel
                                    : anim.BodyStat.HandLeft;
                            }

                            if (!handRightHasColor)
                            {
                                anim.HandColorRight = colorDictionary[outerApparel];
                                anim.BodyStat.HandRight = anim.BodyStat.HandRight != PartStatus.Missing
                                    ? PartStatus.Apparel
                                    : anim.BodyStat.HandRight;
                            }
                        }
                        // BUG Tried to get a resource "Things/Pawn/Humanlike/Apparel/Duster/Duster" from a different thread. All resources must be loaded in the main thread. 
                        /*else
                        {
                            if (outerApparel.Stuff != null &&
                                outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
                            {
                                colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
                            }
                            else // BUG Crashhing
                            {
                                Texture2D mainTexture = (Texture2D)outerApparel.Graphic.MatSingle.mainTexture;
                                if (!mainTexture.NullOrBad())
                                {
                                     colorDictionary[outerApparel] = AverageColorFromTexture(mainTexture);
                                }
                            }
                        }*/
                    }
                }

                if (!footApparel.EnumerableNullOrEmpty())
                {
                    Thing outerApparel = null;
                    int highestDrawOrder = 0;
                    foreach (Apparel thing in footApparel)
                    {
                        int thingOutmostLayer =
                            thing.def.apparel.layers.OrderByDescending(def => def.drawOrder).First().drawOrder;
                        if (outerApparel != null && highestDrawOrder >= thingOutmostLayer)
                        {
                            continue;
                        }

                        highestDrawOrder = thingOutmostLayer;
                        outerApparel = thing;
                    }

                    bool hasColor = false;
                    if (outerApparel != null)
                    {
                        if (colorDictionary == null)
                        {
                            colorDictionary = new Dictionary<Thing, Color>();
                        }
                        if (colorDictionary.ContainsKey(outerApparel))
                        {
                            if (!footLeftHasColor)
                            {
                                anim.FootColorLeft = colorDictionary[outerApparel];
                                anim.BodyStat.FootLeft = anim.BodyStat.FootLeft != PartStatus.Missing
                                    ? PartStatus.Apparel
                                    : anim.BodyStat.FootLeft;
                            }

                            if (!footRightHasColor)
                            {
                                anim.FootColorRight = colorDictionary[outerApparel];
                                anim.BodyStat.FootRight = anim.BodyStat.FootRight != PartStatus.Missing
                                    ? PartStatus.Apparel
                                    : anim.BodyStat.FootRight;
                            }
                        }
                        else
                        {
                            if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
                            {
                                CompColorable comp = outerApparel.TryGetComp<CompColorable>();
                                if (comp.Active)
                                {
                                    if (!footLeftHasColor)
                                    {
                                        anim.FootColorLeft = comp.Color;
                                        anim.BodyStat.FootLeft = anim.BodyStat.FootLeft != PartStatus.Missing
                                            ? PartStatus.Apparel
                                            : anim.BodyStat.FootLeft;
                                        footLeftHasColor = true;
                                    }

                                    if (!footRightHasColor)
                                    {
                                        anim.FootColorRight = comp.Color;
                                        anim.BodyStat.FootRight = anim.BodyStat.FootRight != PartStatus.Missing
                                            ? PartStatus.Apparel
                                            : anim.BodyStat.FootRight;
                                        footRightHasColor = true;
                                    }
                                }
                            }

                            // BUG Tried to get a resource "Things/Pawn/Humanlike/Apparel/Duster/Duster" from a different thread. All resources must be loaded in the main thread. 
                            /*
                            else if (outerApparel.Stuff != null &&
                                outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
                            {
                                colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
                            }
                            else // BUG Crashhing
                            {
                                 Texture2D mainTexture = (Texture2D)outerApparel.Graphic.MatSingle.mainTexture;

                                if (!mainTexture.NullOrBad())
                                {
                                      colorDictionary[outerApparel] = AverageColorFromTexture(mainTexture);
                                }
                            }
                            */
                        }
                    }
                }
            }
        }

        public static bool Fleeing(this Pawn pawn)
        {
            Job job = pawn.CurJob;
            return pawn.MentalStateDef == MentalStateDefOf.PanicFlee
                || (job != null && (job.def == JobDefOf.Flee || job.def == JobDefOf.FleeAndCower));
        }

        [CanBeNull]
        public static CompBodyAnimator GetCompAnim([NotNull] this Pawn pawn)
        {
            return pawn.GetComp<CompBodyAnimator>();
        }

        public static bool GetCompAnim([NotNull] this Pawn pawn, [NotNull] out CompBodyAnimator compAnim)
        {
            compAnim = pawn.GetComp<CompBodyAnimator>();
            return compAnim != null;
        }

        /*
        [CanBeNull]
        public static CompFace GetCompFace([NotNull] this Pawn pawn)
        {
            return pawn.GetComp<CompFace>();
        }
        public static bool GetCompFace([NotNull] this Pawn pawn, [NotNull] out CompFace compFace)
        {
            compFace = pawn.GetComp<CompFace>();
            return compFace != null;
        }
        */
        /*
        public static bool GetPawnFace([NotNull] this Pawn pawn, [CanBeNull] out PawnFace pawnFace)
        {
            pawnFace = null;

            if (!pawn.GetCompFace(out CompFace compFace))
            {
                return false;
            }

            PawnFace face = compFace.PawnFace;
            if (face != null)
            {
                pawnFace = face;
                return true;
            }

            return false;
        }
        */

        public static bool HasCompAnimator([NotNull] this Pawn pawn)
        {
            return pawn.def.HasComp(typeof(CompBodyAnimator));
        }

        private static Color32 AverageColorFromTexture(Texture2D texture)
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D tex = new(texture.width, texture.height);
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return AverageColorFromColors(tex.GetPixels32());
        }

        private static Color32 AverageColorFromColors(Color32[] colors)
        {
            Dictionary<Color32, int> shadeDictionary = new();
            foreach (Color32 texColor in colors)
            {
                if (texColor.a < 50)
                {
                    // Ignore low transparency
                    continue;
                }

                Rgb currentRgb = new() { B = texColor.b, G = texColor.b, R = texColor.r };

                if (currentRgb.Compare(new Rgb { B = 0, G = 0, R = 0 }, new Cie1976Comparison()) < 2)
                {
                    // Ignore black pixels
                    continue;
                }

                if (shadeDictionary.Count == 0)
                {
                    shadeDictionary[texColor] = 1;
                    continue;
                }

                bool added = false;
                foreach (Color32 rgb in shadeDictionary.Keys.Where(rgb =>
                             currentRgb.Compare(new Rgb { B = rgb.b, G = rgb.b, R = rgb.r }, new Cie1976Comparison()) < 2))
                {
                    shadeDictionary[rgb]++;
                    added = true;
                    break;
                }

                if (!added)
                {
                    shadeDictionary[texColor] = 1;
                }
            }

            if (shadeDictionary.Count == 0)
            {
                return new Color32(0, 0, 0, MaxValue);
            }

            Color32 greatestValue = shadeDictionary.Aggregate((rgb, max) => rgb.Value > max.Value ? rgb : max).Key;
            greatestValue.a = MaxValue;
            return greatestValue;
        }

        /*
        public static bool HasPawnFace([NotNull] this Pawn pawn)
        {
            if (pawn.GetCompFace(out CompFace compFace))
            {
                PawnFace face = compFace.PawnFace;
                return face != null;
            }

            return false;
        }
        */
    }
}