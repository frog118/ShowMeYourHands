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


            pawn.GetCompAnim()?.ClearCache();

            pawn.CheckForAddedOrMissingPartsAndSetColors();
            pawn.GetCompAnim()?.pawnBodyGraphic?.Initialize();

        }
    }
}
