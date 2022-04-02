using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using ShowMeYourHands;
using Verse;
using Verse.AI;

namespace FacialStuff
{
    public static class PawnExtensions
    {
        public static readonly Dictionary<Pawn, float> pawnBodySizes = new Dictionary<Pawn, float>();
        public const string PathHumanlike = "Things/Pawn/Humanlike/";
        public const string PathAnimals = "Things/Pawn/Animal/";
        public const string STR_Foot = "_Foot";
        public const string STR_Hand = "_Hand";


        private static void CheckBodyForAddedParts(Hediff hediff, CompBodyAnimator anim, BodyPartRecord leftHand, BodyPartRecord leftArm,
                                      BodyPartRecord rightHand, BodyPartRecord rightArm, BodyPartRecord leftFoot, BodyPartRecord leftLeg, BodyPartRecord rightFoot, BodyPartRecord rightLeg)
        {
            if (anim == null)
            {
                return;
            }

            if (!ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
            {
                return;
            }

            if (anim.Props.bipedWithHands)
            {
                if (hediff.Part.parts.Contains(leftHand) || hediff.Part.parts.Contains(leftArm))
                {
                    anim.BodyStat.HandLeft = PartStatus.Artificial;
                    if (ShowMeYourHandsMain.HediffColors.ContainsKey(hediff.def))
                    {
                        anim.leftHandColor = ShowMeYourHandsMain.HediffColors[hediff.def];
                    }
                }



                if (hediff.Part.parts.Contains(rightHand) || hediff.Part.parts.Contains(rightArm))
                {
                    anim.BodyStat.HandRight = PartStatus.Artificial;
                    if (ShowMeYourHandsMain.HediffColors.ContainsKey(hediff.def))
                    {
                        anim.rightHandColor = ShowMeYourHandsMain.HediffColors[hediff.def];
                    }
                }
            }

            if (hediff.Part.parts.Contains(leftFoot) || hediff.Part.parts.Contains(leftLeg))
            {
                anim.BodyStat.FootLeft = PartStatus.Artificial;
                if (ShowMeYourHandsMain.HediffColors.ContainsKey(hediff.def))
                {
                    anim.leftFootColor = ShowMeYourHandsMain.HediffColors[hediff.def];
                }
            }

            if (hediff.Part.parts.Contains(rightFoot) || hediff.Part.parts.Contains(rightLeg))
            {
                anim.BodyStat.FootRight = PartStatus.Artificial;
                if (ShowMeYourHandsMain.HediffColors.ContainsKey(hediff.def))
                {
                    anim.rightFootColor = ShowMeYourHandsMain.HediffColors[hediff.def];
                }
            }
        }


        private static void CheckMissingParts(BodyProps bodyProps)
        {
            Hediff hediff = bodyProps._hediff;

            if (hediff.def != HediffDefOf.MissingBodyPart)
            {
                return;
            }
            /*
            if (bodyProps._face != null)
            {
                if (bodyProps._face.Props.hasEyes)
                {
                    if (hediff.Part == bodyProps._leftEye)
                    {
                        bodyProps._face.BodyStat.EyeLeft = PartStatus.Missing;
                    }

                    if (hediff.Part == bodyProps._rightEye)
                    {
                        bodyProps._face.BodyStat.EyeRight = PartStatus.Missing;
                    }
                }

            }
            */
            if (bodyProps._anim != null && bodyProps._anim.Props.bipedWithHands)
            {
                if (hediff.Part == bodyProps._leftHand)
                {
                    bodyProps._anim.BodyStat.HandLeft = PartStatus.Missing;
                }

                if (hediff.Part == bodyProps._rightHand)
                {
                    bodyProps._anim.BodyStat.HandRight = PartStatus.Missing;
                }

                if (hediff.Part == bodyProps._leftFoot)
                {
                    bodyProps._anim.BodyStat.FootLeft = PartStatus.Missing;
                }

                if (hediff.Part == bodyProps._rightFoot)
                {
                    bodyProps._anim.BodyStat.FootRight = PartStatus.Missing;
                }
            }
        }

        private static void CheckPart(List<BodyPartRecord> body, Hediff hediff,
            [CanBeNull] CompBodyAnimator anim, bool missing)
        {
            if (body.NullOrEmpty() || hediff.def == null)
            {
                Log.Message("Body list or hediff.def is null or empty");
                return;
            }

            if (!hediff.Visible)
            {
                return;
            }

            BodyPartRecord leftEye = body.Find(x => x.customLabel == "left eye");
            BodyPartRecord rightEye = body.Find(x => x.customLabel == "right eye");
            BodyPartRecord jaw = body.Find(x => x.def == BodyPartDefOf.Jaw);


            //BodyPartRecord leftArm = body.Find(x => x.def == BodyPartDefOf.LeftArm);
            //BodyPartRecord rightArm = body.Find(x => x.def == DefDatabase<BodyPartDef>.GetNamed("RightShoulder"));
            BodyPartRecord leftHand = body.Find(x => x.customLabel == "left hand");
            BodyPartRecord rightHand = body.Find(x => x.customLabel == "right hand");

            BodyPartRecord leftFoot = body.Find(x => x.customLabel == "left foot");
            BodyPartRecord rightFoot = body.Find(x => x.customLabel == "right foot");

            BodyPartRecord leftArm = body.Find(x => x.customLabel == "left arm");
            BodyPartRecord rightArm = body.Find(x => x.customLabel == "right arm");

            BodyPartRecord leftLeg = body.Find(x => x.customLabel == "left foot");
            BodyPartRecord rightLeg = body.Find(x => x.customLabel == "right foot");

            if (missing)
            {
                CheckMissingParts(new BodyProps(hediff, anim, leftEye, rightEye, leftHand, rightHand, leftFoot,
                                                rightFoot));
                return;
            }

            // Missing parts first, hands and feet can be replaced by arms/legs
            //  Log.Message("Checking missing parts.");
            AddedBodyPartProps addedPartProps = hediff.def?.addedPartProps;
            if (addedPartProps == null)
            {
                //    Log.Message("No added parts found.");
                return;
            }

            if (hediff.def?.defName == null)
            {
                return;
            }



            //  Log.Message("Checking body for added parts.");

            CheckBodyForAddedParts(hediff, anim, leftHand, leftArm, rightHand, rightArm, leftFoot, leftLeg, rightFoot,
                rightLeg);
        }

        public static bool Aiming(this Pawn pawn)
        {
            return pawn.stances.curStance is Stance_Busy stanceBusy && !stanceBusy.neverAimWeapon &&
                   stanceBusy.focusTarg.IsValid;
        }

        public static bool ShowWeaponOpenly(this Pawn pawn)
        {
            return pawn.carryTracker?.CarriedThing == null && pawn.equipment?.Primary != null &&
                   (pawn.Drafted ||
                    (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon) ||
                    (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon));
        }


        public static bool CheckForAddedOrMissingParts(this Pawn pawn)
        {
            // if (!pawn.RaceProps.Humanlike)
            // {
            //     return;
            // }

            //   string log = "Checking for parts on " + pawn.LabelShort + " ...";
            /*
            if (!ShowMeYourHandsMod.instance.Settings.ShowExtraParts)
            {
                //      log += "\n" + "No extra parts in options, return";
                //      Log.Message(log);
                return false;
            }
*/
            // no head => no face
            if (!pawn.health.hediffSet.HasHead)
            {
                //      log += "\n" + "No head, return";
                //      Log.Message(log);
                return false;
            }

            if (pawn.GetCompBodyAnimator(out CompBodyAnimator anim))
            {
                anim.BodyStat.HandLeft = PartStatus.Natural;
                anim.BodyStat.HandRight = PartStatus.Natural;
                anim.BodyStat.FootLeft = PartStatus.Natural;
                anim.BodyStat.FootRight = PartStatus.Natural;
            }

            List<BodyPartRecord> allParts = pawn.RaceProps?.body?.AllParts;
            if (allParts.NullOrEmpty())
            {
                //     log += "\n" + "All parts null or empty, return";
                //     Log.Message(log);
                return false;
            }

            List<Hediff> hediffs = pawn.health?.hediffSet?.hediffs.Where(x => !x.def.defName.NullOrEmpty()).ToList();

            if (hediffs.NullOrEmpty())
            {
                // || hediffs.Any(x => x.def == HediffDefOf.MissingBodyPart && x.Part.def == BodyPartDefOf.Head))
                //     log += "\n" + "Hediffs null or empty, return";
                //     Log.Message(log);
                return false;
            }

            foreach (Hediff diff in hediffs.Where(diff => diff.def == HediffDefOf.MissingBodyPart))
            {
                // Log.Message("Checking missing part "+diff.def.defName);
                CheckPart(allParts, diff, anim, true);
            }

            foreach (Hediff diff in hediffs.Where(diff => diff.def.addedPartProps != null))
            {
                //  Log.Message("Checking added part on " + pawn + "--"+diff.def.defName);
                CheckPart(allParts, diff, anim, false);
            }

            return true;
        }

        public static bool Fleeing(this Pawn pawn)
        {
            Job job = pawn.CurJob;
            return pawn.MentalStateDef == MentalStateDefOf.PanicFlee
                || (job != null && (job.def == JobDefOf.Flee || job.def == JobDefOf.FleeAndCower));
        }

        [CanBeNull]
        public static CompBodyAnimator GetCompBodyAnimator([NotNull] this Pawn pawn)
        {
            return pawn.GetComp<CompBodyAnimator>();
        }

        public static bool GetCompBodyAnimator([NotNull] this Pawn pawn, [NotNull] out CompBodyAnimator compAnim)
        {
            compAnim = pawn.GetComp<CompBodyAnimator>();
            return compAnim != null;
        }
        /*
        [CanBeNull]
        public static CompFace GetCompFace([NotNull] this Pawn pawn)
        {
            return pawn.GetComp<CompFace>();
        }
        public static bool GetCompFace([NotNull] this Pawn pawn, [NotNull] out CompFace compFace)
        {
            compFace = pawn.GetComp<CompFace>();
            return compFace != null;
        }
        */
        /*
        public static bool GetPawnFace([NotNull] this Pawn pawn, [CanBeNull] out PawnFace pawnFace)
        {
            pawnFace = null;

            if (!pawn.GetCompFace(out CompFace compFace))
            {
                return false;
            }

            PawnFace face = compFace.PawnFace;
            if (face != null)
            {
                pawnFace = face;
                return true;
            }

            return false;
        }
        */
        public static bool HasCompAnimator([NotNull] this Pawn pawn)
        {
            return pawn.def.HasComp(typeof(CompBodyAnimator));
        }


        /*
        public static bool HasPawnFace([NotNull] this Pawn pawn)
        {
            if (pawn.GetCompFace(out CompFace compFace))
            {
                PawnFace face = compFace.PawnFace;
                return face != null;
            }

            return false;
        }
        */
    }
}