using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShowMeYourHands;

[HarmonyPatch(typeof(PawnRenderer), "DrawCarriedThing")]
public static class PawnRenderer_DrawCarriedThing
{
    public static void Postfix(Vector3 drawLoc, Pawn ___pawn)
    {
        Thing carriedThing = ___pawn.carryTracker?.CarriedThing;
        if (carriedThing == null)
        {
            return;
        }

        if (!ShowMeYourHandsMod.instance.Settings.ShowWhenCarry)
        {
            return;
        }

        HandDrawer handComp = ___pawn.GetComp<HandDrawer>();
        if (handComp == null)
        {
            return;
        }

        Vector3 vector = drawLoc;
        bool behind = false;
        bool flip = false;
        if (___pawn.CurJob == null ||
            !___pawn.jobs.curDriver.ModifyCarriedThingDrawPos(ref vector, ref behind, ref flip))
        {
            if (carriedThing is Pawn || carriedThing is Corpse)
            {
                vector += new Vector3(0.44f, 0f, 0f);
            }
            else
            {
                vector += new Vector3(0.18f, 0f, 0.05f);
            }
        }

        if (behind)
        {
            vector.y -= 0.03474903f;
        }
        else
        {
            vector.y += 0.03474903f;
        }

        handComp.DrawHands(carriedThing, vector);
    }
}