// ReSharper disable StyleCop.SA1401

using System;
using System.Collections.Generic;
using System.Linq;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using JetBrains.Annotations;
using RimWorld;
using ShowMeYourHands;
using UnityEngine;
using Verse;
using static System.Byte;

namespace FacialStuff.GraphicsFS
{
    [StaticConstructorOnStartup]
    public class PawnBodyGraphic
    {
        #region Public Fields
        private readonly Pawn _pawn;

        public readonly CompBodyAnimator CompAni;
        public Graphic FootGraphicLeft;
        public Graphic FootGraphicLeftCol;
        public Graphic FootGraphicLeftShadow;
        public Graphic FootGraphicRight;
        public Graphic FootGraphicRightCol;
        public Graphic FootGraphicRightShadow;
        public Graphic FrontPawGraphicLeft;
        public Graphic FrontPawGraphicLeftCol;
        public Graphic FrontPawGraphicLeftShadow;
        public Graphic FrontPawGraphicRight;
        public Graphic FrontPawGraphicRightCol;
        public Graphic FrontPawGraphicRightShadow;
        public Graphic HandGraphicLeft;
        public Graphic HandGraphicLeftCol;
        public Graphic HandGraphicLeftShadow;
        public Graphic HandGraphicRight;
        public Graphic HandGraphicRightCol;
        public Graphic HandGraphicRightShadow;

        #endregion Public Fields

        #region Private Fields


        private readonly Color _shadowColor = new(0.54f, 0.56f, 0.6f);

        #endregion Private Fields

        #region Public Constructors

        public PawnBodyGraphic(CompBodyAnimator compAni)
        {
            this.CompAni = compAni;
            this._pawn = compAni.pawn;
            LongEventHandler.ExecuteWhenFinished(this.CheckForAddedOrMissingPartsAndSetColors);
            this.Initialize();
        }

        #endregion Public Constructors

        #region Public Methods

        public void Initialize()
        {
            LongEventHandler.ExecuteWhenFinished(
                                                 () =>
                                                 {

                                                     this.InitializeGraphicsFeet();

                                                     this.InitializeGraphicsHand();

                                                     this.InitializeGraphicsFrontPaws();
                                                 });
        }

        #endregion Public Methods

        #region Private Methods

        private void InitializeGraphicsFeet()
        {
            string texNameFoot = CompAni.TexNameFoot();
            string texNameArtificial = PawnExtensions.PathHumanlike + "Feet/" + CompAni.Props.handType + "_PegLeg";

            // no story, either animal or not humanoid biped

            Color rightColorFoot = Color.red;
            Color leftColorFoot = Color.blue;

            Color skinColor;
            bool animalOverride = this._pawn.story == null;
            if (animalOverride)
            {
                PawnKindLifeStage curKindLifeStage = this._pawn.ageTracker.CurKindLifeStage;

                skinColor = curKindLifeStage.bodyGraphicData.color;
            }
            else
            {
                skinColor = _pawn.story.SkinColor;

            }

            Color rightFootShadowColor = (animalOverride ? skinColor : this.CompAni.FootColorRight) * this._shadowColor;
            Color leftFootShadowColor = (animalOverride ? skinColor : this.CompAni.FootColorLeft) * this._shadowColor;


            Vector2 drawSize = new(1f,1f);
            var stats = this.CompAni.BodyStat;
            this.FootGraphicRight = GraphicDatabase.Get<Graphic_Multi>(
                CompAni.BodyStat.FootRight == PartStatus.Artificial ? texNameArtificial : texNameFoot,
                GetShader(stats.FootRight),
                drawSize,
                animalOverride? skinColor : this.CompAni.FootColorRight,
                animalOverride ? skinColor : this.CompAni.FootColorRight);

            this.FootGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(
                CompAni.BodyStat.FootLeft == PartStatus.Artificial ? texNameArtificial : texNameFoot,
                GetShader(stats.FootLeft),
                drawSize,
                animalOverride ? skinColor : this.CompAni.FootColorLeft,
                animalOverride ? skinColor : this.CompAni.FootColorLeft);

            this.FootGraphicRightShadow = GraphicDatabase.Get<Graphic_Multi>(
                CompAni.BodyStat.FootRight == PartStatus.Artificial ? texNameArtificial : texNameFoot,
                GetShader(stats.FootRight),
                drawSize,
                rightFootShadowColor,
                rightFootShadowColor);

            this.FootGraphicLeftShadow = GraphicDatabase.Get<Graphic_Multi>(
                CompAni.BodyStat.FootLeft == PartStatus.Artificial ? texNameArtificial : texNameFoot,
                GetShader(stats.FootLeft),
                drawSize,
                leftFootShadowColor,
                leftFootShadowColor);

            this.FootGraphicRightCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                GetShader(stats.FootRight),
                drawSize,
                animalOverride ? skinColor : rightColorFoot,
                animalOverride ? skinColor : rightColorFoot);

            this.FootGraphicLeftCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                GetShader(stats.FootLeft),
                drawSize,
                animalOverride ? skinColor : leftColorFoot,
                animalOverride ? skinColor : leftColorFoot);
        }

        private void InitializeGraphicsFrontPaws()
        {
            if (!this.CompAni.Props.quadruped)
            {
                return;

            }
            string texNameFoot = PawnExtensions.PathAnimals + "Paws/" + this.CompAni.Props.handType + PawnExtensions.STR_Foot;

            Color skinColor;
            if (this._pawn.story != null)
            {
                skinColor = this._pawn.story.SkinColor;
            }
            else
            {
                PawnKindLifeStage curKindLifeStage = this._pawn.ageTracker.CurKindLifeStage;

                skinColor = curKindLifeStage.bodyGraphicData.color;
            }

            Color rightColorFoot = Color.cyan;
            Color leftColorFoot = Color.magenta;

            Color rightFootColor = skinColor;
            Color leftFootColor = skinColor;
            Color metal = new(0.51f, 0.61f, 0.66f);

            switch (this.CompAni.BodyStat.FootRight)
            {
                case PartStatus.Artificial:
                    rightFootColor = metal;
                    break;
            }

            switch (this.CompAni.BodyStat.FootLeft)
            {
                case PartStatus.Artificial:
                    leftFootColor = metal;
                    break;
            }

            Color rightFootColorShadow = rightFootColor * this._shadowColor;
            Color leftFootColorShadow = leftFootColor * this._shadowColor;


            Vector2 drawSize = new(1f, 1f);
            this.FrontPawGraphicRight = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightFootColor,
                skinColor);

            this.FrontPawGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftFootColor,
                skinColor);

            this.FrontPawGraphicRightShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightFootColorShadow,
                skinColor);

            this.FrontPawGraphicLeftShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftFootColorShadow,
                skinColor);

            this.FrontPawGraphicRightCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightColorFoot,
                skinColor);

            this.FrontPawGraphicLeftCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftColorFoot,
                skinColor);
        }

        private void InitializeGraphicsHand()
        {
            if (!this.CompAni.Props.bipedWithHands)
            {
                return;
            }

            string texNameHand = this.CompAni.TexNameHand();

            Color rightColorHand = Color.cyan;
            Color leftColorHand = Color.magenta;





            Color metal = new(0.51f, 0.61f, 0.66f);

            var leftHandColor = this.CompAni.HandColorLeft;
            var rightHandColor = this.CompAni.HandColorRight;

            Color leftHandColorShadow = leftHandColor * this._shadowColor;
            Color rightHandColorShadow = rightHandColor * this._shadowColor;

            var stats = this.CompAni.BodyStat;
            Vector2 drawSize = new(1f, 1f);
            this.HandGraphicRight = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandRight),
                drawSize,
                rightHandColor,
                rightHandColor);

            this.HandGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandLeft),
                drawSize,
                leftHandColor,
                leftHandColor);

            this.HandGraphicRightShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandRight),
                drawSize,
                rightHandColorShadow,
                rightHandColorShadow);

            this.HandGraphicLeftShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandLeft),
                drawSize,
                leftHandColorShadow,
                leftHandColorShadow);

            // for development
            this.HandGraphicRightCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandRight),
                drawSize,
                rightColorHand,
                rightColorHand);

            this.HandGraphicLeftCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandLeft),
                drawSize,
                leftColorHand,
                leftColorHand);
        }

        private Shader GetShader(PartStatus partStatus)
        {
            switch (partStatus)
            {
                case PartStatus.Natural:
                    return ShaderDatabase.CutoutSkin;
                case PartStatus.Missing:
                case PartStatus.Artificial:
                case PartStatus.DisplayOverBeard:
                case PartStatus.Apparel:
                default:
                    break;
            }
            return ShaderDatabase.Cutout;

        }

        bool handLeftHasColor = false;
        bool handRightHasColor = false;
        bool footLeftHasColor = false;
        bool footRightHasColor = false;


        public void CheckForAddedOrMissingPartsAndSetColors()
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
            Pawn pawn = this._pawn;
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
             handLeftHasColor = false;
             handRightHasColor = false;
             footLeftHasColor = false;
             footRightHasColor = false;
            colorBody = new List<bool> { false, false, false, false};
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
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Radius") ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Paw"))
                                    {
                                        if (diff.Part.customLabel.Contains("front left") || diff.Part.customLabel.Contains("left"))
                                        {
                                            anim.BodyStat.HandLeft = PartStatus.Missing;
                                        }

                                        if (diff.Part.customLabel.Contains("front right") || diff.Part.customLabel.Contains("right"))
                                        {
                                            anim.BodyStat.HandRight = PartStatus.Missing;
                                        }
                                    }

                                    if (diff.Part.def == BodyPartDefOf.Leg ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Femur") ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Tibia") ||
                                        diff.Part.def == DefDatabase<BodyPartDef>.GetNamedSilentFail("Foot"))
                                    {
                                        if (diff.Part.customLabel.Contains("rear left") || diff.Part.customLabel.Contains("left"))
                                        {
                                            anim.BodyStat.FootLeft = PartStatus.Missing;
                                        }

                                        if (diff.Part.customLabel.Contains("rear right") || diff.Part.customLabel.Contains("right"))
                                        {
                                            anim.BodyStat.FootRight = PartStatus.Missing;
                                        }

                                        if (diff.Part.customLabel.Contains("front left"))
                                        {
                                            anim.BodyStat.HandLeft = PartStatus.Missing;
                                        }

                                        if (diff.Part.customLabel.Contains("front right"))
                                        {
                                            anim.BodyStat.HandRight = PartStatus.Missing;
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
                IEnumerable<Apparel> handApparel = pawn.apparel.WornApparel.Where(apparel =>
                    apparel.def.apparel.bodyPartGroups.Contains(
                        DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Hands")));
                IEnumerable<Apparel> footApparel = pawn.apparel.WornApparel
                    .Where(apparel => apparel.def.apparel.bodyPartGroups.Contains(
                        DefDatabase<BodyPartGroupDef>.GetNamedSilentFail(
                            "Feet")));

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
                        // The method is only called on changes, and the apparel can be dyed. deactivated for now
                        // if (PawnExtensions.colorDictionary == null)
                        {
                            PawnExtensions.colorDictionary = new Dictionary<Thing, Color>();
                        }

                        if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
                        {
                            CompColorable comp = outerApparel.TryGetComp<CompColorable>();
                            if (comp.Active)
                            {
                                SetHandColor(comp.Color);
                            }
                        }

                        if (PawnExtensions.colorDictionary.ContainsKey(outerApparel))
                        {
                            SetHandColor(PawnExtensions.colorDictionary[outerApparel]);
                        }
                        // BUG Tried to get a resource "Things/Pawn/Humanlike/Apparel/Duster/Duster" from a different thread. All resources must be loaded in the main thread. 
                        else
                        {
                            if (outerApparel.Graphic != null && outerApparel.Stuff != null && outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
                            {
                                PawnExtensions.colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
                            }
                            else // BUG Crashhing
                            {
                                Texture2D mainTexture = (Texture2D)outerApparel.Graphic.MatSingle.mainTexture;
                                if (!mainTexture.NullOrBad())
                                {
                                    PawnExtensions.colorDictionary[outerApparel] = AverageColorFromTexture(mainTexture);
                                }
                            }

                            if (PawnExtensions.colorDictionary.ContainsKey(outerApparel))
                            {
                                SetHandColor(PawnExtensions.colorDictionary[outerApparel]);
                            }
                        }
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
                     //   if (PawnExtensions.colorDictionary == null)
                        {
                            PawnExtensions.colorDictionary = new Dictionary<Thing, Color>();
                        }
                        if (PawnExtensions.colorDictionary.ContainsKey(outerApparel))
                        {
                            SetFeetColor(PawnExtensions.colorDictionary[outerApparel]);
                        }
                        else
                        {
                            if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
                            {
                                CompColorable comp = outerApparel.TryGetComp<CompColorable>();
                                if (comp.Active)
                                {
                                    SetFeetColor(comp.Color);
                                }
                            }
                            else if (outerApparel.Stuff != null && outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
                            {
                                PawnExtensions.colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
                            }
                            else // BUG Crashhing
                            {
                                Texture2D mainTexture = (Texture2D)outerApparel.Graphic.MatSingle.mainTexture;

                                if (!mainTexture.NullOrBad())
                                {
                                    PawnExtensions.colorDictionary[outerApparel] = AverageColorFromTexture(mainTexture);
                                }
                            }

                            if (PawnExtensions.colorDictionary.ContainsKey(outerApparel))
                            {
                                SetFeetColor(PawnExtensions.colorDictionary[outerApparel]);
                            }

                        }
                    }
                }
            }
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
        private List<bool> colorBody = new List<bool>{false,false,false,false};

        private void SetHandColor(Color color )
        {
            CompBodyAnimator anim = this.CompAni;
            if (!handLeftHasColor)
            {
                anim.HandColorLeft = color;
                anim.BodyStat.HandLeft = anim.BodyStat.HandLeft != PartStatus.Missing
                    ? PartStatus.Apparel
                    : anim.BodyStat.HandLeft;
                handLeftHasColor = true;
            }

            if (!handRightHasColor)
            {
                anim.HandColorRight = color;
                anim.BodyStat.HandRight = anim.BodyStat.HandRight != PartStatus.Missing
                    ? PartStatus.Apparel
                    : anim.BodyStat.HandRight;
                handRightHasColor = true;
            }

        }
        private void SetFeetColor(Color color )
        {
            CompBodyAnimator anim = this.CompAni;
            if (!footLeftHasColor)
            {
                anim.FootColorLeft = color;
                anim.BodyStat.FootLeft = anim.BodyStat.FootLeft != PartStatus.Missing
                    ? PartStatus.Apparel
                    : anim.BodyStat.FootLeft;
                footLeftHasColor = true;
            }

            if (!footRightHasColor)
            {
                anim.FootColorRight = color;
                anim.BodyStat.FootRight = anim.BodyStat.FootRight != PartStatus.Missing
                    ? PartStatus.Apparel
                    : anim.BodyStat.FootRight;
                footRightHasColor = true;
            }

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

        #endregion Private Methods
    }
}