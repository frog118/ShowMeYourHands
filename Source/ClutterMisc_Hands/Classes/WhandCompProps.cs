using UnityEngine;
using Verse;

namespace WHands
{
    public class WhandCompProps : CompProperties
    {
        public Vector3 MainHand = Vector3.zero;
        public Vector3 SecHand = Vector3.zero;

        public WhandCompProps()
        {
            compClass = typeof(WhandComp);
        }
    }
}