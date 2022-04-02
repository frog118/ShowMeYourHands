using System.Collections.Generic;
using System.Linq;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using JetBrains.Annotations;
using RimWorld;
using ShowMeYourHands;
using UnityEngine;
using Verse;
using static System.Byte;

namespace FacialStuff
{
    public abstract class BasicDrawer
    {
        #region Protected Fields





        [NotNull]
        public Pawn pawn { get; set; }


        #endregion Protected Fields

        #region Public Methods

        protected virtual Mesh GetPawnMesh(bool wantsBody)
        {
            return MeshPool.humanlikeBodySet?.MeshAt(this.CompAnimator.pawn.Rotation);
        }

        #endregion Public Methods

        #region Protected Methods

        protected JointLister GetJointPositions(JointType jointType, Vector3 offsets,
                                                float jointWidth,
                                                bool carrying = false, bool armed = false)
        {
            Rot4 rot = this.CompAnimator.pawn.Rotation;
            JointLister joints = new()
            {
                jointType = jointType
            };
            float leftX = offsets.x;
            float rightX = offsets.x;
            float leftZ = offsets.z;
            float rightZ = offsets.z;

            float offsetY = Offsets.YOffset_HandsFeetOver;
            float leftY = offsetY;

            bool offsetsCarrying = false;

            switch (jointType)
            {
                case JointType.Shoulder:
                    offsetY = armed ? -Offsets.YOffset_HandsFeet : Offsets.YOffset_HandsFeetOver;
                    leftY = this.CompAnimator.IsMoving ? Offsets.YOffset_HandsFeetOver : offsetY;
                    if (carrying) { offsetsCarrying = true; }
                    break;
            }

            float rightY = offsetY;
            if (offsetsCarrying)
            {
                leftX = -jointWidth / 2;
                rightX = jointWidth / 2;
                leftZ = -0.025f;
                rightZ = -leftZ;
            }
            else if (rot.IsHorizontal)
            {
                float offsetX = jointWidth * 0.1f;
                float offsetZ = jointWidth * 0.2f;

                if (rot == Rot4.East)
                {
                    leftY = -Offsets.YOffset_Behind;
                    leftZ += +offsetZ;
                }
                else
                {
                    rightY = -Offsets.YOffset_Behind;
                    rightZ += offsetZ;
                }

                leftX += offsetX;
                rightX -= offsetX;
            }
            else
            {
                leftX = -rightX;
            }

            if (rot == Rot4.North)
            {
                leftY = rightY = -Offsets.YOffset_Behind;
                // leftX *= -1;
                // rightX *= -1;
            }

            joints.RightJoint = new Vector3(rightX, rightY, rightZ);
            joints.LeftJoint = new Vector3(leftX, leftY, leftZ);

            return joints;
        }

        [NotNull]
        public CompBodyAnimator CompAnimator { get; set; }

        protected internal void DoWalkCycleOffsets(ref Vector3 rightFoot,
            ref Vector3 leftFoot,
            ref float footAngleRight,
            ref float footAngleLeft,
            ref float offsetJoint,
            SimpleCurve offsetX,
            SimpleCurve offsetZ,
            SimpleCurve angle)
        {
            rightFoot = Vector3.zero;
            leftFoot = Vector3.zero;
            footAngleRight = 0;
            footAngleLeft = 0;
            if (!this.CompAnimator.IsMoving)
            {
                return;
            }
            float bodysizeScaling = CompAnimator.GetBodysizeScaling(out _);
            float percent = this.CompAnimator.MovedPercent;

            float flot = percent;
            if (flot <= 0.5f)
            {
                flot += 0.5f;
            }
            else
            {
                flot -= 0.5f;
            }

            Rot4 rot = this.CompAnimator.pawn.Rotation;
            if (rot.IsHorizontal)
            {
                rightFoot.x = offsetX.Evaluate(percent);
                leftFoot.x = offsetX.Evaluate(flot);

                footAngleRight = angle.Evaluate(percent);
                footAngleLeft = angle.Evaluate(flot);
                rightFoot.z = offsetZ.Evaluate(percent);
                leftFoot.z = offsetZ.Evaluate(flot);

                rightFoot.x += offsetJoint;
                leftFoot.x += offsetJoint;

                if (rot == Rot4.West)
                {
                    rightFoot.x *= -1f;
                    leftFoot.x *= -1f;
                    footAngleLeft *= -1f;
                    footAngleRight *= -1f;
                    offsetJoint *= -1;
                }
            }
            else
            {
                rightFoot.z = offsetZ.Evaluate(percent);
                leftFoot.z = offsetZ.Evaluate(flot);
                offsetJoint = 0;
            }
            
            // smaller steps for smaller pawns
            if (bodysizeScaling < 1f)
            {
                SimpleCurve curve = new() { new CurvePoint(0f, 0.5f), new CurvePoint(1f, 1f) };
                float mod = curve.Evaluate(bodysizeScaling);
                rightFoot.x *= mod;
                rightFoot.z *= mod;
                leftFoot.x *= mod;
                leftFoot.z *= mod;
            }

        }



        #endregion Protected Methods
    }
}