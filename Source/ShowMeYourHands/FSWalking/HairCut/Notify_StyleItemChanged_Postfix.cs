using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ShowMeYourHands.FSWalking.HairCut
{
    [HarmonyPatch(typeof(Pawn_StyleTracker), nameof(Pawn_StyleTracker.Notify_StyleItemChanged))]
    internal class Notify_StyleItemChanged_Postfix
    {
        public static void Postfix(Pawn_StyleTracker __instance)
        {
            ResolveApparelGraphics_Postfix.Postfix(__instance.pawn.Drawer.renderer.graphics);
        }
    }
}
