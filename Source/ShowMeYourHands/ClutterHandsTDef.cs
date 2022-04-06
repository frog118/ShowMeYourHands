using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WHands;

public class ClutterHandsTDef : ThingDef
{
    public readonly List<CompTargets> WeaponCompLoader = new();

    public class CompTargets
    {
        public readonly List<string> ThingTargets = new();
        public Vector3 MainHand = Vector3.zero;
        public Vector3 SecHand = Vector3.zero;

        public float? AttackAngleOffset = 0f;
        public Vector3 WeaponPositionOffset = Vector3.zero;
        public Vector3 AimedWeaponPositionOffset = Vector3.zero;

    }
}