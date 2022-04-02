using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FacialStuff.Animator;
using FacialStuff.AnimatorWindows;
using FacialStuff.Tweener;
using RimWorld;
using ShowMeYourHands;
using ShowMeYourHands.FSWalking;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FacialStuff
{
    public class HumanBipedDrawer : PawnBodyDrawer
    {
        #region Protected Fields

        protected const float OffsetGroundZ = -0.575f;

        protected DamageFlasher Flasher;

        #endregion Protected Fields

        #region Private Fields

        //  private PawnFeetTweener feetTweener;
        private float _animatedPercent;

        #endregion Private Fields

        #region Public Properties

        public Material LeftHandMat =>
        this.Flasher.GetDamagedMat(this.CompAnimator.pawnBodyGraphic?.HandGraphicLeft?.MatSingle);

        public Material LeftHandShadowMat => this.Flasher.GetDamagedMat(this.CompAnimator.pawnBodyGraphic
                                                                           ?.HandGraphicLeftShadow?.MatSingle);

        public Material RightHandMat =>
        this.Flasher.GetDamagedMat(this.CompAnimator.pawnBodyGraphic?.HandGraphicRight?.MatSingle);

        public Material RightHandShadowMat => this.Flasher.GetDamagedMat(this.CompAnimator.pawnBodyGraphic
                                                                            ?.HandGraphicRightShadow?.MatSingle);

        #endregion Public Properties

        #region Public Methods

        public override void ApplyBodyWobble(ref Vector3 rootLoc, ref Vector3 footPos)
        {
            if (this.CompAnimator.BodyAnim != null)
            {
                this.CompAnimator.ModifyBodyAndFootPos(ref rootLoc, ref footPos);
            }
            if (this.CompAnimator.IsMoving)
            {
                WalkCycleDef walkCycle = this.CompAnimator.WalkCycle;
                if (walkCycle != null)
                {
                    float bodysizeScaling = CompAnimator.GetBodysizeScaling(out _);
                    float bam = this.CompAnimator.BodyOffsetZ*bodysizeScaling;

                    rootLoc.z += bam;
                    this.SetBodyAngle(this.CompAnimator.MovedPercent);

                    // Log.Message(CompFace.Pawn + " - " + this.movedPercent + " - " + bam.ToString());
                }
            }
            base.ApplyBodyWobble(ref rootLoc, ref footPos);

            // Adds the leg length to the rootloc and relocates the feet to keep the pawn in center, e.g. for shields

        }



        public void ApplyEquipmentWobble(ref Vector3 rootLoc)
        {
            if (this.CompAnimator.IsMoving)
            {
                WalkCycleDef walkCycle = this.CompAnimator.WalkCycle;
                if (walkCycle != null)
                {
                    float bam = this.CompAnimator.BodyOffsetZ;
                    rootLoc.z += bam;

                    // Log.Message(CompFace.Pawn + " - " + this.movedPercent + " - " + bam.ToString());
                }
            }

            return;
            // cannot move root pos so the stuff below not working
        }



        public override bool CarryStuff()
        {
            Pawn pawn = this.pawn;

            Thing carriedThing = pawn.carryTracker?.CarriedThing;
            if (carriedThing != null)
            {
                return true;
            }

            return base.CarryStuff();
        }

        public void DoAttackAnimationHandOffsets(ref List<float> weaponAngle, ref Vector3 weaponPosition, bool flipped)
        {
            Pawn pawn = this.pawn;
            if (pawn.story != null && ((pawn.story.DisabledWorkTagsBackstoryAndTraits & WorkTags.Violent) != 0))
            {
                return;
            }

            if (pawn.health?.capacities != null)
            {
                if (!pawn.health.capacities
                         .CapableOf(PawnCapacityDefOf
                                   .Manipulation))
                {
                    if (pawn.RaceProps != null && pawn.RaceProps.ToolUser)
                    {
                        return;
                    }
                }
            }

            // total weapon angle change during animation sequence
            int totalSwingAngle = 0;
            Vector3 currentOffset = this.CompAnimator.Jitterer.CurrentOffset;

            float jitterMax = this.CompAnimator.JitterMax;
            float magnitude = currentOffset.magnitude;
            float animationPhasePercent = magnitude / jitterMax;
            {
                // if (damageDef == DamageDefOf.Stab)
                weaponPosition += currentOffset;
            }

            // else if (damageDef == DamageDefOf.Blunt || damageDef == DamageDefOf.Cut)
            // {
            // totalSwingAngle = 120;
            // weaponPosition += currentOffset + new Vector3(0, 0, Mathf.Sin(magnitude * Mathf.PI / jitterMax) / 10);
            // }
            float angle = animationPhasePercent * totalSwingAngle;
            weaponAngle[0] += (flipped ? -1f : 1f) * angle;
            weaponAngle[1] += (flipped ? -1f : 1f) * angle;
        }


        private Material OverrideMaterialIfNeeded(Material original, Pawn pawn, bool portrait = false)
        {
            Material baseMat = ((!portrait && pawn.IsInvisible()) ? InvisibilityMatPool.GetInvisibleMat(original) : original);
            return pawn.Drawer.renderer.graphics.flasher.GetDamagedMat(baseMat);
        }
        // Deactivated
        public override void DrawPawnBody(Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags, out Mesh bodyMesh)
        {
            // renderBody is AFAIK only used for beds, so ignore it and undress

            Vector3 bodyLoc = rootLoc;
            bodyLoc.x += this.CompAnimator.BodyAnim?.offCenterX ?? 0f;
            bodyLoc.y += Offsets.YOffset_Body;

            PawnGraphicSet graphics = ((BasicDrawer)this).pawn.Drawer.renderer.graphics; ;
            Pawn pawn = ((BasicDrawer)this).pawn;
            // Original for integration
            Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 vector = rootLoc;
            vector.y += 0.008687258f;
            Vector3 loc = vector;
            loc.y += 0.00144787633f;
            bodyMesh = null;
            if (bodyDrawType == RotDrawMode.Dessicated && !pawn.RaceProps.Humanlike && graphics.dessicatedGraphic != null && !flags.FlagSet(PawnRenderFlags.Portrait))
            {
                graphics.dessicatedGraphic.Draw(vector, facing, pawn, angle);
                return;
            }
            if (pawn.RaceProps.Humanlike)
            {
                bodyMesh = MeshPool.humanlikeBodySet.MeshAt(facing);
            }
            else
            {
                bodyMesh = graphics.nakedGraphic.MeshAt(facing);
            }
            List<Material> list = graphics.MatsBodyBaseAt(facing, bodyDrawType, flags.FlagSet(PawnRenderFlags.Clothes));
            for (int i = 0; i < list.Count; i++)
            {
                Material mat = (flags.FlagSet(PawnRenderFlags.Cache) ? list[i] : OverrideMaterialIfNeeded(list[i], pawn, flags.FlagSet(PawnRenderFlags.Portrait)));
                GenDraw.DrawMeshNowOrLater(bodyMesh, vector, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                vector.y += 0.00289575267f;
            }
            if (ModsConfig.IdeologyActive && graphics.bodyTattooGraphic != null && bodyDrawType != RotDrawMode.Dessicated && (facing != Rot4.North || pawn.style.BodyTattoo.visibleNorth))
            {
                GenDraw.DrawMeshNowOrLater(pawn.Drawer.renderer.GetBodyOverlayMeshSet().MeshAt(facing), loc, quat, graphics.bodyTattooGraphic.MatAt(facing), flags.FlagSet(PawnRenderFlags.DrawNow));
            }

        }


        public override void DrawFeet(Quaternion drawQuat, Vector3 rootLoc, Vector3 bodyLoc, float factor = 1f)
        {
            if (this.ShouldBeIgnored())
            {
                return;
            }
            /// No feet while sitting at a table
            Job curJob = this.pawn.CurJob;
            if (curJob != null)
            {
                if (curJob.def == JobDefOf.Ingest && !this.CurrentRotation.IsHorizontal)
                {
                    if (curJob.targetB.IsValid)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Rot4 rotty = new(i);
                            IntVec3 intVec = this.pawn.Position + rotty.FacingCell;
                            if (intVec == curJob.targetB)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            var unused = this.CompAnimator.FootColor;

            if (pawn.GetPosture() == PawnPosture.Standing) // keep the feet straight while standing, ignore the bodyQuat
            {
                drawQuat = Quaternion.AngleAxis(0f, Vector3.up);
            }

            if (this.CompAnimator.IsMoving)
            {
                // drawQuat *= Quaternion.AngleAxis(-pawn.Drawer.renderer.BodyAngle(), Vector3.up);
            }

            Rot4 rot = this.CompAnimator.CurrentRotation;

            // Basic values
            BodyAnimDef body = this.CompAnimator.BodyAnim;
            if (body == null)
            {
                return;
            }

            JointLister groundPos = this.GetJointPositions(JointType.Hip,
                                                           body.hipOffsets[rot.AsInt],
                                                           body.hipOffsets[Rot4.North.AsInt].x);

            Vector3 rightFootCycle = Vector3.zero;
            Vector3 leftFootCycle = Vector3.zero;
            float footAngleRight = 0;
            float footAngleLeft = 0;
            float offsetJoint = 0;
            WalkCycleDef cycle = this.CompAnimator.WalkCycle;
            if (this.CompAnimator.IsMoving && cycle != null)
            {
                offsetJoint = cycle.HipOffsetHorizontalX.Evaluate(this.CompAnimator.MovedPercent);
                this.DoWalkCycleOffsets(
                                        ref rightFootCycle,
                                        ref leftFootCycle,
                                        ref footAngleRight,
                                        ref footAngleLeft,
                                        ref offsetJoint,
                                        cycle.FootPositionX,
                                        cycle.FootPositionZ,
                                        cycle.FootAngle);

            }

            // pawn jumping too hight,move the feet
            if (!CompAnimator.IsMoving && pawn.GetPosture() == PawnPosture.Standing)
            {
                float bodysizeScaling = CompAnimator.GetBodysizeScaling(out _);

                Vector3 footVector = rootLoc;

                // Arms too far away from body
                while (Vector3.Distance(bodyLoc, footVector) > body.extraLegLength * bodysizeScaling * 1.5f)
                {
                    float step = 0.025f;
                    footVector = Vector3.MoveTowards(footVector, bodyLoc, step);
                }

                // carriedThing.DrawAt(drawPos, flip);
                footVector.y = rootLoc.y;
                if (this.CompAnimator.CurrentRotation == Rot4.North) // put the hands behind the pawn
                {
                    footVector.y -= Offsets.YOffset_Behind;
                }
                rootLoc = footVector;
            }

            this.GetBipedMesh(out Mesh footMeshRight, out Mesh footMeshLeft);

            Material matRight;
            Material matLeft;
            /*
            if (MainTabWindow_BaseAnimator.Colored)
            {
                matRight = this.CompAnimator.PawnBodyGraphic?.FootGraphicRightCol?.MatAt(rot);
                matLeft = this.CompAnimator.PawnBodyGraphic?.FootGraphicLeftCol?.MatAt(rot);
            }
            else 
            */
            {
                Material rightFootMat = this.CompAnimator.pawnBodyGraphic?.FootGraphicRight?.MatAt(rot);
                Material leftFootMat = this.CompAnimator.pawnBodyGraphic?.FootGraphicLeft?.MatAt(rot);
                Material leftShadowMat = this.CompAnimator.pawnBodyGraphic?.FootGraphicLeftShadow?.MatAt(rot);
                Material rightShadowMat = this.CompAnimator.pawnBodyGraphic?.FootGraphicRightShadow?.MatAt(rot);

                switch (rot.AsInt)
                {
                    default:
                        matRight = this.Flasher.GetDamagedMat(rightFootMat);
                        matLeft = this.Flasher.GetDamagedMat(leftFootMat);
                        break;

                    case 1:
                        matRight = this.Flasher.GetDamagedMat(rightFootMat);

                        matLeft = this.Flasher.GetDamagedMat(leftShadowMat);
                        break;

                    case 3:

                        matRight = this.Flasher.GetDamagedMat(rightShadowMat);
                        matLeft = this.Flasher.GetDamagedMat(leftFootMat);
                        break;
                }
            }

            bool drawRight = matRight != null && this.CompAnimator.BodyStat.FootRight != PartStatus.Missing;

            bool drawLeft = matLeft != null && this.CompAnimator.BodyStat.FootLeft != PartStatus.Missing;

            groundPos.LeftJoint = drawQuat * groundPos.LeftJoint;
            groundPos.RightJoint = drawQuat * groundPos.RightJoint;
            leftFootCycle = drawQuat * leftFootCycle;
            rightFootCycle = drawQuat * rightFootCycle;
            Vector3 ground = rootLoc + drawQuat * new Vector3(0, 0, OffsetGroundZ) * factor;
            
            if (drawLeft)
            {
                // TweenThing leftFoot = TweenThing.FootLeft;
                // PawnPartsTweener tweener = this.CompAnimator.PartTweener;
                // if (tweener != null)
                {
                    Vector3 position = ground + (groundPos.LeftJoint + leftFootCycle) * factor;
                    // tweener.PartPositions[(int)leftFoot] = position;
                    // tweener.PreThingPosCalculation(leftFoot, spring: SpringTightness.Stff);

                    Graphics.DrawMesh(
                                               footMeshLeft,
                                               position, // tweener.TweenedPartsPos[(int)leftFoot],
                                               drawQuat * Quaternion.AngleAxis(footAngleLeft, Vector3.up),
                                               matLeft,
                                               0);
                }
            }

            if (drawRight)
            {
                // TweenThing rightFoot = TweenThing.FootRight;
                // PawnPartsTweener tweener = this.CompAnimator.PartTweener;
                // if (tweener != null)
                // {
                Vector3 position = ground + (groundPos.RightJoint + rightFootCycle) * factor;

                // tweener.PartPositions[(int)rightFoot] = position;
                //     tweener.PreThingPosCalculation(rightFoot, spring: SpringTightness.Stff);
                Graphics.DrawMesh(
                                           footMeshRight,
                                           position, // tweener.TweenedPartsPos[(int)rightFoot],
                                           drawQuat * Quaternion.AngleAxis(footAngleRight, Vector3.up),
                                           matRight,
                                           0);

                // }
            }
            /*
            if (MainTabWindow_BaseAnimator.Develop)
            {
                // for debug
                Material centerMat = GraphicDatabase
                                    .Get<Graphic_Single>("Hands/Ground", ShaderDatabase.Transparent, Vector2.one,
                                                         Color.red).MatSingle;

                GenDraw.DrawMeshNowOrLater(
                                           footMeshLeft,
                                           ground + groundPos.LeftJoint +
                                           new Vector3(offsetJoint, -0.301f, 0),
                                           drawQuat * Quaternion.AngleAxis(0, Vector3.up),
                                           centerMat,
                                           false);

                GenDraw.DrawMeshNowOrLater(
                                           footMeshRight,
                                           ground + groundPos.RightJoint +
                                           new Vector3(offsetJoint, 0.301f, 0),
                                           drawQuat * Quaternion.AngleAxis(0, Vector3.up),
                                           centerMat,
                                           false);

                Material hipMat = GraphicDatabase
                    .Get<Graphic_Single>("Hands/Human_Hand_dev", ShaderDatabase.Transparent, Vector2.one,
                        Color.blue).MatSingle;

                GenDraw.DrawMeshNowOrLater(
                    footMeshLeft,
                    groundPos.LeftJoint +
                    new Vector3(offsetJoint, -0.301f, 0),
                    drawQuat * Quaternion.AngleAxis(0, Vector3.up),
                    hipMat,
                    false);

                // UnityEngine.Graphics.DrawMesh(handsMesh, center + new Vector3(0, 0.301f, z),
                // Quaternion.AngleAxis(0, Vector3.up), centerMat, 0);
                // UnityEngine.Graphics.DrawMesh(handsMesh, center + new Vector3(0, 0.301f, z2),
                // Quaternion.AngleAxis(0, Vector3.up), centerMat, 0);
            }
            */
        }

        public override void DrawHands(Quaternion bodyQuat, Vector3 drawPos,
            Thing carriedThing = null, bool flip = false)
        {
            if (this.ShouldBeIgnored())
            {
                return;
            }


            if (!PawnExtensions.pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
            {
                var bodySize = 1f;
                if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
                {
                    if (pawn.RaceProps != null)
                    {
                        bodySize = pawn.RaceProps.baseBodySize;
                    }

                    if (ShowMeYourHandsMain.BabysAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                    {
                        bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] { pawn });
                    }
                }

                PawnExtensions.pawnBodySizes[pawn] = 0.8f * bodySize;
            }


            if (!this.CompAnimator.Props.bipedWithHands)
            {
                return;
            }

            ThingWithComps eq = pawn?.equipment?.Primary;
            if (eq != null && !pawn.CurJob.def.neverShowWeapon)
            {
                Type baseType = pawn.Drawer.renderer.GetType();
                MethodInfo methodInfo = baseType.GetMethod("CarryWeaponOpenly", BindingFlags.NonPublic | BindingFlags.Instance);
                object result = methodInfo?.Invoke(pawn.Drawer.renderer, null);
                if (result != null && (bool)result)
                {
                    DrawEquipmentAiming_Patch.DrawEquipmentAiming_Postfix(eq, drawPos, CurrentRotation == Rot4.West ? 217f : 143f, pawn);
                }
            }
            // return if hands already drawn on carrything
            bool carrying = this.CarryStuff();
            float bodysizeScaling = CompAnimator.GetBodysizeScaling(out _);

            BodyAnimDef body = this.CompAnimator.BodyAnim;

            if (carrying && !CompAnimator.IsMoving) // pawn could be eating
            {
                // this.ApplyEquipmentWobble(ref drawPos);


                Vector3 handVector = drawPos;

                // Arms too far away from body
                while (Vector3.Distance(this.pawn.DrawPos, handVector) > body.armLength * bodysizeScaling * 1.5f)
                {
                    float step = 0.025f;
                    handVector = Vector3.MoveTowards(handVector, this.pawn.DrawPos, step);
                }

                // carriedThing.DrawAt(drawPos, flip);
                handVector.y = drawPos.y;
                if (CurrentRotation == Rot4.North) // put the hands behind the pawn
                {
                    handVector.y -= Offsets.YOffset_Behind;
                }
                drawPos = handVector;
            }


            Rot4 rot = this.CompAnimator.CurrentRotation;

            if (body == null)
            {
                return;
            }

            JointLister shoulperPos = this.GetJointPositions(JointType.Shoulder,
                                                             body.shoulderOffsets[rot.AsInt],
                                                             body.shoulderOffsets[Rot4.North.AsInt].x,
                                                             carrying, this.pawn.ShowWeaponOpenly());

            List<float> handSwingAngle = new() { 0f, 0f };
            List<float> shoulderAngle = new() { 0f, 0f };
            Vector3 rightHand = Vector3.zero;
            Vector3 leftHand = Vector3.zero;
            WalkCycleDef walkCycle = this.CompAnimator.WalkCycle;
            PoseCycleDef poseCycle = this.CompAnimator.PoseCycle;

            if (walkCycle != null && !carrying)
            {
                float offsetJoint = walkCycle.ShoulderOffsetHorizontalX.Evaluate(this.CompAnimator.MovedPercent);

                // Children's arms are way too long
                this.DoWalkCycleOffsets(
                                        body.armLength,
                                        ref rightHand,
                                        ref leftHand,
                                        ref shoulderAngle,
                                        ref handSwingAngle,
                                        ref shoulperPos,
                                        carrying,
                                        walkCycle.HandsSwingAngle,
                                        offsetJoint);
            }


           // this.DoAttackAnimationHandOffsets(ref handSwingAngle, ref rightHand, false);

           var unused = this.CompAnimator.HandColor;

            this.GetBipedMesh(out Mesh handMeshRight, out Mesh handMeshLeft);

            Material matLeft = this.LeftHandMat;
            Material matRight = this.RightHandMat;

            /*if (MainTabWindow_BaseAnimator.Colored)
            {
                matLeft = this.CompAnimator.PawnBodyGraphic?.HandGraphicLeftCol?.MatSingle;
                matRight = this.CompAnimator.PawnBodyGraphic?.HandGraphicRightCol?.MatSingle;
            }
            else */
            if (carriedThing == null)
            {
                // Should draw shadow if inner side of the palm is facing to camera?
                switch (rot.AsInt)
                {
                    case 1:
                        matLeft = this.LeftHandShadowMat;
                        break;

                    case 3:
                        matRight = this.RightHandShadowMat;
                        break;
                }
            }

            bool drawLeft = matLeft != null && this.CompAnimator.BodyStat.HandLeft != PartStatus.Missing;
            bool drawRight = matRight != null && this.CompAnimator.BodyStat.HandRight != PartStatus.Missing;

            float shouldRotate = pawn.GetPosture() == PawnPosture.Standing ? 0f : 90f; 


            if (drawLeft)
            {
                Quaternion quat;
                Vector3 position;
                bool noTween = false;
                if (!this.CompAnimator.IsMoving && this.CompAnimator.SecondHandPosition != Vector3.zero)
                {
                    position = this.CompAnimator.SecondHandPosition;
                    quat = this.CompAnimator.SecondHandQuat;
                    quat *= Quaternion.AngleAxis(90f, Vector3.up);
                    noTween = true;
                }
                else
                {
                    shoulperPos.LeftJoint = bodyQuat * shoulperPos.LeftJoint;
                    leftHand = bodyQuat * leftHand.RotatedBy(-handSwingAngle[0] - shoulderAngle[0]);

                    position = drawPos + (shoulperPos.LeftJoint + leftHand) * bodysizeScaling;
                    quat = bodyQuat * Quaternion.AngleAxis(-handSwingAngle[0] - shoulderAngle[0] -shouldRotate, Vector3.up);
                }

                TweenThing handLeft = TweenThing.HandLeft;
                this.DrawTweenedHand(position, handMeshLeft, matLeft, quat, handLeft, noTween);
                //GenDraw.DrawMeshNowOrLater(
                //                           handMeshLeft, position,
                //                           quat,
                //                           matLeft,
                //                           portrait);
            }

            if (drawRight)
            {
                Quaternion quat;
                Vector3 position;
                bool noTween = false;
                if (this.CompAnimator.FirstHandPosition != Vector3.zero)
                {
                    quat = this.CompAnimator.FirstHandQuat;
                    quat *= Quaternion.AngleAxis(-90f, Vector3.up);
                    position = this.CompAnimator.FirstHandPosition;
                    noTween = true;



            }
            else
                {
                    shoulperPos.RightJoint = bodyQuat * shoulperPos.RightJoint;
                    rightHand = bodyQuat * rightHand.RotatedBy(handSwingAngle[1] - shoulderAngle[1]);

                    position = drawPos + (shoulperPos.RightJoint + rightHand) * bodysizeScaling;
                    quat = bodyQuat * Quaternion.AngleAxis(handSwingAngle[1] +shoulderAngle[1] +shouldRotate, Vector3.up);
                }


                TweenThing handRight = TweenThing.HandRight;
                this.DrawTweenedHand(position, handMeshRight, matRight, quat, handRight, noTween);
                // GenDraw.DrawMeshNowOrLater(
                //                            handMeshRight, position,
                //                            quat,
                //                            matRight,
                //                            portrait);
            }
            /*
            if (MainTabWindow_BaseAnimator.Develop)
            {
                // for debug
                Material centerMat = GraphicDatabase.Get<Graphic_Single>(
                                                                         "Hands/Human_Hand_dev",
                                                                         ShaderDatabase.CutoutSkin,
                                                                         Vector2.one,
                                                                         Color.white).MatSingle;

                GenDraw.DrawMeshNowOrLater(
                                           handMeshLeft,
                                           drawPos + shoulperPos.LeftJoint + new Vector3(0, -0.301f, 0),
                                           bodyQuat * Quaternion.AngleAxis(-shoulderAngle[0], Vector3.up),
                                           centerMat,
                                           false);

                GenDraw.DrawMeshNowOrLater(
                                           handMeshRight,
                                           drawPos + shoulperPos.RightJoint + new Vector3(0, 0.301f, 0),
                                           bodyQuat * Quaternion.AngleAxis(-shoulderAngle[1], Vector3.up),
                                           centerMat,
                                           false);
            }
            */
        }

        public override void Initialize()
        {
            this.Flasher = this.pawn.Drawer.renderer.graphics.flasher;

            // this.feetTweener = new PawnFeetTweener();
            base.Initialize();
        }

        public void SetBodyAngle(float movedPercent)
        {
            WalkCycleDef walkCycle = this.CompAnimator.WalkCycle;
            if (walkCycle != null)
            {
                float angle;
                if (this.CompAnimator.CurrentRotation.IsHorizontal)
                {
                    angle = (this.CompAnimator.CurrentRotation == Rot4.West ? -1 : 1)
                          * walkCycle.BodyAngle.Evaluate(movedPercent);
                }
                else
                {
                    angle = (this.CompAnimator.CurrentRotation == Rot4.South ? -1 : 1)
                          * walkCycle.BodyAngleVertical.Evaluate(movedPercent);
                }

                this.CompAnimator.BodyAngle = angle;
            }
        }

        public Job lastJob;
        private Rot4 CurrentRotation;

        public virtual void SelectWalkcycle(bool pawnInEditor)
        {
            /*
            if (pawnInEditor)
            {
                this.CompAnimator.SetWalkCycle(Find.WindowStack.WindowOfType<MainTabWindow_WalkAnimator>().EditorWalkcycle);
                return;
            }
            */

            if (this.pawn.CurJob != null && this.pawn.CurJob != this.lastJob)
            {
                BodyAnimDef animDef = this.CompAnimator.BodyAnim;

                Dictionary<LocomotionUrgency, WalkCycleDef> cycles = animDef?.walkCycles;

                if (cycles != null && cycles.Count > 0)
                {
                    if (cycles.TryGetValue(this.pawn.CurJob.locomotionUrgency, out WalkCycleDef cycle))
                    {
                        if (cycle != null)
                        {
                            this.CompAnimator.SetWalkCycle(cycle);
                        }
                    }
                    else
                    {
                        this.CompAnimator.SetWalkCycle(animDef.walkCycles.FirstOrDefault().Value);
                    }
                }

                this.lastJob = this.pawn.CurJob;
            }
        }

        public virtual void SelectPosecycle()
        {
            return;
            
            // if (HarmonyPatchesFS.AnimatorIsOpen())
            {
                //  this.CompAnimator.PoseCycle = MainTabWindow_PoseAnimator.EditorPoseCycle;
            }

            if (this.pawn.CurJob != null)
            {
                BodyAnimDef animDef = this.CompAnimator.BodyAnim;

                List<PoseCycleDef> cycles = animDef?.poseCycles;

                if (cycles != null && cycles.Count > 0)
                {
                    this.CompAnimator.PoseCycle = animDef.poseCycles.FirstOrDefault();
                }

                // switch (this.Pawn.CurJob.locomotionUrgency)
                // {
                // case LocomotionUrgency.None:
                // case LocomotionUrgency.Amble:
                // this.walkCycle = WalkCycleDefOf.Biped_Amble;
                // break;
                // case LocomotionUrgency.Walk:
                // this.walkCycle = WalkCycleDefOf.Biped_Walk;
                // break;
                // case LocomotionUrgency.Jog:
                // this.walkCycle = WalkCycleDefOf.Biped_Jog;
                // break;
                // case LocomotionUrgency.Sprint:
                // this.walkCycle = WalkCycleDefOf.Biped_Sprint;
                // break;
                // }
            }
        }

        public override void Tick()
        {
            base.Tick();

            BodyAnimator animator = CompAnimator.BodyAnimator;
           /*
            if (animator != null)
            {
                animator.IsPosing(out this._animatedPercent);
            }
            */
           // var curve = bodyFacing.IsHorizontal ? this.walkCycle.BodyOffsetZ : this.walkCycle.BodyOffsetVerticalZ;
           //bool pawnInEditor = HarmonyPatchesFS.AnimatorIsOpen() && MainTabWindow_BaseAnimator.pawn == this.pawn;
            if (!Find.TickManager.Paused)
            {
                bool pawnInEditor = false;
                this.SelectWalkcycle(pawnInEditor);
                this.SelectPosecycle();

               // this.CompAnimator.FirstHandPosition = Vector3.zero;
                //this.CompAnimator.SecondHandPosition = Vector3.zero;
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected void GetBipedMesh(out Mesh meshRight, out Mesh meshLeft)
        {
            Rot4 rot = this.CompAnimator.CurrentRotation;

            switch (rot.AsInt)
            {
                default:
                    meshRight = this.CompAnimator.pawnBodyMesh;// MeshPool.plane10;
                    meshLeft = this.CompAnimator.pawnBodyMeshFlipped; // MeshPool.plane10Flip;
                    break;

                case 1:
                    meshRight = this.CompAnimator.pawnBodyMesh;
                    meshLeft = this.CompAnimator.pawnBodyMesh;
                    break;

                case 3:
                    meshRight = CompAnimator.pawnBodyMeshFlipped;// MeshPool.plane10Flip;
                    meshLeft = CompAnimator.pawnBodyMeshFlipped;// MeshPool.plane10Flip;
                    break;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void DoWalkCycleOffsets(
        float armLength,
        ref Vector3 rightHand,
        ref Vector3 leftHand,
        ref List<float> shoulderAngle,
        ref List<float> handSwingAngle,
        ref JointLister shoulderPos,
        bool carrying,
        SimpleCurve cycleHandsSwingAngle,
        float offsetJoint)
        {
            // Has the pawn something in his hands?
            if (carrying)
            {
                return;
            }
            float bodysizeScaling = CompAnimator.GetBodysizeScaling(out _);

            Rot4 rot = this.CompAnimator.CurrentRotation;

            // Basic values if pawn is carrying stuff
            float x = 0;
            float x2 = -x;
            float y = Offsets.YOffset_Behind;
            float y2 = y;
            float z;
            float z2;

            // Offsets for hands from the pawn center
            z = z2 = - armLength;

            if (rot.IsHorizontal)
            {
                x = x2 = 0f;
                if (rot == Rot4.East)
                {
                    y2 = -0.5f;
                }
                else
                {
                    y = -0.05f;
                }
            }
            else if (rot == Rot4.North)
            {
                y = y2 = -0.02f;
                x *= -1;
                x2 *= -1;
            }

            // Swing the hands, try complete the cycle
            if (this.CompAnimator.IsMoving)
            {
                WalkCycleDef walkCycle = this.CompAnimator.WalkCycle;
                float percent = this.CompAnimator.MovedPercent;
                if (rot.IsHorizontal)
                {
                    float lookie = rot == Rot4.West ? -1f : 1f;
                    float f = lookie * offsetJoint;

                    shoulderAngle[0] = shoulderAngle[1] = lookie * walkCycle?.shoulderAngle ?? 0f;

                    shoulderPos.RightJoint.x += f;
                    shoulderPos.LeftJoint.x += f;

                    handSwingAngle[0] = handSwingAngle[1] =
                                        (rot == Rot4.West ? -1 : 1) * cycleHandsSwingAngle.Evaluate(percent);
                }
                else
                {
                    z += cycleHandsSwingAngle.Evaluate(percent) / 500;
                    z2 -= cycleHandsSwingAngle.Evaluate(percent) / 500;

                    z += walkCycle?.shoulderAngle / 800 ?? 0f;
                    z2 += walkCycle?.shoulderAngle / 800 ?? 0f;
                }
            }

            if (/*MainTabWindow_BaseAnimator.Panic || */ this.pawn.Fleeing() || this.pawn.IsBurning())
            {
                float offset = 1f + armLength;
                x *= offset;
                z *= offset;
                x2 *= offset;
                z2 *= offset;
                handSwingAngle[0] += 180f;
                handSwingAngle[1] += 180f;
                shoulderAngle[0] = shoulderAngle[1] = 0f;
            }

            rightHand = new Vector3(x, y, z)* bodysizeScaling;
            leftHand = new Vector3(x2, y2, z2)* bodysizeScaling;
        }


        private void DrawTweenedHand(Vector3 position, Mesh handsMesh, Material material, Quaternion quat, TweenThing tweenThing, bool noTween)
        {
            if (position == Vector3.zero || handsMesh == null || material == null)
            {
                return;
            }

            if (this.ShouldBeIgnored())
            {
                return;
            }
            
            if (Find.TickManager.TicksGame == this.CompAnimator.LastPosUpdate[(int)tweenThing])
            {
                position = this.CompAnimator.LastPosition[(int)tweenThing];
            }
            else
            {
                Pawn_PathFollower pawnPathFollower = this.pawn.pather;
                if (pawnPathFollower != null && pawnPathFollower.MovedRecently(5))
                {
                    noTween = true;
                }

                this.CompAnimator.LastPosUpdate[(int)tweenThing] = Find.TickManager.TicksGame;


                Vector3Tween tween = this.CompAnimator.Vector3Tweens[(int)tweenThing];


                switch (tween.State)
                {
                    case TweenState.Running:
                        if (noTween || this.CompAnimator.IsMoving)
                        {
                            tween.Stop(StopBehavior.ForceComplete);
                        }

                        position = tween.CurrentValue;
                        break;

                    case TweenState.Paused:
                        break;

                    case TweenState.Stopped:
                        if (noTween || (this.CompAnimator.IsMoving))
                        {
                            break;
                        }

                        ScaleFunc scaleFunc = ScaleFuncs.SineEaseOut;


                        Vector3 start = this.CompAnimator.LastPosition[(int)tweenThing];
                        float distance = Vector3.Distance(start, position);
                        float duration = Mathf.Abs(distance * 50f);
                        if (start != Vector3.zero && duration > 12f)
                        {
                            start.y = position.y;
                            tween.Start(start, position, duration, scaleFunc);
                            position = start;
                        }

                        break;
                }

                this.CompAnimator.LastPosition[(int)tweenThing] = position;
            }

            //  tweener.PreThingPosCalculation(tweenThing, noTween);

            Graphics.DrawMesh(
                                       handsMesh, position,
                                       quat,
                                       material,
                                       0);
        }

        public bool ShouldBeIgnored()
        {
            return this.pawn.Dead || !this.pawn.Spawned || this.pawn.InContainerEnclosed;
        }

        #endregion Private Methods
    }
}