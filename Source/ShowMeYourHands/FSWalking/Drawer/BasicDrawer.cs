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
        public Pawn pawn;


        #endregion Protected Fields

        #region Public Methods

        protected virtual Mesh GetPawnMesh(bool wantsBody)
        {
            return MeshPool.humanlikeBodySet?.MeshAt(this.CompAnimator.CurrentRotation);
        }

        #endregion Public Methods

        #region Protected Methods

        protected JointLister GetJointPositions(JointType jointType, Vector3 offsets,
                                                float jointWidth,
                                                bool carrying = false, bool armed = false)
        {
            Rot4 rot = this.CompAnimator.CurrentRotation;
            JointLister joints = new()
            {
                jointType = jointType
            };
            float leftX = offsets.x;
            float rightX = offsets.x;
            float leftZ = offsets.z;
            float rightZ = offsets.z;

            float offsetY = Offsets.YOffset_HandsFeetOver;

            bool offsetsCarrying = false;

            switch (jointType)
            {
                case JointType.Shoulder:
                    offsetY = Offsets.YOffset_HandsFeetOver;
                    if (carrying) { offsetsCarrying = true; }
                    break;
            }

            float leftY = offsetY;
            float rightY = offsetY;

            if (offsetsCarrying)
            {
                leftX = -jointWidth / 1.2f;
                rightX = jointWidth / 1.2f;
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

        [NotNull] public CompBodyAnimator CompAnimator;



        #endregion Protected Methods
    }
}