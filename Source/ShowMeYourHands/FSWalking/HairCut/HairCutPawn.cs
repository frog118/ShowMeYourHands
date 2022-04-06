using JetBrains.Annotations;
using ShowMeYourHands.FSWalking.HairCut;
using UnityEngine;
using Verse;

namespace FacialStuff.HairCut
{
    public class HairCutPawn
    {
        [CanBeNull] public Graphic HairCutGraphic;

        public Pawn Pawn;

        [CanBeNull]
        public void Postfiix(Rot4 facing, bool portrait, bool cached, ref Material __result)
        {
            if (this.HairCutGraphic == null)
            {
                return;
               // Class3.ResolveApparelGraphics_Postfix(this.Pawn.Drawer.renderer.graphics);

            }

            Material material = this.HairCutGraphic?.MatAt(facing);

            if (!material.NullOrBad() && !cached)
            {
                material = this.Pawn.Drawer.renderer.graphics.flasher.GetDamagedMat(material);
            }

            if (!material.NullOrBad())
            {
                __result = material;
            }
        }
    }
}