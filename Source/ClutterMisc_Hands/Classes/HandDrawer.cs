using UnityEngine;
using Verse;

namespace WHands
{
    [StaticConstructorOnStartup]
    public class HandDrawer : ThingComp
    {
        private Vector3 FHand;
        private Graphic HandTex;

        public int PrimaryID;

        private Vector3 SHand;

        public bool TwoHand = true;

        public void ReadXML()
        {
            var whandCompProps = (WhandCompProps) props;
            if (whandCompProps.MainHand != Vector3.zero)
            {
                FHand = whandCompProps.MainHand;
            }

            if (whandCompProps.SecHand != Vector3.zero)
            {
                SHand = whandCompProps.SecHand;
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

        private void AngleCalc(Vector3 rootLoc)
        {
            if (!(parent is Pawn pawn) || pawn.Dead || !pawn.Spawned)
            {
                return;
            }

            if (pawn.equipment?.Primary == null)
            {
                return;
            }

            if (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon)
            {
                return;
            }

            var compProperties = pawn.equipment.Primary.def.GetCompProperties<WhandCompProps>();
            if (compProperties != null)
            {
                FHand = compProperties.MainHand;
                SHand = compProperties.SecHand;
            }
            else
            {
                SHand = Vector3.zero;
                FHand = Vector3.zero;
            }

            rootLoc.y += 0.0449999981f;
            if (pawn.stances.curStance is Stance_Busy {neverAimWeapon: false} stance_Busy &&
                stance_Busy.focusTarg.IsValid)
            {
                var a = stance_Busy.focusTarg.HasThing
                    ? stance_Busy.focusTarg.Thing.DrawPos
                    : stance_Busy.focusTarg.Cell.ToVector3Shifted();

                var num = 0f;
                if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                {
                    num = (a - pawn.DrawPos).AngleFlat();
                }

                var b = new Vector3(0f, 0f, 0.4f).RotatedBy(num);
                DrawHands(pawn.equipment.Primary, rootLoc + b, num);
            }
            else if (CarryWeaponOpenly(pawn))
            {
                if (pawn.Rotation == Rot4.South)
                {
                    var drawLoc = rootLoc + new Vector3(0f, 0f, -0.22f);
                    DrawHands(pawn.equipment.Primary, drawLoc, 143f);
                }
                else if (pawn.Rotation == Rot4.East)
                {
                    var drawLoc2 = rootLoc + new Vector3(0.2f, 0f, -0.22f);
                    DrawHands(pawn.equipment.Primary, drawLoc2, 143f);
                }
                else if (pawn.Rotation == Rot4.West)
                {
                    var drawLoc3 = rootLoc + new Vector3(-0.2f, 0f, -0.22f);
                    DrawHands(pawn.equipment.Primary, drawLoc3, 217f);
                }
            }
            else
            {
                GunDraver(pawn.equipment.Primary, pawn.DrawPos, pawn);
            }
        }

        private void GunDraver(Thing eq, Vector3 drawLoc, Pawn pawn)
        {
            if (eq == null || eq.def.defName != "Gun_Pistol")
            {
                return;
            }

            var WepHolderPos = new Vector3(0, 5f, 0);
            var matrix = default(Matrix4x4);
            var size = new Vector3(0.84f, 0f, 0.84f);
            var mesh = MeshPool.plane10;
            float num = 90;

            if (pawn.Rotation == Rot4.South)
            {
                WepHolderPos = new Vector3(0.3f, 5f, -0.3f);
                num = 270f;
            }
            else if (pawn.Rotation == Rot4.East)
            {
                WepHolderPos = new Vector3(0, 5f, -0.3f);
            }
            else if (pawn.Rotation == Rot4.West)
            {
                WepHolderPos = new Vector3(0, 0f, -0.3f);
                num = 270f;
            }
            else if (pawn.Rotation == Rot4.North)
            {
                WepHolderPos = new Vector3(-0.3f, 0f, -0.3f);
                num = 75f;
            }

            matrix.SetTRS(drawLoc + WepHolderPos, Quaternion.AngleAxis(num, Vector3.up), size);
            Graphics.DrawMesh(mesh, matrix, eq.Graphic.MatSingle, 0);
        }

        private void DrawHands(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            var flag = false;
            var pawn = parent as Pawn;
            var num = aimAngle - 90f;
            if (aimAngle > 20f && aimAngle < 160f)
            {
                num += eq.def.equippedAngleOffset;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                num -= 180f;
                num -= eq.def.equippedAngleOffset;
                flag = true;
            }
            else
            {
                num += eq.def.equippedAngleOffset;
            }

            num %= 360f;
            if (HandTex != null)
            {
                var matSingle = HandTex.MatSingle;
                if (pawn == null)
                {
                    return;
                }

                matSingle.color = pawn.story.SkinColor;
                if (FHand != Vector3.zero)
                {
                    var num2 = FHand.x;
                    var z = FHand.z;
                    var y = FHand.y;
                    if (flag)
                    {
                        num2 = -num2;
                    }

                    Graphics.DrawMesh(MeshPool.plane10, drawLoc + new Vector3(num2, y, z).RotatedBy(num),
                        Quaternion.AngleAxis(num, Vector3.up), matSingle, 0);
                }

                if (SHand == Vector3.zero)
                {
                    return;
                }

                var num3 = SHand.x;
                var z2 = SHand.z;
                var y2 = SHand.y;
                if (flag)
                {
                    num3 = -num3;
                }

                Graphics.DrawMesh(MeshPool.plane10, drawLoc + new Vector3(num3, y2, z2).RotatedBy(num),
                    Quaternion.AngleAxis(num, Vector3.up), matSingle, 0);
            }
            else if (HandTex == null)
            {
                if (pawn != null)
                {
                    HandTex = GraphicDatabase.Get<Graphic_Single>("Hand", ShaderDatabase.CutoutSkin,
                        new Vector2(1f, 1f),
                        pawn.story.SkinColor, pawn.story.SkinColor);
                }
            }
        }

        public override void PostDraw()
        {
            if (HandTex != null)
            {
                AngleCalc(parent.DrawPos);
            }
            else
            {
                if (parent is Pawn pawn)
                {
                    HandTex = GraphicDatabase.Get<Graphic_Single>("Hand", ShaderDatabase.CutoutSkin,
                        new Vector2(1f, 1f),
                        pawn.story.SkinColor, pawn.story.SkinColor);
                }
            }
        }
    }
}