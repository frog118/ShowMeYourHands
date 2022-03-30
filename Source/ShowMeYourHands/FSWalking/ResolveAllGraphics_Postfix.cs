using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FacialStuff;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands.FSWalking
{
[HarmonyPatch(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics))]
    internal class ResolveAllGraphics_Postfix
    {
        public static void Postfix(PawnGraphicSet __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null)
            {
                return;
            }

            pawn.GetCompAnim()?.PawnBodyGraphic?.Initialize();


            pawn.GetComp<CompBodyAnimator>()?.ClearCache();


        }
    }
}
