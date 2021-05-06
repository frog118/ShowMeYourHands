using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShowMeYourHands
{
    [StaticConstructorOnStartup]
    internal class ShowMeYourHandsMod : Mod
    {
        private const float weaponMiddle = 49f;

        /// <summary>
        ///     The instance of the settings to be read by the mod
        /// </summary>
        public static ShowMeYourHandsMod instance;

        private static readonly Vector2 buttonSize = new Vector2(120f, 25f);

        private static readonly Vector2 weaponSize = new Vector2(105f, 105f);

        private static readonly Vector2 iconSize = new Vector2(24f, 24f);

        private static readonly Vector2 handSize = new Vector2(24f, 24f);

        private static readonly int buttonSpacer = 250;

        private static readonly float columnSpacer = 0.1f;

        private static float leftSideWidth;

        private static Listing_Standard listing_Standard;

        private static Vector3 currentMainHand;

        private static Vector3 currentOffHand;

        private static bool currentHasOffHand;

        private static bool currentMainBehind;

        private static bool currentOffBehind;

        private static Vector2 tabsScrollPosition;

        private static Vector2 summaryScrollPosition;

        private static List<ThingDef> allWeapons;

        private static string currentVersion;

        private static Graphic handTex;

        private static Dictionary<string, int> totalWeaponsByMod;

        private static Dictionary<string, int> fixedWeaponsByMod;

        public static List<string> DefinedByDef;

        private static string selectedDef = "Settings";

        private static string selectedSubDef;

        /// <summary>
        ///     The private settings
        /// </summary>
        private ShowMeYourHandsModSettings settings;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="content"></param>
        public ShowMeYourHandsMod(ModContentPack content)
            : base(content)
        {
            instance = this;
            ParseHelper.Parsers<SaveableVector3>.Register(SaveableVector3.FromString);
            if (instance.Settings.ManualMainHandPositions == null)
            {
                instance.Settings.ManualMainHandPositions = new Dictionary<string, SaveableVector3>();
                instance.Settings.ManualOffHandPositions = new Dictionary<string, SaveableVector3>();
            }

            currentVersion =
                VersionFromManifest.GetVersionFromModMetaData(
                    ModLister.GetActiveModWithIdentifier("Mlie.ShowMeYourHands"));
        }

        private static string SelectedDef
        {
            get => selectedDef;
            set
            {
                if (value == "Summary")
                {
                    UpdateWeaponStatistics();
                }

                selectedDef = value;
            }
        }

        /// <summary>
        ///     The instance-settings for the mod
        /// </summary>
        internal ShowMeYourHandsModSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = GetSettings<ShowMeYourHandsModSettings>();
                }

                return settings;
            }

            set => settings = value;
        }

        private static List<ThingDef> AllWeapons
        {
            get
            {
                if (allWeapons == null || allWeapons.Count == 0)
                {
                    allWeapons = (from weapon in DefDatabase<ThingDef>.AllDefsListForReading
                        where weapon.IsWeapon && !weapon.destroyOnDrop && !weapon.menuHidden && !IsShield(weapon)
                        orderby weapon.label
                        select weapon).ToList();
                }

                return allWeapons;
            }
            set => allWeapons = value;
        }

        private Graphic HandTex
        {
            get
            {
                if (handTex == null)
                {
                    handTex = GraphicDatabase.Get<Graphic_Multi>("HandIcon", ShaderDatabase.CutoutSkin,
                        new Vector2(1f, 1f),
                        PawnSkinColors.GetSkinColor(0.5f), PawnSkinColors.GetSkinColor(0.5f));
                }

                return handTex;
            }
            set => handTex = value;
        }

        /// <summary>
        ///     The settings-window
        /// </summary>
        /// <param name="rect"></param>
        public override void DoSettingsWindowContents(Rect rect)
        {
            base.DoSettingsWindowContents(rect);

            var rect2 = rect.ContractedBy(1);
            leftSideWidth = rect2.ContractedBy(10).width / 5 * 2;

            listing_Standard = new Listing_Standard();

            DrawOptions(rect2);
            DrawTabsList(rect2);
            Settings.Write();
        }

        /// <summary>
        ///     The title for the mod-settings
        /// </summary>
        /// <returns></returns>
        public override string SettingsCategory()
        {
            return "Show Me Your Hands";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            RimWorld_MainMenuDrawer_MainMenuOnGUI.UpdateHandDefinitions();
        }

        public static bool IsShield(ThingDef weapon)
        {
            var isShield = false;
            if (weapon.weaponTags == null)
            {
                return false;
            }

            foreach (var tag in weapon.weaponTags)
            {
                if (tag == "Shield_Sidearm")
                {
                    continue;
                }

                if (tag == "Shield_NoSidearm")
                {
                    continue;
                }

                if (tag.Contains("_ValidSidearm"))
                {
                    continue;
                }

                if (tag.Contains("ShieldSafe"))
                {
                    continue;
                }

                if (!tag.ToLower().Contains("shield"))
                {
                    continue;
                }

                isShield = true;
            }

            return isShield;
        }

        private static void DrawButton(Action action, string text, Vector2 pos)
        {
            var rect = new Rect(pos.x, pos.y, buttonSize.x, buttonSize.y);
            if (!Widgets.ButtonText(rect, text, true, false, Color.white))
            {
                return;
            }

            SoundDefOf.Designate_DragStandard_Changed.PlayOneShotOnCamera();
            action();
        }

        private static void UpdateWeaponStatistics()
        {
            totalWeaponsByMod = new Dictionary<string, int>();
            fixedWeaponsByMod = new Dictionary<string, int>();
            foreach (var currentWeapon in allWeapons)
            {
                var weaponModName = currentWeapon.modContentPack?.Name;
                if (string.IsNullOrEmpty(weaponModName))
                {
                    weaponModName = "SMYH.unknown".Translate();
                }

                if (totalWeaponsByMod.ContainsKey(weaponModName))
                {
                    totalWeaponsByMod[weaponModName]++;
                }
                else
                {
                    totalWeaponsByMod[weaponModName] = 1;
                }

                if (!DefinedByDef.Contains(currentWeapon.defName) &&
                    !instance.Settings.ManualMainHandPositions.ContainsKey(currentWeapon.defName))
                {
                    continue;
                }

                if (fixedWeaponsByMod.ContainsKey(weaponModName))
                {
                    fixedWeaponsByMod[weaponModName]++;
                }
                else
                {
                    fixedWeaponsByMod[weaponModName] = 1;
                }
            }
        }

        private bool DrawIcon(ThingDef thing, Rect rect, Vector3 mainHandPosition, Vector3 offHandPosition)
        {
            Texture texture;
            if (thing?.graphicData?.graphicClass == typeof(Graphic_StackCount))
            {
                texture = (thing.graphicData?.Graphic as Graphic_StackCount)?.SubGraphicForStackCount(1, thing)
                    .MatSingle.mainTexture;
            }
            else
            {
                texture = thing?.graphicData?.Graphic?.MatSingle?.mainTexture;
            }

            if (texture == null)
            {
                return false;
            }

            Widgets.DrawBoxSolid(rect, Color.grey);
            var rectInner = rect.ContractedBy(2);
            Widgets.DrawBoxSolid(rectInner, new ColorInt(42, 43, 44).ToColor);
            var frameRect = rectInner.ContractedBy(3);

            var mainHandCoords = new Vector2(weaponMiddle + (mainHandPosition.x * 100) - (handSize.x / 2),
                weaponMiddle - (mainHandPosition.z * 100) - (handSize.y / 2));
            var mainHandRect = new Rect(frameRect.x + mainHandCoords.x, frameRect.y + mainHandCoords.y,
                handSize.x,
                handSize.y);
            var offHandCoords = new Vector2(weaponMiddle + (offHandPosition.x * 100) - (handSize.x / 2),
                weaponMiddle - (offHandPosition.z * 100) - (handSize.y / 2));
            var offHandRect = new Rect(frameRect.x + offHandCoords.x, frameRect.y + offHandCoords.y,
                handSize.x,
                handSize.y);

            if (currentMainBehind)
            {
                GUI.DrawTexture(mainHandRect, HandTex.MatSouth.mainTexture);
            }

            if (currentHasOffHand && currentOffBehind)
            {
                GUI.DrawTexture(offHandRect, HandTex.MatSouth.mainTexture);
            }

            if (thing.IsRangedWeapon)
            {
                DrawTextureRotatedLocal(frameRect, texture,
                    thing.equippedAngleOffset);
            }
            else
            {
                GUI.DrawTexture(frameRect, texture);
            }

            if (!currentMainBehind)
            {
                GUI.DrawTexture(mainHandRect, HandTex.MatSouth.mainTexture);
            }

            if (currentHasOffHand && !currentOffBehind)
            {
                GUI.DrawTexture(offHandRect, HandTex.MatSouth.mainTexture);
            }

            return true;
        }

        private void DrawTextureRotatedLocal(Rect rect, Texture texture, float angle)
        {
            if (angle == 0f)
            {
                GUI.DrawTexture(rect, texture);
                return;
            }

            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, rect.center);
            GUI.DrawTexture(rect, texture);
            GUI.matrix = matrix;
        }

        private void DrawWeapon(ThingDef thing, Rect rect)
        {
            if (thing?.graphicData?.Graphic?.MatSingle?.mainTexture == null)
            {
                return;
            }

            var texture2D = thing.graphicData.Graphic.MatSingle.mainTexture;
            if (texture2D.width != texture2D.height)
            {
                var ratio = (float) texture2D.width / texture2D.height;

                if (ratio < 1)
                {
                    rect.x += (rect.width - (rect.width * ratio)) / 2;
                    rect.width *= ratio;
                }
                else
                {
                    rect.y += (rect.height - (rect.height / ratio)) / 2;
                    rect.height /= ratio;
                }
            }

            GUI.DrawTexture(rect, texture2D);
        }

        private void DrawOptions(Rect rect)
        {
            var optionsOuterContainer = rect.ContractedBy(10);
            optionsOuterContainer.x += leftSideWidth + columnSpacer;
            optionsOuterContainer.width -= leftSideWidth + columnSpacer;
            Widgets.DrawBoxSolid(optionsOuterContainer, Color.grey);
            var optionsInnerContainer = optionsOuterContainer.ContractedBy(1);
            Widgets.DrawBoxSolid(optionsInnerContainer, new ColorInt(42, 43, 44).ToColor);
            var frameRect = optionsInnerContainer.ContractedBy(10);
            frameRect.x = leftSideWidth + columnSpacer + 20;
            frameRect.y += 15;
            frameRect.height -= 15;
            var contentRect = frameRect;
            contentRect.x = 0;
            contentRect.y = 0;

            switch (SelectedDef)
            {
                case null:
                    return;
                case "Settings":
                {
                    listing_Standard.Begin(frameRect);
                    Text.Font = GameFont.Medium;
                    listing_Standard.Label("SMYH.settings".Translate());
                    Text.Font = GameFont.Small;
                    listing_Standard.Gap();
                    if (Prefs.UIScale != 1f)
                    {
                        GUI.color = Color.yellow;
                        listing_Standard.Label(
                            "SMYH.uiscale.label".Translate(),
                            -1F,
                            "SMYH.uiscale.tooltip".Translate());
                        listing_Standard.Gap();
                        GUI.color = Color.white;
                    }

                    if (instance.Settings.ManualMainHandPositions?.Count > 0)
                    {
                        var copyPoint = listing_Standard.Label("SMYH.copy.label".Translate(), -1F,
                            "SMYH.copy.tooltip".Translate());
                        DrawButton(CopyChangedWeapons, "SMYH.copy.button".Translate(),
                            new Vector2(copyPoint.position.x + buttonSpacer, copyPoint.position.y));
                    }

                    var labelPoint = listing_Standard.Label("SMYH.resetall.label".Translate(), -1F,
                        "SMYH.resetall.tooltip".Translate());
                    DrawButton(instance.Settings.ResetManualValues, "SMYH.resetall.button".Translate(),
                        new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("SMYH.logging.label".Translate(), ref Settings.VerboseLogging,
                        "SMYH.logging.tooltip".Translate());
                    if (currentVersion != null)
                    {
                        listing_Standard.Gap();
                        GUI.contentColor = Color.gray;
                        listing_Standard.Label("SMYH.version.label".Translate(currentVersion));
                        GUI.contentColor = Color.white;
                    }

                    listing_Standard.End();
                    break;
                }
                case "Summary":
                {
                    var tabFrameRect = frameRect;
                    var tabContentRect = tabFrameRect;
                    tabContentRect.x = 0;
                    tabContentRect.y = 0;
                    tabContentRect.height = (totalWeaponsByMod.Count * 25f) + 15;
                    listing_Standard.BeginScrollView(tabFrameRect, ref summaryScrollPosition, ref tabContentRect);
                    Text.Font = GameFont.Medium;
                    listing_Standard.Label("SMYH.summary".Translate());
                    Text.Font = GameFont.Small;
                    listing_Standard.Gap();
                    foreach (var keyValuePair in totalWeaponsByMod)
                    {
                        var fixedWeapons = 0;
                        if (fixedWeaponsByMod.ContainsKey(keyValuePair.Key))
                        {
                            fixedWeapons = fixedWeaponsByMod[keyValuePair.Key];
                        }

                        var percent = fixedWeapons / (decimal) keyValuePair.Value * 100;

                        GUI.color = GetColorFromPercent(percent);

                        if (listing_Standard.ListItemSelectable(
                            $"{keyValuePair.Key} {Math.Round(percent)}% ({fixedWeapons}/{keyValuePair.Value})",
                            Color.yellow,
                            out _,
                            selectedSubDef == keyValuePair.Key))
                        {
                            selectedSubDef = selectedSubDef == keyValuePair.Key ? null : keyValuePair.Key;
                        }

                        GUI.color = Color.white;
                    }

                    listing_Standard.EndScrollView(ref tabContentRect);
                    break;
                }

                default:
                {
                    var currentDef = DefDatabase<ThingDef>.GetNamedSilentFail(SelectedDef);
                    listing_Standard.Begin(frameRect);
                    if (currentDef == null)
                    {
                        listing_Standard.Label("SMYH.error.weapon".Translate(SelectedDef));
                        listing_Standard.End();
                        break;
                    }

                    var compProperties = currentDef.GetCompProperties<WhandCompProps>();
                    if (compProperties == null)
                    {
                        listing_Standard.Label("SMYH.error.hands".Translate(SelectedDef));
                        listing_Standard.End();
                        break;
                    }

                    Text.Font = GameFont.Medium;
                    var labelPoint = listing_Standard.Label(currentDef.label.CapitalizeFirst(), -1F,
                        currentDef.defName);
                    Text.Font = GameFont.Small;
                    var modName = currentDef.modContentPack?.Name;
                    var modId = currentDef.modContentPack?.PackageId;
                    if (currentDef.modContentPack != null)
                    {
                        listing_Standard.Label($"{modName}", -1F, modId);
                    }
                    else
                    {
                        listing_Standard.Gap();
                    }

                    var description = currentDef.description;
                    if (!string.IsNullOrEmpty(description))
                    {
                        if (description.Length > 200)
                        {
                            description = description.Substring(0, 200) + "...";
                        }

                        Widgets.Label(new Rect(labelPoint.x, labelPoint.y + 50, 310, 110), description);
                    }

                    listing_Standard.Gap(100);

                    var weaponRect = new Rect(labelPoint.x + 350, labelPoint.y + 25, weaponSize.x, weaponSize.y);

                    if (currentMainHand == Vector3.zero)
                    {
                        currentMainHand = compProperties.MainHand;
                        currentOffHand = compProperties.SecHand;
                        currentHasOffHand = currentOffHand != Vector3.zero;
                        currentMainBehind = compProperties.MainHand.y < 0;
                        currentOffBehind = compProperties.SecHand.y < 0;
                    }

                    if (!DrawIcon(currentDef, weaponRect, currentMainHand, currentOffHand))
                    {
                        listing_Standard.Label("SMYH.error.texture".Translate(SelectedDef));
                        listing_Standard.End();
                        break;
                    }

                    listing_Standard.Gap(20);
                    listing_Standard.CheckboxLabeled("SMYH.twohands.label".Translate(), ref currentHasOffHand);
                    listing_Standard.GapLine();
                    listing_Standard.ColumnWidth = 230;
                    listing_Standard.Label("SMYH.mainhandhorizontal.label".Translate());
                    currentMainHand.x = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                        currentMainHand.x, -0.5f, 0.5f, false,
                        currentMainHand.x.ToString(), null, null, 0.001f);
                    var lastMainLabel = listing_Standard.Label("SMYH.mainhandvertical.label".Translate());
                    currentMainHand.z = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                        currentMainHand.z, -0.5f, 0.5f, false,
                        currentMainHand.z.ToString(), null, null, 0.001f);
                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("SMYH.renderbehind.label".Translate(), ref currentMainBehind);

                    if (currentHasOffHand)
                    {
                        listing_Standard.NewColumn();
                        listing_Standard.Gap(212);
                        listing_Standard.Label("SMYH.offhandhorizontal.label".Translate());
                        currentOffHand.x = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                            currentOffHand.x, -0.5f, 0.5f, false,
                            currentOffHand.x.ToString(), null, null, 0.001f);
                        listing_Standard.Label("SMYH.offhandvertical.label".Translate());
                        currentOffHand.z = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                            currentOffHand.z, -0.5f, 0.5f, false,
                            currentOffHand.z.ToString(), null, null, 0.001f);
                        listing_Standard.Gap();
                        listing_Standard.CheckboxLabeled("SMYH.renderbehind.label".Translate(), ref currentOffBehind);
                    }

                    if (currentMainHand != compProperties.MainHand ||
                        currentOffHand != compProperties.SecHand ||
                        currentHasOffHand != (currentOffHand != Vector3.zero) ||
                        currentMainBehind != compProperties.MainHand.y < 0 ||
                        currentOffBehind != compProperties.SecHand.y < 0)
                    {
                        DrawButton(() =>
                        {
                            currentMainHand = compProperties.MainHand;
                            currentOffHand = compProperties.SecHand;
                            currentHasOffHand = currentOffHand != Vector3.zero;
                            currentMainBehind = compProperties.MainHand.y < 0;
                            currentOffBehind = compProperties.SecHand.y < 0;
                        }, "SMYH.reset.button".Translate(), lastMainLabel.position + new Vector2(350, 230));
                        DrawButton(() =>
                        {
                            currentMainHand.y = currentMainBehind ? -0.1f : 0.1f;
                            currentOffHand.y = currentOffBehind ? -0.1f : 0.1f;
                            if (!currentHasOffHand)
                            {
                                currentOffHand = Vector3.zero;
                            }

                            compProperties.MainHand = currentMainHand;
                            compProperties.SecHand = currentOffHand;
                            instance.Settings.ManualMainHandPositions[currentDef.defName] =
                                new SaveableVector3(compProperties.MainHand);
                            instance.Settings.ManualOffHandPositions[currentDef.defName] =
                                new SaveableVector3(compProperties.SecHand);
                        }, "SMYH.save.button".Translate(), lastMainLabel.position + new Vector2(25, 230));
                    }


                    listing_Standard.End();
                    break;
                }
            }
        }

        private void CopyChangedWeapons()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            stringBuilder.AppendLine("<Defs>");
            stringBuilder.AppendLine("  <WHands.ClutterHandsTDef>");
            stringBuilder.AppendLine(
                $"     <defName>ClutterHandsSettings_{SystemInfo.deviceName.GetHashCode()}</defName>");
            stringBuilder.AppendLine("      <label>Weapon hand settings</label>");
            stringBuilder.AppendLine("      <thingClass>Thing</thingClass>");
            stringBuilder.AppendLine("      <WeaponCompLoader>");

            foreach (var settingsManualMainHandPosition in instance.Settings.ManualMainHandPositions)
            {
                stringBuilder.AppendLine("          <li>");
                stringBuilder.AppendLine($"              <MainHand>{settingsManualMainHandPosition.Value}</MainHand>");
                if (instance.Settings.ManualOffHandPositions.ContainsKey(settingsManualMainHandPosition.Key))
                {
                    var secHand = instance.Settings.ManualOffHandPositions[settingsManualMainHandPosition.Key];
                    if (secHand.ToVector3() != Vector3.zero)
                    {
                        stringBuilder.AppendLine($"              <SecHand>{secHand}</SecHand>");
                    }
                }

                stringBuilder.AppendLine("              <ThingTargets>");
                stringBuilder.AppendLine($"                 <li>{settingsManualMainHandPosition.Key}</li>");
                stringBuilder.AppendLine("              </ThingTargets>");
                stringBuilder.AppendLine("          </li>");
            }

            stringBuilder.AppendLine("      </WeaponCompLoader>");
            stringBuilder.AppendLine("  </WHands.ClutterHandsTDef>");
            stringBuilder.AppendLine("</Defs>");

            GUIUtility.systemCopyBuffer = stringBuilder.ToString();
            Messages.Message("Modified data copied to clipboard.", MessageTypeDefOf.SituationResolved, false);
        }

        private void DrawTabsList(Rect rect)
        {
            var scrollContainer = rect.ContractedBy(10);
            scrollContainer.width = leftSideWidth;
            Widgets.DrawBoxSolid(scrollContainer, Color.grey);
            var innerContainer = scrollContainer.ContractedBy(1);
            Widgets.DrawBoxSolid(innerContainer, new ColorInt(42, 43, 44).ToColor);
            var tabFrameRect = innerContainer.ContractedBy(5);
            tabFrameRect.y += 15;
            tabFrameRect.height -= 15;
            var tabContentRect = tabFrameRect;
            tabContentRect.x = 0;
            tabContentRect.y = 0;
            tabContentRect.width -= 20;
            tabContentRect.height = (AllWeapons.Count * 25f) + 48;
            listing_Standard.BeginScrollView(tabFrameRect, ref tabsScrollPosition, ref tabContentRect);
            //Text.Font = GameFont.Tiny;
            if (listing_Standard.ListItemSelectable("SMYH.settings".Translate(), Color.yellow,
                out _, SelectedDef == "Settings"))
            {
                SelectedDef = SelectedDef == "Settings" ? null : "Settings";
            }

            if (listing_Standard.ListItemSelectable("SMYH.summary".Translate(), Color.yellow,
                out _, SelectedDef == "Summary"))
            {
                SelectedDef = SelectedDef == "Summary" ? null : "Summary";
            }

            listing_Standard.ListItemSelectable(null, Color.yellow, out _);
            foreach (var thingDef in AllWeapons)
            {
                if (!DefinedByDef.Contains(thingDef.defName) &&
                    !instance.Settings.ManualMainHandPositions.ContainsKey(thingDef.defName))
                {
                    GUI.color = Color.red;
                }
                else
                {
                    GUI.color = Color.green;
                }

                if (listing_Standard.ListItemSelectable(thingDef.label.CapitalizeFirst(), Color.yellow,
                    out var position,
                    SelectedDef == thingDef.defName, thingDef.modContentPack?.Name == selectedSubDef))
                {
                    SelectedDef = SelectedDef == thingDef.defName ? null : thingDef.defName;
                    currentMainHand = Vector3.zero;
                    currentOffHand = Vector3.zero;
                }

                GUI.color = Color.white;
                position.x = position.x + tabContentRect.width - iconSize.x;
                DrawWeapon(thingDef, new Rect(position, iconSize));
            }

            //Text.Font = GameFont.Small;
            listing_Standard.EndScrollView(ref tabContentRect);
        }

        private Color GetColorFromPercent(decimal percent)
        {
            switch (percent)
            {
                case < 25:
                    return Color.red;
                case < 50:
                    return Color.yellow;
                case < 75:
                    return Color.white;
                case >= 75:
                    return Color.green;
            }
        }
    }
}