using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WHands;

namespace ShowMeYourHands
{
    [HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
    public static class RimWorld_MainMenuDrawer_MainMenuOnGUI
    {
        private static bool alreadyRun;

        private static List<ThingDef> doneWeapons = new List<ThingDef>();

        [HarmonyPostfix]
        public static void MainMenuOnGUI()
        {
            if (alreadyRun)
            {
                return;
            }

            alreadyRun = true;

            UpdateHandDefinitions();
        }

        public static void UpdateHandDefinitions()
        {
            doneWeapons = new List<ThingDef>();
            LoadFromSettings();
            LoadFromDefs();
            FigureOutTheRest();
            ShowMeYourHandsMain.LogMessage($"Defined hand definitions of {doneWeapons.Count} weapons", true);
        }

        private static void FigureOutTheRest()
        {
            foreach (var weapon in from weapon in DefDatabase<ThingDef>.AllDefsListForReading
                where weapon.IsWeapon && !weapon.destroyOnDrop && !weapon.menuHidden &&
                      !doneWeapons.Contains(weapon)
                select weapon)
            {
                var isShield = false;
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

                if (isShield)
                {
                    ShowMeYourHandsMain.LogMessage($"Ignoring {weapon.defName} since its probably a shield");
                    continue;
                }

                var compProps = weapon.GetCompProperties<WhandCompProps>();
                if (compProps == null)
                {
                    compProps = new WhandCompProps
                    {
                        compClass = typeof(WhandComp)
                    };
                    if (weapon.IsMeleeWeapon)
                    {
                        compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
                    }
                    else
                    {
                        compProps.SecHand = IsWeaponLong(weapon, out var mainHand, out var secHand)
                            ? secHand
                            : Vector3.zero;
                        compProps.MainHand = mainHand;
                    }

                    weapon.comps.Add(compProps);
                }
                else
                {
                    if (weapon.IsMeleeWeapon)
                    {
                        compProps.MainHand = new Vector3(-0.25f, 0.3f, 0);
                    }
                    else
                    {
                        compProps.SecHand = IsWeaponLong(weapon, out var mainHand, out var secHand)
                            ? secHand
                            : Vector3.zero;
                        compProps.MainHand = mainHand;
                    }
                }

                doneWeapons.Add(weapon);
            }
        }

        private static void LoadFromSettings()
        {
            if (ShowMeYourHandsMod.instance.Settings.ManualMainHandPositions == null)
            {
                return;
            }

            foreach (var keyValuePair in ShowMeYourHandsMod.instance?.Settings?.ManualMainHandPositions)
            {
                var weapon = DefDatabase<ThingDef>.GetNamedSilentFail(keyValuePair.Key);
                if (weapon == null)
                {
                    continue;
                }

                var compProps = weapon.GetCompProperties<WhandCompProps>();
                if (compProps == null)
                {
                    compProps = new WhandCompProps

                    {
                        compClass = typeof(WhandComp),
                        MainHand = keyValuePair.Value.ToVector3(),
                        SecHand =
                            ShowMeYourHandsMod.instance?.Settings?.ManualOffHandPositions
                                .ContainsKey(keyValuePair.Key) == true
                                ? ShowMeYourHandsMod.instance.Settings.ManualOffHandPositions[keyValuePair.Key]
                                    .ToVector3()
                                : Vector3.zero
                    };
                    weapon.comps.Add(compProps);
                }
                else
                {
                    compProps.MainHand = keyValuePair.Value.ToVector3();
                    compProps.SecHand =
                        ShowMeYourHandsMod.instance?.Settings?.ManualOffHandPositions.ContainsKey(keyValuePair.Key) ==
                        true
                            ? ShowMeYourHandsMod.instance.Settings.ManualOffHandPositions[keyValuePair.Key].ToVector3()
                            : Vector3.zero;
                }

                doneWeapons.Add(weapon);
            }
        }

        private static void LoadFromDefs()
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("ClutterHandsSettings");

            if (def is not ClutterHandsTDef clutterHandsTDef)
            {
                return;
            }

            if (clutterHandsTDef.WeaponCompLoader.Count <= 0)
            {
                return;
            }

            foreach (var weaponSets in clutterHandsTDef.WeaponCompLoader)
            {
                if (weaponSets.ThingTargets.Count <= 0)
                {
                    continue;
                }

                foreach (var weaponDefName in weaponSets.ThingTargets)
                {
                    var weapon = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
                    if (weapon == null)
                    {
                        continue;
                    }

                    if (doneWeapons.Contains(weapon))
                    {
                        continue;
                    }

                    var compProps = weapon.GetCompProperties<WhandCompProps>();
                    if (compProps == null)
                    {
                        compProps = new WhandCompProps

                        {
                            compClass = typeof(WhandComp),
                            MainHand = weaponSets.MainHand,
                            SecHand = weaponSets.SecHand
                        };
                        weapon.comps.Add(compProps);
                    }
                    else
                    {
                        compProps.MainHand = weaponSets.MainHand;
                        compProps.SecHand = weaponSets.SecHand;
                    }

                    doneWeapons.Add(weapon);
                }
            }
        }

        private static bool IsWeaponLong(ThingDef weapon, out Vector3 mainHand, out Vector3 secHand)
        {
            var texture = weapon.graphicData.Graphic.MatSingle.mainTexture;

            // This is not allowed
            //var icon = (Texture2D) texture;

            // This is
            var renderTexture = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTexture);
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var icon = new Texture2D(texture.width, texture.height);
            icon.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            icon.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);


            var pixels = icon.GetPixels32();
            var width = icon.width;
            var startPixel = width;
            var endPixel = 0;

            for (var i = 0; i < icon.height; i++)
            {
                for (var j = 0; j < startPixel; j++)
                {
                    if (pixels[j + (i * width)].a < 5)
                    {
                        continue;
                    }

                    startPixel = j;
                    break;
                }

                for (var j = width - 1; j >= endPixel; j--)
                {
                    if (pixels[j + (i * width)].a < 5)
                    {
                        continue;
                    }

                    endPixel = j;
                    break;
                }
            }


            var percentWidth = (endPixel - startPixel) / (float) width;
            var percentStart = 0f;
            if (startPixel != 0)
            {
                percentStart = startPixel / (float) width;
            }

            var percentEnd = 0f;
            if (width - endPixel != 0)
            {
                percentEnd = (width - endPixel) / (float) width;
            }

            ShowMeYourHandsMain.LogMessage(
                $"{weapon.defName}: start {startPixel.ToString()}, percentstart {percentStart}, end {endPixel.ToString()}, percentend {percentEnd}, width {width}, percent {percentWidth}");

            if (percentWidth > 0.7f)
            {
                mainHand = new Vector3(-0.3f + percentStart, 0.3f, -0.05f);
                secHand = new Vector3(0.2f, 0, -0.05f);
            }
            else
            {
                mainHand = new Vector3(-0.3f + percentStart, 0.3f, 0f);
                secHand = Vector3.zero;
            }

            return percentWidth > 0.7f;
        }
    }
}