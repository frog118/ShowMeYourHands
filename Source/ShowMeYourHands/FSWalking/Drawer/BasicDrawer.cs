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

namespace FacialStuff
{
    public abstract class BasicDrawer
    {
        #region Protected Fields
        private Color handColor;
        private static Dictionary<Thing, Color> colorDictionary;

        [NotNull]
        public Pawn pawn { get; set; }


        #endregion Protected Fields

        #region Public Methods

        protected virtual Mesh GetPawnMesh(bool wantsBody)
        {
            return MeshPool.humanlikeBodySet?.MeshAt(this.CompAnimator.pawn.Rotation);
        }

        #endregion Public Methods

        #region Protected Methods

        protected JointLister GetJointPositions(JointType jointType, Vector3 offsets,
                                                float jointWidth,
                                                bool carrying = false, bool armed = false)
        {
            Rot4 rot = this.CompAnimator.pawn.Rotation;
            JointLister joints = new()
            {
                jointType = jointType
            };
            float leftX = offsets.x;
            float rightX = offsets.x;
            float leftZ = offsets.z;
            float rightZ = offsets.z;

            float offsetY = Offsets.YOffset_HandsFeetOver;
            float leftY = offsetY;

            bool offsetsCarrying = false;

            switch (jointType)
            {
                case JointType.Shoulder:
                    offsetY = armed ? -Offsets.YOffset_HandsFeet : Offsets.YOffset_HandsFeetOver;
                    leftY = this.CompAnimator.IsMoving ? Offsets.YOffset_HandsFeetOver : offsetY;
                    if (carrying) { offsetsCarrying = true; }
                    break;
            }

            float rightY = offsetY;
            if (offsetsCarrying)
            {
                leftX = -jointWidth / 2;
                rightX = jointWidth / 2;
                leftZ = -0.025f;
                rightZ = -leftZ;
            }
            else if (rot.IsHorizontal)
            {
                float offsetX = jointWidth * 0.1f;
                float offsetZ = jointWidth * 0.2f;

                if (rot == Rot4.East)
                {
                    leftY = -Offsets.YOffset_Behind;
                    leftZ += +offsetZ;
                }
                else
                {
                    rightY = -Offsets.YOffset_Behind;
                    rightZ += offsetZ;
                }

                leftX += offsetX;
                rightX -= offsetX;
            }
            else
            {
                leftX = -rightX;
            }

            if (rot == Rot4.North)
            {
                leftY = rightY = -Offsets.YOffset_Behind;
                // leftX *= -1;
                // rightX *= -1;
            }

            joints.RightJoint = new Vector3(rightX, rightY, rightZ);
            joints.LeftJoint = new Vector3(leftX, leftY, leftZ);

            return joints;
        }

        [NotNull]
        public CompBodyAnimator CompAnimator { get; set; }

        private Color HandColor
        {
            get
            {
                if (GenTicks.TicksAbs % 100 != 0 && handColor != default)
                {
                    return handColor;
                }

                if (pawn == null || !pawn.GetCompAnim(out CompBodyAnimator anim))
                {
                    return Color.white;
                }

                handColor = getHandColor(pawn, out bool hasGloves, out Color secondColor);
                if (anim?.pawnBodyGraphic?.HandGraphicRight == null || anim.pawnBodyGraphic.HandGraphicRight.color != handColor)
                {
                    if (hasGloves)
                    {
                        anim.pawnBodyGraphic.HandGraphicRight = GraphicDatabase.Get<Graphic_Single>("HandClean", ShaderDatabase.Cutout,
                            new Vector2(1f, 1f),
                            handColor, handColor);
                    }
                    else
                    {
                        anim.pawnBodyGraphic.HandGraphicRight = GraphicDatabase.Get<Graphic_Single>("Hand", ShaderDatabase.Cutout,
                            new Vector2(1f, 1f),
                            handColor, handColor);
                    }
                }

                if (anim.pawnBodyGraphic.HandGraphicLeft != null && anim.pawnBodyGraphic.HandGraphicLeft.color == handColor)
                {
                    return handColor;
                }

                if (hasGloves)
                {
                    anim.pawnBodyGraphic.HandGraphicLeft = GraphicDatabase.Get<Graphic_Single>("OffHandClean",
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        handColor, handColor);
                }
                else
                {
                    if (secondColor != default)
                    {
                        anim.pawnBodyGraphic.HandGraphicLeft = GraphicDatabase.Get<Graphic_Single>("OffHand",
                            ShaderDatabase.Cutout,
                            new Vector2(1f, 1f),
                            secondColor, secondColor);
                    }
                    else
                    {
                        anim.pawnBodyGraphic.HandGraphicLeft = GraphicDatabase.Get<Graphic_Single>("OffHand", ShaderDatabase.Cutout,
                            new Vector2(1f, 1f),
                            handColor, handColor);
                    }
                }

                return handColor;
            }
            set => handColor = value;
        }
        private Color getHandColor(Pawn pawn, out bool hasGloves, out Color secondColor)
        {
            hasGloves = false;
            secondColor = default;
            List<Hediff_AddedPart> addedHands = null;

            if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts ||
                ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
            {
                addedHands = pawn.health?.hediffSet?.GetHediffs<Hediff_AddedPart>()
                    .Where(x => x.Part.def == ShowMeYourHandsMain.HandDef ||
                                x.Part.parts.Any(record => record.def == ShowMeYourHandsMain.HandDef)).ToList();
            }

            if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts && pawn.health is { hediffSet: { } })
            {
                pawn.GetCompAnim().pawnsMissingAHand  = pawn.health
                        .hediffSet
                        .GetNotMissingParts().Count(record => record.def == ShowMeYourHandsMain.HandDef) +
                    addedHands?.Count < 2;
            }

            if (!ShowMeYourHandsMod.instance.Settings.MatchArmorColor || !(from apparel in pawn.apparel.WornApparel
                                                                           where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                                                                           select apparel).Any())
            {
                if (!ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
                {
                    return pawn.story.SkinColor;
                }

                if (addedHands == null || !addedHands.Any())
                {
                    return pawn.story.SkinColor;
                }

                Color mainColor = (Color)default;

                foreach (Hediff_AddedPart hediffAddedPart in addedHands)
                {
                    if (!ShowMeYourHandsMain.HediffColors.ContainsKey(hediffAddedPart.def))
                    {
                        continue;
                    }

                    if (mainColor == default)
                    {
                        mainColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
                        continue;
                    }

                    secondColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
                }

                if (mainColor == default)
                {
                    return pawn.story.SkinColor;
                }

                if (secondColor == default)
                {
                    secondColor = pawn.story.SkinColor;
                }

                return mainColor;
            }

            IEnumerable<Apparel> handApparel = from apparel in pawn.apparel.WornApparel
                                               where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                                               select apparel;

            //ShowMeYourHandsMain.LogMessage($"Found gloves on {pawn.NameShortColored}: {string.Join(",", handApparel)}");

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

            if (outerApparel == null)
            {
                return pawn.story.SkinColor;
            }

            hasGloves = true;
            if (colorDictionary == null)
            {
                colorDictionary = new Dictionary<Thing, Color>();
            }

            if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
            {
                CompColorable comp = outerApparel.TryGetComp<CompColorable>();
                if (comp.Active)
                {
                    return comp.Color;
                }
            }

            if (colorDictionary.ContainsKey(outerApparel))
            {
                return colorDictionary[outerApparel];
            }

            if (outerApparel.Stuff != null && outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
            {
                colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
            }
            else
            {
                colorDictionary[outerApparel] =
                    AverageColorFromTexture((Texture2D)outerApparel.Graphic.MatSingle.mainTexture);
            }

            return colorDictionary[outerApparel];
        }

        private Color32 AverageColorFromTexture(Texture2D texture)
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

        private Color32 AverageColorFromColors(Color32[] colors)
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

        #endregion Protected Methods
    }
}