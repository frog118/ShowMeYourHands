using System;
using System.Collections.Generic;
using System.Linq;
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

        private static readonly Vector2 handSize = new Vector2(24f, 24f);

        private static readonly int buttonSpacer = 250;

        private static readonly float columnSpacer = 0.1f;

        private static float leftSideWidth;

        private static Listing_Standard listing_Standard;

        private static string selectedDef = "Settings";

        private static Vector3 currentMainHand;

        private static Vector3 currentOffHand;

        private static bool currentHasOffHand;

        private static bool currentMainBehind;

        private static bool currentOffBehind;

        private static Vector2 tabsScrollPosition;

        private static List<ThingDef> allWeapons;

        private static string currentVersion;

        private static Graphic handTex;

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
                        where weapon.IsWeapon && !weapon.destroyOnDrop && !weapon.menuHidden
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

            switch (selectedDef)
            {
                case null:
                    return;
                case "Settings":
                {
                    listing_Standard.Begin(frameRect);
                    listing_Standard.Gap();
                    if (Prefs.UIScale != 1f)
                    {
                        GUI.color = Color.yellow;
                        listing_Standard.Label(
                            "INFO \nUI-scale is not set to 1.0x. The settings GUI will look strange for some weapons.",
                            -1F,
                            "Some weapons need to be rotated as their texture dose not have the default angle. The rotation-function has issues when the UI-scale is set to anything else than 1.0x. It does not need to be changed for the settings to have effect.");
                        listing_Standard.Gap();
                        GUI.color = Color.white;
                    }

                    var labelPoint = listing_Standard.Label("Manual config reset", -1F,
                        "Reset all defined hand-settings");
                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("Enable verbose logging", ref Settings.VerboseLogging,
                        "Shows verbose logging, for finding errors");
                    DrawButton(instance.Settings.ResetManualValues, "Reset all",
                        new Vector2(labelPoint.position.x + buttonSpacer, labelPoint.position.y));
                    if (currentVersion != null)
                    {
                        listing_Standard.Gap();
                        GUI.contentColor = Color.gray;
                        listing_Standard.Label("Installed mod-version: " + currentVersion);
                        GUI.contentColor = Color.white;
                    }

                    listing_Standard.End();
                    break;
                }

                default:
                {
                    var currentDef = DefDatabase<ThingDef>.GetNamedSilentFail(selectedDef);
                    listing_Standard.Begin(frameRect);
                    if (currentDef == null)
                    {
                        listing_Standard.Label($"Error, could not find {selectedDef} in the database");
                        listing_Standard.End();
                        break;
                    }

                    var compProperties = currentDef.GetCompProperties<WhandCompProps>();
                    if (compProperties == null)
                    {
                        listing_Standard.Label($"Error, could not find hand-info of {selectedDef} in the database");
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
                        listing_Standard.Label($"Error, could not find hand-info of {selectedDef} in the database");
                        listing_Standard.End();
                        break;
                    }

                    listing_Standard.Gap(20);
                    listing_Standard.CheckboxLabeled("Uses two hands", ref currentHasOffHand);
                    listing_Standard.GapLine();
                    listing_Standard.ColumnWidth = 230;
                    listing_Standard.Label("Main hand horizontal");
                    currentMainHand.x = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                        currentMainHand.x, -0.5f, 0.5f, false,
                        currentMainHand.x.ToString(), null, null, 0.001f);
                    var lastMainLabel = listing_Standard.Label("Main hand vertical");
                    currentMainHand.z = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                        currentMainHand.z, -0.5f, 0.5f, false,
                        currentMainHand.z.ToString(), null, null, 0.001f);
                    listing_Standard.Gap();
                    listing_Standard.CheckboxLabeled("Render behind weapon", ref currentMainBehind);

                    if (currentHasOffHand)
                    {
                        listing_Standard.NewColumn();
                        listing_Standard.Gap(212);
                        listing_Standard.Label("Off hand horizontal");
                        currentOffHand.x = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                            currentOffHand.x, -0.5f, 0.5f, false,
                            currentOffHand.x.ToString(), null, null, 0.001f);
                        listing_Standard.Label("Off hand vertical");
                        currentOffHand.z = Widgets.HorizontalSlider(listing_Standard.GetRect(20),
                            currentOffHand.z, -0.5f, 0.5f, false,
                            currentOffHand.z.ToString(), null, null, 0.001f);
                        listing_Standard.Gap();
                        listing_Standard.CheckboxLabeled("Render behind weapon", ref currentOffBehind);
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
                        }, "Reset", lastMainLabel.position + new Vector2(350, 230));
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
                        }, "Save", lastMainLabel.position + new Vector2(25, 230));
                    }


                    listing_Standard.End();
                    break;
                }
            }
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
            tabContentRect.height = (AllWeapons.Count * 22f) + 15;
            listing_Standard.BeginScrollView(tabFrameRect, ref tabsScrollPosition, ref tabContentRect);
            Text.Font = GameFont.Tiny;
            if (listing_Standard.ListItemSelectable("Settings", Color.yellow, selectedDef == "Settings"))
            {
                selectedDef = selectedDef == "Settings" ? null : "Settings";
            }

            listing_Standard.ListItemSelectable(null, Color.yellow);
            foreach (var thingDef in AllWeapons)
            {
                if (!listing_Standard.ListItemSelectable(
                    thingDef.label.CapitalizeFirst(), Color.yellow,
                    selectedDef == thingDef.defName))
                {
                    continue;
                }

                selectedDef = selectedDef == thingDef.defName ? null : thingDef.defName;
                currentMainHand = Vector3.zero;
                currentOffHand = Vector3.zero;
            }

            Text.Font = GameFont.Small;
            listing_Standard.EndScrollView(ref tabContentRect);
        }
    }
}