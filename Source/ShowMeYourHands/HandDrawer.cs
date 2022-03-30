using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using FacialStuff;
using RimWorld;
using UnityEngine;
using Verse;
using static System.Byte;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public class HandDrawer : ThingComp
{

    private Mesh handMesh;
    private int LastDrawn;
    private Vector3 MainHand;
    private Vector3 OffHand;


    public void ReadXML()
    {
        WhandCompProps whandCompProps = (WhandCompProps)props;
        if (whandCompProps.MainHand != Vector3.zero)
        {
            MainHand = whandCompProps.MainHand;
        }

        if (whandCompProps.SecHand != Vector3.zero)
        {
            OffHand = whandCompProps.SecHand;
        }
    }

    private bool CarryWeaponOpenly(Pawn pawn)
    {
        return pawn.carryTracker?.CarriedThing == null && (pawn.Drafted ||
                                                           pawn.CurJob != null &&
                                                           pawn.CurJob.def.alwaysShowWeapon ||
                                                           pawn.mindState.duty != null &&
                                                           pawn.mindState.duty.def.alwaysShowWeapon);
    }



}