using UnityEngine;
using Verse;

namespace ShowMeYourHands;

public class WhandCompProps : CompProperties
{
    public Vector3 MainHand = Vector3.zero;
    public Vector3 SecHand = Vector3.zero;

    public float? AttackAngleOffset = 0f;
    public Vector3 WeaponPositionOffset = Vector3.zero;
    public Vector3 AimedWeaponPositionOffset = Vector3.zero;


    public WhandCompProps()
    {
        compClass = typeof(WhandComp);
    }
}