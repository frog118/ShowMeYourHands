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

            pawn.GetCompAnim()?.pawnBodyGraphic?.Initialize();


            pawn.GetComp<CompBodyAnimator>()?.ClearCache();


        }
    }
}
