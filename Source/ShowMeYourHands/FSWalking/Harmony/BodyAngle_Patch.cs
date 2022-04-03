using FacialStuff;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands.FSWalking
{
    [HarmonyAfter("com.yayo.yayoAni")]
    [HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
    class BodyAngle_Patch
    {
        static void Postfix(PawnRenderer __instance, ref float __result, Pawn ___pawn)
        {
            if (___pawn == null || !___pawn.GetCompAnim(out CompBodyAnimator compAnim))
            {
                return;
            }

            //if (compAnim != null)
            {
                if (compAnim.IsMoving)
                {
                    __result += compAnim.BodyAngle;
                    //return false;
                }
            }
            //return true;
        }

    }

}
