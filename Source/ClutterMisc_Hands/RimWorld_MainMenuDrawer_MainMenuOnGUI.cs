using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WHands
{
    [HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
    public static class RimWorld_MainMenuDrawer_MainMenuOnGUI
    {
        private static bool alreadyRun;

        [HarmonyPostfix]
        public static void MainMenuOnGUI()
        {
            if (alreadyRun)
            {
                return;
            }

            var count = 0;
            alreadyRun = true;
            foreach (var weapon in from weapon in DefDatabase<ThingDef>.AllDefsListForReading
                where weapon.IsWeapon && !weapon.destroyOnDrop && !weapon.menuHidden &&
                      !ClutterMain.doneWeapons.Contains(weapon)
                select weapon)
            {
                if (weapon.weaponTags?.Find(tag => tag.ToLower().Contains("shield")) != null)
                {
                    continue;
                }

                var compie = new WhandCompProps {compClass = typeof(WhandComp)};
                if (weapon.IsMeleeWeapon)
                {
                    compie.MainHand = new Vector3(-0.25f, 0.3f, 0);
                    weapon.comps.Add(compie);
                    count++;
                    continue;
                }

                if (!weapon.IsRangedWeapon)
                {
                    continue;
                }

                if (IsWeaponLong(weapon, out var mainHand, out var secHand))
                {
                    compie.SecHand = secHand;
                }

                compie.MainHand = mainHand;

                weapon.comps.Add(compie);
                count++;
            }

            Log.Message(
                $"[ShowMeYourHands]: Added hand definitions to {ClutterMain.doneWeapons.Count + count} weapons");
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

            //Log.Message(
            //    $"{weapon.defName}: start {startPixel.ToString()}, percentstart {percentStart}, end {endPixel.ToString()}, percentend {percentEnd}, width {width}, percent {percentWidth}");

            if (percentWidth > 0.7f)
            {
                mainHand = new Vector3(-0.3f + percentStart, 0.3f, -0.05f);
                secHand = new Vector3(0.2f, 0, -0.05f);
            }
            else
            {
                mainHand = new Vector3(-0.3f + percentStart, 0.3f, 0f);
                secHand = Vector3.one;
            }

            return percentWidth > 0.7f;
        }
    }
}