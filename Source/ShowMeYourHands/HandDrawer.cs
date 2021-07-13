using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using HarmonyLib;
using UnityEngine;
using Verse;
using static System.Byte;

namespace ShowMeYourHands
{
    [StaticConstructorOnStartup]
    public class HandDrawer : ThingComp
    {
        private static Dictionary<Thing, Color> colorDictionary;

        private readonly Dictionary<Pawn, Graphic> mainHandGraphics = new Dictionary<Pawn, Graphic>();
        private readonly Dictionary<Pawn, Graphic> offHandGraphics = new Dictionary<Pawn, Graphic>();
        private readonly Dictionary<Pawn, float> pawnBodySizes = new Dictionary<Pawn, float>();

        private Color handColor;
        private Vector3 MainHand;
        private Vector3 OffHand;

        private Color HandColor
        {
            get
            {
                if (GenTicks.TicksAbs % 100 != 0 && handColor != default)
                {
                    return handColor;
                }

                if (!(parent is Pawn pawn))
                {
                    return Color.white;
                }

                handColor = getHandColor(pawn, out var hasGloves);
                if (!mainHandGraphics.ContainsKey(pawn) || mainHandGraphics[pawn].color != handColor)
                {
                    if (hasGloves)
                    {
                        mainHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("HandClean", ShaderDatabase.Cutout,
                            new Vector2(1f, 1f),
                            handColor, handColor);
                    }
                    else
                    {
                        mainHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("Hand", ShaderDatabase.Cutout,
                            new Vector2(1f, 1f),
                            handColor, handColor);
                    }
                }

                if (offHandGraphics.ContainsKey(pawn) && offHandGraphics[pawn].color == handColor)
                {
                    return handColor;
                }

                if (hasGloves)
                {
                    offHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("OffHandClean",
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        handColor, handColor);
                }
                else
                {
                    offHandGraphics[pawn] = GraphicDatabase.Get<Graphic_Single>("OffHand", ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        handColor, handColor);
                }

                return handColor;
            }
            set => handColor = value;
        }

        public void ReadXML()
        {
            var whandCompProps = (WhandCompProps) props;
            if (whandCompProps.MainHand != Vector3.zero)
            {
                MainHand = whandCompProps.MainHand;
            }

            if (whandCompProps.SecHand != Vector3.zero)
            {
                OffHand = whandCompProps.SecHand;
            }
        }

        private bool CarryWeaponOpenly(Pawn pawn)
        {
            return pawn.carryTracker?.CarriedThing == null && (pawn.Drafted ||
                                                               pawn.CurJob != null &&
                                                               pawn.CurJob.def.alwaysShowWeapon ||
                                                               pawn.mindState.duty != null &&
                                                               pawn.mindState.duty.def.alwaysShowWeapon);
        }

        private void AngleCalc()
        {
            if (!(parent is Pawn pawn) || pawn.Dead || !pawn.Spawned)
            {
                return;
            }

            if (pawn.equipment?.Primary == null)
            {
                return;
            }

            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon)
            {
                return;
            }

            var mainhandWeapon = pawn.equipment.Primary;
            var compProperties = mainhandWeapon.def.GetCompProperties<WhandCompProps>();
            if (compProperties != null)
            {
                MainHand = compProperties.MainHand;
                OffHand = compProperties.SecHand;
            }
            else
            {
                OffHand = Vector3.zero;
                MainHand = Vector3.zero;
            }

            ThingWithComps offhandWeapon = null;
            if (pawn.equipment.AllEquipmentListForReading.Count == 2)
            {
                offhandWeapon = (from weapon in pawn.equipment.AllEquipmentListForReading
                    where weapon != mainhandWeapon
                    select weapon).First();
                var offhandComp = offhandWeapon?.def.GetCompProperties<WhandCompProps>();
                if (offhandComp != null)
                {
                    OffHand = offhandComp.MainHand;
                }
            }

            if (pawn.stances.curStance is Stance_Busy {neverAimWeapon: false} stance_Busy &&
                stance_Busy.focusTarg.IsValid)
            {
                var a = stance_Busy.focusTarg.HasThing
                    ? stance_Busy.focusTarg.Thing.DrawPos
                    : stance_Busy.focusTarg.Cell.ToVector3Shifted();

                var num = 0f;
                if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                {
                    num = (a - pawn.DrawPos).AngleFlat();
                }

                DrawHands(mainhandWeapon, num, offhandWeapon, false, true);
                return;
            }

            var baseType = pawn.Drawer.renderer.GetType();
            var methodInfo = baseType.GetMethod("CarryWeaponOpenly", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = methodInfo?.Invoke(pawn.Drawer.renderer, null);
            if (result == null || !(bool) result)
            {
                return;
            }

            if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
            {
                DrawHands(mainhandWeapon, 143f, offhandWeapon, true);
                return;
            }

            if (pawn.Rotation == Rot4.East)
            {
                DrawHands(mainhandWeapon, 143f, offhandWeapon);
                return;
            }

            if (pawn.Rotation != Rot4.West)
            {
                return;
            }

            DrawHands(mainhandWeapon, 217f, offhandWeapon);
        }


        private void DrawHands(Thing mainHandWeapon, float aimAngle, Thing offHandWeapon = null, bool idle = false,
            bool aiming = false)
        {
            var flipped = false;
            if (!(parent is Pawn pawn))
            {
                return;
            }

            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        mainHandWeapon.def.GetHashCode());
                }

                return;
            }

            var mainWeaponLocation = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item1;
            var mainHandAngle = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2;
            var offhandWeaponLocation = Vector3.zero;
            var offHandAngle = mainHandAngle;
            var mainMeleeExtra = 0f;
            var offMeleeExtra = 0f;
            var mainMelee = false;
            var offMelee = false;
            if (offHandWeapon != null)
            {
                if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(offHandWeapon))
                {
                    if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                    {
                        Log.ErrorOnce(
                            $"[ShowMeYourHands]: Could not find the position for {offHandWeapon.def.label} from the mod {offHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                            offHandWeapon.def.GetHashCode());
                    }
                }
                else
                {
                    offhandWeaponLocation = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item1;
                    offHandAngle = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item2;
                }
            }

            mainHandAngle = mainHandAngle - 90f;
            offHandAngle = offHandAngle - 90f;
            if (pawn.Rotation == Rot4.West || aimAngle is > 200f and < 340f)
            {
                flipped = true;
            }

            if (mainHandWeapon.def.IsMeleeWeapon)
            {
                mainMelee = true;
                mainMeleeExtra = 0.0001f;
                if (idle && offHandWeapon != null) //Dual wield idle vertical
                {
                    if (pawn.Rotation == Rot4.South)
                    {
                        mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                    }
                }
                else
                {
                    if (flipped)
                    {
                        mainHandAngle -= 180f;
                        mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                    }
                }
            }
            else
            {
                if (flipped)
                {
                    mainHandAngle -= 180f;
                }
            }

            if (offHandWeapon?.def.IsMeleeWeapon == true)
            {
                offMelee = true;
                offMeleeExtra = 0.0001f;
                if (idle && pawn.Rotation == Rot4.North) //Dual wield north
                {
                    offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    if (flipped)
                    {
                        offHandAngle -= 180f;
                        offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        offHandAngle += offHandWeapon.def.equippedAngleOffset;
                    }
                }
            }
            else
            {
                if (flipped)
                {
                    offHandAngle -= 180f;
                }
            }

            mainHandAngle %= 360f;
            offHandAngle %= 360f;

            var unused = HandColor;

            if (!mainHandGraphics.ContainsKey(pawn) || !offHandGraphics.ContainsKey(pawn))
            {
                return;
            }

            var mainHandTex = mainHandGraphics[pawn];
            var offHandTex = offHandGraphics[pawn];


            if (mainHandTex == null || offHandTex == null)
            {
                return;
            }

            var matSingle = mainHandTex.MatSingle;
            var offSingle = offHandTex.MatSingle;
            var drawSize = 1f;

            if (ShowMeYourHandsMod.instance.Settings.RepositionHands && mainHandWeapon.def.graphicData != null &&
                mainHandWeapon.def?.graphicData?.drawSize.x != 1f)
            {
                drawSize = mainHandWeapon.def.graphicData.drawSize.x;
            }

            if (!pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
            {
                var bodySize = 1f;
                if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
                {
                    if (pawn.RaceProps != null)
                    {
                        bodySize = pawn.RaceProps.baseBodySize;
                    }

                    if (ShowMeYourHandsMain.BabysAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                    {
                        bodySize = (float) ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] {pawn});
                    }
                }

                pawnBodySizes[pawn] = 0.8f * bodySize;
            }

            var mesh = MeshMakerPlanes.NewPlaneMesh(pawnBodySizes[pawn], flipped);

            if (MainHand != Vector3.zero)
            {
                var x = MainHand.x * drawSize;
                var z = MainHand.z * drawSize;
                var y = MainHand.y < 0 ? -0.0001f : 0.0001f;

                if (flipped)
                {
                    x *= -1;
                }

                if (pawn.Rotation == Rot4.North && !mainMelee && !aiming)
                {
                    z += 0.1f;
                }

                if (ShowMeYourHandsMain.OversizedWeaponLoaded)
                {
                    mainWeaponLocation += AdjustRenderOffsetFromDir(pawn, mainHandWeapon as ThingWithComps);
                }

                Graphics.DrawMesh(mesh,
                    mainWeaponLocation + new Vector3(x, y + mainMeleeExtra, z).RotatedBy(mainHandAngle),
                    Quaternion.AngleAxis(mainHandAngle, Vector3.up), y >= 0 ? matSingle : offSingle, 0);
            }

            if (OffHand == Vector3.zero)
            {
                return;
            }

            var x2 = OffHand.x * drawSize;
            var z2 = OffHand.z * drawSize;
            var y2 = OffHand.y < 0 ? -0.0001f : 0.0001f;


            if (offHandWeapon != null)
            {
                drawSize = 1f;

                if (ShowMeYourHandsMod.instance.Settings.RepositionHands && offHandWeapon.def.graphicData != null &&
                    offHandWeapon.def?.graphicData?.drawSize.x != 1f)
                {
                    drawSize = offHandWeapon.def.graphicData.drawSize.x;
                }

                x2 = OffHand.x * drawSize;
                z2 = OffHand.z * drawSize;

                if (flipped)
                {
                    x2 *= -1;
                }

                if (idle && !offMelee)
                {
                    if (pawn.Rotation == Rot4.South)
                    {
                        z2 += 0.05f;
                    }
                    else
                    {
                        z2 -= 0.05f;
                    }
                }


                if (ShowMeYourHandsMain.OversizedWeaponLoaded)
                {
                    offhandWeaponLocation += AdjustRenderOffsetFromDir(pawn, offHandWeapon as ThingWithComps);
                }

                Graphics.DrawMesh(mesh,
                    offhandWeaponLocation + new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle),
                    Quaternion.AngleAxis(offHandAngle, Vector3.up),
                    y2 >= 0 ? matSingle : offSingle, 0);
                return;
            }

            if (flipped)
            {
                x2 *= -1;
            }

            Graphics.DrawMesh(mesh,
                mainWeaponLocation + new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(mainHandAngle),
                Quaternion.AngleAxis(mainHandAngle, Vector3.up), y2 >= 0 ? matSingle : offSingle, 0);
        }


        public override void PostDraw()
        {
            AngleCalc();
        }

        private Color getHandColor(Pawn pawn, out bool hasGloves)
        {
            hasGloves = false;
            if (!ShowMeYourHandsMod.instance.Settings.MatchArmorColor)
            {
                return pawn.story.SkinColor;
            }

            var handApparel = from apparel in pawn.apparel.WornApparel
                where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                select apparel;
            if (!handApparel.Any())
            {
                return pawn.story.SkinColor;
            }

            //ShowMeYourHandsMain.LogMessage($"Found gloves on {pawn.NameShortColored}: {string.Join(",", handApparel)}");

            Thing outerApparel = null;
            var highestDrawOrder = 0;
            foreach (var thing in handApparel)
            {
                var thingOutmostLayer =
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
                    AverageColorFromTexture((Texture2D) outerApparel.Graphic.MatSingle.mainTexture);
            }

            return colorDictionary[outerApparel];
        }


        private Color32 AverageColorFromTexture(Texture2D texture)
        {
            var renderTexture = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTexture);
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var tex = new Texture2D(texture.width, texture.height);
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return AverageColorFromColors(tex.GetPixels32());
        }

        private Color32 AverageColorFromColors(Color32[] colors)
        {
            var shadeDictionary = new Dictionary<Color32, int>();
            foreach (var texColor in colors)
            {
                if (texColor.a < 50)
                {
                    // Ignore low transparency
                    continue;
                }

                var currentRgb = new Rgb {B = texColor.b, G = texColor.b, R = texColor.r};

                if (currentRgb.Compare(new Rgb {B = 0, G = 0, R = 0}, new Cie1976Comparison()) < 2)
                {
                    // Ignore black pixels
                    continue;
                }

                if (shadeDictionary.Count == 0)
                {
                    shadeDictionary[texColor] = 1;
                    continue;
                }


                var added = false;
                foreach (var rgb in shadeDictionary.Keys.Where(rgb =>
                    currentRgb.Compare(new Rgb {B = rgb.b, G = rgb.b, R = rgb.r}, new Cie1976Comparison()) < 2))
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

            var greatestValue = shadeDictionary.Aggregate((rgb, max) => rgb.Value > max.Value ? rgb : max).Key;
            greatestValue.a = MaxValue;
            return greatestValue;
        }

        private static Vector3 AdjustRenderOffsetFromDir(Pawn pawn, ThingWithComps weapon)
        {
            var thingComp = weapon.AllComps.FirstOrDefault(y => y.GetType().ToString().Contains("CompOversizedWeapon"));
            if (thingComp == null)
            {
                return Vector3.zero;
            }

            var props = Traverse.Create(thingComp).Property("Props");
            if (props == null)
            {
                return Vector3.zero;
            }

            Traverse offset = null;

            if (pawn.Rotation == Rot4.North)
            {
                offset = props.Field("northOffset");
            }

            if (pawn.Rotation == Rot4.East)
            {
                offset = props.Field("eastOffset");
            }

            if (pawn.Rotation == Rot4.South)
            {
                offset = props.Field("southOffset");
            }

            if (pawn.Rotation == Rot4.West)
            {
                offset = props.Field("westOffset");
            }

            var value = offset?.GetValue<Vector3>();
            return value ?? Vector3.zero;
        }
    }
}