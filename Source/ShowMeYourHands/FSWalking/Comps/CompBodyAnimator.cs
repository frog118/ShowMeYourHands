using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using FacialStuff.Animator;
using FacialStuff.GraphicsFS;
using FacialStuff.Tweener;
using JetBrains.Annotations;
using RimWorld;
using ShowMeYourHands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FacialStuff.DefOfs;
using UnityEngine;
using Verse;
using Verse.AI;
using static System.Byte;

namespace FacialStuff
{
    [StaticConstructorOnStartup]
    public class CompBodyAnimator : ThingComp
    {
        #region Public Fields

        public bool Deactivated;
        public bool IgnoreRenderer;

        [CanBeNull] public BodyAnimDef BodyAnim;

        public BodyPartStats BodyStat;

        public float JitterMax = 0.35f;
        public readonly Vector3Tween[] Vector3Tweens = new Vector3Tween[(int)TweenThing.Max];

        //   [CanBeNull] public PawnPartsTweener PartTweener;

        [CanBeNull] public PawnBodyGraphic pawnBodyGraphic;

        //[CanBeNull] public PoseCycleDef PoseCycle;

        public Vector3 FirstHandPosition;
        public Vector3 SecondHandPosition;

        public void DoWalkCycleOffsets(ref Vector3 rightFoot,
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
            if (!this.IsMoving)
            {
                return;
            }
            float bodysizeScaling = GetBodysizeScaling(out _);
            float percent = this.MovedPercent;

            float flot = percent;
            if (flot <= 0.5f)
            {
                flot += 0.5f;
            }
            else
            {
                flot -= 0.5f;
            }

            Rot4 rot = this.CurrentRotation;
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

        public Vector3 lastMainHandPosition;

        public void DoWalkCycleOffsets(
        float armLength,
        ref Vector3 rightHand,
        ref Vector3 leftHand,
        ref List<float> shoulderAngle,
        ref List<float> handSwingAngle,
        ref JointLister shoulderPos,
        SimpleCurve cycleHandsSwingAngle,
        float offsetJoint)
        {
            // Has the pawn something in his hands?

            float bodysizeScaling = GetBodysizeScaling(out _);

            Rot4 rot = this.CurrentRotation;

            // Basic values if pawn is carrying stuff
            float x = 0;
            float x2 = -x;
            float y = Offsets.YOffset_Behind;
            float y2 = y;
            float z;
            float z2;

            // Offsets for hands from the pawn center
            z = z2 = -armLength;

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
            if (this.IsMoving)
            {
                WalkCycleDef walkCycle = this.WalkCycle;
                float percent = this.MovedPercent;
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

            rightHand = new Vector3(x, y, z) * bodysizeScaling;
            leftHand = new Vector3(x2, y2, z2) * bodysizeScaling;

            lastMainHandPosition = rightHand;
        }

        [NotNull]
        public WalkCycleDef WalkCycle
        {
            get => _walkCycle == null ? WalkCycleDefOf.Biped_Amble : _walkCycle;
            private set => _walkCycle = value;
        }

        #endregion Public Fields

        #region Private Fields
        private Rot4? _currentRotationOverride = null;

        private static readonly FieldInfo _infoJitterer;

        [NotNull] private readonly List<Material> _cachedNakedMatsBodyBase = new();

        private readonly List<Material> _cachedSkinMatsBodyBase = new();

        private int _cachedNakedMatsBodyBaseHash = -1;
        private int _cachedSkinMatsBodyBaseHash = -1;
        private int _lastRoomCheck;

        private bool _initialized;

        [CanBeNull] private Room _theRoom;

        #endregion Private Fields

        #region Public Properties

        public BodyAnimator BodyAnimator
        {
            get => _bodyAnimator;
            private set => _bodyAnimator = value;
        }

        public JitterHandler Jitterer
            => GetHiddenValue(typeof(Pawn_DrawTracker), this.pawn.Drawer, "jitterer", _infoJitterer) as
                JitterHandler;

        [NotNull]
        public Pawn pawn => this.parent as Pawn;

        public List<PawnBodyDrawer> pawnBodyDrawers
        {
            get => _pawnBodyDrawers;
            private set => _pawnBodyDrawers = value;
        }

        public CompProperties_BodyAnimator Props => (CompProperties_BodyAnimator)this.props;

        #endregion Public Properties

        #region Private Properties

        [CanBeNull]
        private Room TheRoom
        {
            get
            {
                if (this.pawn.Dead)
                {
                    return null;
                }

                if (Find.TickManager.TicksGame < this._lastRoomCheck + 60f)
                {
                    return this._theRoom;
                }

                this._theRoom = this.pawn.GetRoom();
                this._lastRoomCheck = Find.TickManager.TicksGame;

                return this._theRoom;
            }
        }

        #endregion Private Properties

        #region Public Methods

        public static object GetHiddenValue(Type type, object __instance, string fieldName, [CanBeNull] FieldInfo info)
        {
            if (info == null)
            {
                info = type.GetField(fieldName, GenGeneric.BindingFlagsAll);
            }

            return info?.GetValue(__instance);
        }

        public void ApplyBodyWobble(ref Vector3 rootLoc, ref Vector3 footPos)
        {
            if (this.pawnBodyDrawers == null)
            {
                return;
            }

            int i = 0;
            int count = this.pawnBodyDrawers.Count;
            while (i < count)
            {
                this.pawnBodyDrawers[i].ApplyBodyWobble(ref rootLoc, ref footPos);
                i++;
            }
        }

        // Verse.PawnGraphicSet
        public void ClearCache()
        {
            this._cachedSkinMatsBodyBaseHash = -1;
            this._cachedNakedMatsBodyBaseHash = -1;
        }

        public void SetBodyAngle()
        {
            WalkCycleDef walkCycle = this.WalkCycle;
            float movedPercent = this.MovedPercent;

            Rot4 currentRotation = this.CurrentRotation;
            if (currentRotation.IsHorizontal)
            {
                this.BodyAngle = (currentRotation == Rot4.West ? -1 : 1)
                                 * walkCycle.BodyAngle.Evaluate(movedPercent);
            }
            else
            {
                this.BodyAngle = (currentRotation == Rot4.South ? -1 : 1)
                                 * walkCycle.BodyAngleVertical.Evaluate(movedPercent);
            }

        }


        public void ModifyBodyAndFootPos(ref Vector3 rootLoc, ref Vector3 footPos)
        {
            float bodysizeScaling = GetBodysizeScaling(out _);
            float legModifier = this.BodyAnim.extraLegLength * bodysizeScaling;
            float posModB = legModifier * 0.75f;
            float posModF = -legModifier * 0.25f;
            Vector3 vector3 = new(0, 0, posModB);
            Vector3 vector4 = new(0, 0, posModF);

            // No rotation when moving
            if (this.IsMoving)
            {
                vector3 = vector3.RotatedBy(BodyAngle);
                vector4 = vector4.RotatedBy(BodyAngle);
            }

            if (!this.IsRider)
            {
            }

            rootLoc += vector3;
            footPos += vector4;
        }

        // public override string CompInspectStringExtra()
        // {
        //     string extra = this.Pawn.DrawPos.ToString();
        //     return extra;
        // }

        // off for now

        public void DrawFeet(Quaternion bodyQuat, Vector3 rootLoc, Vector3 bodyLoc, float factor = 1f)
        {
            if (!this.pawnBodyDrawers.NullOrEmpty())
            {
                int i = 0;
                int count = this.pawnBodyDrawers.Count;
                while (i < count)
                {
                    this.pawnBodyDrawers[i].DrawFeet(bodyQuat, rootLoc, bodyLoc, factor);
                    i++;
                }
            }
        }

        public void DrawHands(Quaternion bodyQuat, Vector3 rootLoc, [CanBeNull] Thing carriedThing = null,
            bool flip = false)
        {
            if (this.pawnBodyDrawers.NullOrEmpty())
            {
                return;
            }

            int i = 0;
            int count = this.pawnBodyDrawers.Count;
            while (i < count)
            {
                this.pawnBodyDrawers[i].DrawHands(bodyQuat, rootLoc, carriedThing, flip);
                i++;
            }
        }

        public void InitializePawnDrawer()
        {
            if (this.Props.bodyDrawers.Any())
            {
                this.pawnBodyDrawers = new List<PawnBodyDrawer>();
                for (int i = 0; i < this.Props.bodyDrawers.Count; i++)
                {
                    PawnBodyDrawer thingComp =
                    (PawnBodyDrawer)Activator.CreateInstance(this.Props.bodyDrawers[i].GetType());
                    thingComp.compAnimator = this;
                    thingComp.pawn = this.pawn;
                    this.pawnBodyDrawers.Add(thingComp);
                    thingComp.Initialize();
                }
            }
            else
            {
                this.pawnBodyDrawers = new List<PawnBodyDrawer>();
                bool isQuaduped = this.pawn.GetCompAnim().BodyAnim.quadruped;
                PawnBodyDrawer thingComp = isQuaduped
                    ? (PawnBodyDrawer)Activator.CreateInstance(typeof(QuadrupedDrawer))
                    : (PawnBodyDrawer)Activator.CreateInstance(typeof(HumanBipedDrawer));
                thingComp.compAnimator = this;
                thingComp.pawn = this.pawn;
                this.pawnBodyDrawers.Add(thingComp);
                thingComp.Initialize();
            }
        }

        public override string CompInspectStringExtra()
        {
            // var tween = Vector3Tweens[(int)TweenThing.Equipment];
            // var log = tween.State + " =>"+  tween.StartValue + " - " + tween.EndValue + " / " + tween.CurrentTime + " / " + tween.CurrentValue;
            // return log;
            //  return MoveState.ToString() + " - " + MovedPercent;

            //  return  lastAimAngle.ToString() ;

            return base.CompInspectStringExtra();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this._lastRoomCheck, "lastRoomCheck");
            // Scribe_Values.Look(ref this.PawnBodyGraphic, "PawnBodyGraphic");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            for (int i = 0; i < this.Vector3Tweens.Length; i++)
            {
                this.Vector3Tweens[i] = new Vector3Tween();
            }
            this.BodyAnimator = new BodyAnimator(this.pawn, this);
            this.pawnBodyGraphic = new PawnBodyGraphic(this);

            string bodyType = "Undefined";

            if (this.pawn.story?.bodyType != null)
            {
                bodyType = this.pawn.story.bodyType.ToString();
            }

            string defaultName = "BodyAnimDef_" + this.pawn.def.defName + "_" + bodyType;
            List<string> names = new()
            {
                defaultName,
                // "BodyAnimDef_" + ThingDefOf.Human.defName + "_" + bodyType
            };

            bool needsNewDef = true;
            foreach (string name in names)
            {
                BodyAnimDef dbDef = DefDatabase<BodyAnimDef>.GetNamedSilentFail(name);
                if (dbDef == null)
                {
                    continue;
                }

                this.BodyAnim = dbDef;
                needsNewDef = false;
                break;
            }

            if (needsNewDef)
            {
                this.BodyAnim = new BodyAnimDef { defName = defaultName, label = defaultName };
                DefDatabase<BodyAnimDef>.Add(this.BodyAnim);
            }
        }

        public void TickDrawers()
        {
            if (!this._initialized)
            {
                this.InitializePawnDrawer();
                this._initialized = true;
            }

            if (this.pawnBodyDrawers.NullOrEmpty())
            {
                return;
            }

            int i = 0;
            int count = this.pawnBodyDrawers.Count;
            while (i < count)
            {
                this.pawnBodyDrawers[i].Tick();
                i++;
            }

        }

        public List<Material> UnderwearMatsBodyBaseAt(Rot4 facing, RotDrawMode bodyCondition = RotDrawMode.Fresh)
        {
            int num = facing.AsInt + 1000 * (int)bodyCondition;
            if (num != this._cachedSkinMatsBodyBaseHash)
            {
                this._cachedSkinMatsBodyBase.Clear();
                this._cachedSkinMatsBodyBaseHash = num;
                PawnGraphicSet graphics = this.pawn.Drawer.renderer.graphics;
                if (bodyCondition == RotDrawMode.Fresh)
                {
                    this._cachedSkinMatsBodyBase.Add(graphics.nakedGraphic.MatAt(facing));
                }
                else if (bodyCondition == RotDrawMode.Rotting || graphics.dessicatedGraphic == null)
                {
                    this._cachedSkinMatsBodyBase.Add(graphics.rottingGraphic.MatAt(facing));
                }
                else if (bodyCondition == RotDrawMode.Dessicated)
                {
                    this._cachedSkinMatsBodyBase.Add(graphics.dessicatedGraphic.MatAt(facing));
                }

                for (int i = 0; i < graphics.apparelGraphics.Count; i++)
                {
                    ApparelLayerDef lastLayer = graphics.apparelGraphics[i].sourceApparel.def.apparel.LastLayer;

                    // if (lastLayer != ApparelLayerDefOf.Shell && lastLayer != ApparelLayerDefOf.Overhead)
                    if (lastLayer == ApparelLayerDefOf.OnSkin)
                    {
                        this._cachedSkinMatsBodyBase.Add(graphics.apparelGraphics[i].graphic.MatAt(facing));
                    }
                }
                // One more time to get at least one pieces of cloth
                if (this._cachedSkinMatsBodyBase.Count < 1)
                {
                    for (int i = 0; i < graphics.apparelGraphics.Count; i++)
                    {
                        ApparelLayerDef lastLayer = graphics.apparelGraphics[i].sourceApparel.def.apparel.LastLayer;

                        // if (lastLayer != ApparelLayerDefOf.Shell && lastLayer != ApparelLayerDefOf.Overhead)
                        if (lastLayer == ApparelLayerDefOf.Middle)
                        {
                            this._cachedSkinMatsBodyBase.Add(graphics.apparelGraphics[i].graphic.MatAt(facing));
                        }
                    }
                }
            }

            return this._cachedSkinMatsBodyBase;
        }

        #endregion Public Methods

        public float MovedPercent
        {
            get => _movedPercent;
            private set => _movedPercent = value;
        }

        public float BodyAngle = 0f;

        public float LastAimAngle = 143f;

        //  public float lastWeaponAngle = 53f;
        public readonly Vector3[] LastPosition = new Vector3[(int)TweenThing.Max];

        // public readonly FloatTween AimAngleTween = new();
        public bool HasLeftHandPosition => this.SecondHandPosition != Vector3.zero;

        public Vector3 LastEqPos = Vector3.zero;
        public float DrawOffsetY;
        private float bodysizeScaling = 0f;
        public Mesh pawnBodyMesh;
        public Mesh pawnBodyMeshFlipped;

        public void CheckMovement()
        {
            /*
            if (HarmonyPatchesFS.AnimatorIsOpen() && MainTabWindow_BaseAnimator.pawn == this.pawn)
            {
                this.IsMoving = true;
                this.MovedPercent = MainTabWindow_BaseAnimator.AnimationPercent;
                return;
            }
            */
            if (this.IsRider)
            {
                this.IsMoving = false;
                return;
            }
            // pawn started pathing

            this.MovedPercent = PawnMovedPercent(pawn);
        }
        private static float CalcShortestRot(float from, float to)
        {
            // If from or to is a negative, we have to recalculate them.
            // For an example, if from = -45 then from(-45) + 360 = 315.
            if (@from < 0)
            {
                @from += 360;
            }

            if (to < 0)
            {
                to += 360;
            }

            // Do not rotate if from == to.
            if (@from == to ||
                @from == 0 && to == 360 ||
                @from == 360 && to == 0)
            {
                return 0;
            }

            // Pre-calculate left and right.
            float left = (360 - @from) + to;
            float right = @from - to;

            // If from < to, re-calculate left and right.
            if (@from < to)
            {
                if (to > 0)
                {
                    left = to - @from;
                    right = (360 - to) + @from;
                }
                else
                {
                    left = (360 - to) + @from;
                    right = to - @from;
                }
            }

            // Determine the shortest direction.
            return ((left <= right) ? left : (right * -1));
        }


        private static int LastDrawn;
        private static Vector3 MainHand;
        private static Vector3 OffHand;

        private static Vector3 AdjustRenderOffsetFromDir(Pawn pawn, ThingWithComps weapon)
        {
            if (!ShowMeYourHandsMain.OversizedWeaponLoaded && !ShowMeYourHandsMain.EnableOversizedLoaded)
            {
                return Vector3.zero;
            }

            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    return ShowMeYourHandsMain.northOffsets.TryGetValue(weapon.def, out Vector3 northValue)
                        ? northValue
                        : Vector3.zero;

                case 1:
                    return ShowMeYourHandsMain.eastOffsets.TryGetValue(weapon.def, out Vector3 eastValue)
                        ? eastValue
                        : Vector3.zero;

                case 2:
                    return ShowMeYourHandsMain.southOffsets.TryGetValue(weapon.def, out Vector3 southValue)
                        ? southValue
                        : Vector3.zero;

                case 3:
                    return ShowMeYourHandsMain.westOffsets.TryGetValue(weapon.def, out Vector3 westValue)
                        ? westValue
                        : Vector3.zero;

                default:
                    return Vector3.zero;
            }
        }
        public static void SetPositionsForHandsOnWeapons(Pawn pawn, bool flipped,
    [CanBeNull] WhandCompProps compWeaponExtensions,
    CompBodyAnimator animator)
        {
            // Prepare everything for DrawHands, but don't draw
            if (compWeaponExtensions == null || pawn == null)
            {
                return;
            }

            ThingWithComps mainHandWeapon = pawn.equipment.Primary;
            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        mainHandWeapon.def.GetHashCode());
                }

                return;
            }
            WhandCompProps compProperties = mainHandWeapon.def.GetCompProperties<WhandCompProps>();
            if (compProperties != null)
            {
                MainHand = compProperties.MainHand;
                OffHand = compProperties.SecHand;
            }
            else
            {
                OffHand = Vector3.zero;
                MainHand = Vector3.zero;
            }

            ThingWithComps offHandWeapon = null;
            if (pawn.equipment.AllEquipmentListForReading.Count == 2)
            {
                offHandWeapon = (from weapon in pawn.equipment.AllEquipmentListForReading
                                 where weapon != mainHandWeapon
                                 select weapon).First();
                WhandCompProps offhandComp = offHandWeapon?.def.GetCompProperties<WhandCompProps>();
                if (offhandComp != null)
                {
                    OffHand = offhandComp.MainHand;
                }
            }

            float aimAngle = 0f;
            bool aiming = false;

            if (pawn.stances.curStance is Stance_Busy { neverAimWeapon: false, focusTarg.IsValid: true } stance_Busy)
            {
                Vector3 a = stance_Busy.focusTarg.HasThing
                    ? stance_Busy.focusTarg.Thing.DrawPos
                    : stance_Busy.focusTarg.Cell.ToVector3Shifted();

                if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                {
                    aimAngle = (a - pawn.DrawPos).AngleFlat();
                }

                aiming = true;
            }


            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        mainHandWeapon.def.GetHashCode());
                }

                return;
            }

            Vector3 mainWeaponLocation = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item1;
            float mainHandAngle = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2;
            Vector3 offhandWeaponLocation = Vector3.zero;
            float offHandAngle = mainHandAngle;
            float mainMeleeExtra = 0f;
            float offMeleeExtra = 0f;
            bool mainMelee = false;
            bool offMelee = false;

            if (offHandWeapon != null)
            {
                if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(offHandWeapon))
                {
                    if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                    {
                        Log.ErrorOnce(
                            $"[ShowMeYourHands]: Could not find the position for {offHandWeapon.def.label} from the mod {offHandWeapon.def.modContentPack.Name}, equipped by {pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                            offHandWeapon.def.GetHashCode());
                    }
                }
                else
                {
                    offhandWeaponLocation = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item1;
                    offHandAngle = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item2;
                }
            }

            bool idle = false;
            if (pawn.Rotation == Rot4.South || pawn.Rotation == Rot4.North)
            {
                idle = true;
            }

            mainHandAngle -= 90f;
            offHandAngle -= 90f;
            if (pawn.Rotation == Rot4.West || aimAngle is > 200f and < 340f)
            {
                flipped = true;
            }

            if (mainHandWeapon.def.IsMeleeWeapon)
            {
                mainMelee = true;
                mainMeleeExtra = 0.0001f;
                if (idle && offHandWeapon != null) //Dual wield idle vertical
                {
                    if (pawn.Rotation == Rot4.South)
                    {
                        mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                    }
                }
                else
                {
                    if (flipped)
                    {
                        mainHandAngle -= 180f;
                        mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                    }
                }
            }
            else
            {
                if (flipped)
                {
                    mainHandAngle -= 180f;
                }
            }

            if (offHandWeapon?.def?.IsMeleeWeapon == true)
            {
                offMelee = true;
                offMeleeExtra = 0.0001f;
                if (idle && pawn.Rotation == Rot4.North) //Dual wield north
                {
                    offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    if (flipped)
                    {
                        offHandAngle -= 180f;
                        offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        offHandAngle += offHandWeapon.def.equippedAngleOffset;
                    }
                }
            }
            else
            {
                if (flipped)
                {
                    offHandAngle -= 180f;
                }
            }

            mainHandAngle %= 360f;
            offHandAngle %= 360f;

            float drawSize = 1f;
            LastDrawn = GenTicks.TicksAbs;

            if (ShowMeYourHandsMod.instance.Settings.RepositionHands && mainHandWeapon.def.graphicData != null &&
                mainHandWeapon?.def?.graphicData?.drawSize.x != 1f)
            {
                drawSize = mainHandWeapon.def.graphicData.drawSize.x;
            }

            if (!PawnExtensions.pawnBodySizes.ContainsKey(pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
            {
                float bodySize = 1f;
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

            // ToDo Integrate: y >= 0 ? matSingle : offSingle

            // Only put the second hand on when aiming or not moving => free left hand for running
            //  bool leftOnWeapon = true;// aiming || !animator.IsMoving;

            if (MainHand != Vector3.zero)
            {
                float x = MainHand.x * drawSize;
                float z = MainHand.z * drawSize;
                float y = MainHand.y < 0 ? -0.0001f : 0.001f;

                if (flipped)
                {
                    x *= -1;
                }

                if (pawn.Rotation == Rot4.North && !mainMelee && !aiming)
                {
                    z += 0.1f;
                }

                animator.FirstHandPosition = mainWeaponLocation + AdjustRenderOffsetFromDir(pawn, mainHandWeapon as ThingWithComps);
                animator.FirstHandPosition += new Vector3(x, y + mainMeleeExtra, z).RotatedBy(mainHandAngle);
                animator.FirstHandQuat = Quaternion.AngleAxis(mainHandAngle, Vector3.up);
            }
            else
            {
                animator.FirstHandPosition = Vector3.zero;
            }


            if (OffHand != Vector3.zero)
            {
                float x2 = OffHand.x * drawSize;
                float z2 = OffHand.z * drawSize;
                float y2 = OffHand.y < 0 ? -0.0001f : 0.001f;


                if (offHandWeapon != null)
                {
                    drawSize = 1f;

                    if (ShowMeYourHandsMod.instance.Settings.RepositionHands && offHandWeapon.def.graphicData != null &&
                        offHandWeapon.def?.graphicData?.drawSize.x != 1f)
                    {
                        drawSize = offHandWeapon.def.graphicData.drawSize.x;
                    }

                    x2 = OffHand.x * drawSize;
                    z2 = OffHand.z * drawSize;

                    if (flipped)
                    {
                        x2 *= -1;
                    }

                    if (idle && !offMelee)
                    {
                        if (pawn.Rotation == Rot4.South)
                        {
                            z2 += 0.05f;
                        }
                        else
                        {
                            z2 -= 0.05f;
                        }
                    }


                    animator.SecondHandPosition = offhandWeaponLocation + AdjustRenderOffsetFromDir(pawn, offHandWeapon as ThingWithComps);
                    animator.SecondHandPosition += new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle);

                    animator.SecondHandQuat = Quaternion.AngleAxis(offHandAngle, Vector3.up);
                }

            }
            else
            {
                animator.SecondHandPosition = Vector3.zero;
            }

        }

        public  void DoHandOffsetsOnWeapon(ThingWithComps eq, float aimAngle)
        {
            WhandCompProps extensions = eq?.def?.GetCompProperties<WhandCompProps>();

            Pawn ___pawn = this.pawn;

            if ((___pawn == null) || (extensions == null))
            {
                return;
            }

            bool flipped = ___pawn.Rotation == Rot4.West || aimAngle is > 200f and < 340f;

            // Prepare everything for DrawHands, but don't draw
            if (extensions == null)
            {
                return;
            }

            ThingWithComps mainHandWeapon = eq;
            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {___pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        mainHandWeapon.def.GetHashCode());
                }

                return;
            }
            WhandCompProps compProperties = mainHandWeapon.def.GetCompProperties<WhandCompProps>();
            if (compProperties != null)
            {
                MainHand = compProperties.MainHand;
                OffHand = compProperties.SecHand;
            }
            else
            {
                OffHand = Vector3.zero;
                MainHand = Vector3.zero;
            }

            ThingWithComps offHandWeapon = null;
            if (___pawn.equipment.AllEquipmentListForReading.Count == 2)
            {
                offHandWeapon = (from weapon in ___pawn.equipment.AllEquipmentListForReading
                                 where weapon != mainHandWeapon
                                 select weapon).First();
                WhandCompProps offhandComp = offHandWeapon?.def.GetCompProperties<WhandCompProps>();
                if (offhandComp != null)
                {
                    OffHand = offhandComp.MainHand;
                }
            }

            float aimAngle1 = 0f;
            bool aiming = false;

            if (___pawn.stances.curStance is Stance_Busy { neverAimWeapon: false, focusTarg.IsValid: true } stance_Busy)
            {
                Vector3 a = stance_Busy.focusTarg.HasThing
                    ? stance_Busy.focusTarg.Thing.DrawPos
                    : stance_Busy.focusTarg.Cell.ToVector3Shifted();

                if ((a - ___pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                {
                    aimAngle1 = (a - ___pawn.DrawPos).AngleFlat();
                }

                aiming = true;
            }


            if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(mainHandWeapon))
            {
                if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                {
                    Log.ErrorOnce(
                        $"[ShowMeYourHands]: Could not find the position for {mainHandWeapon.def.label} from the mod {mainHandWeapon.def.modContentPack.Name}, equipped by {___pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                        mainHandWeapon.def.GetHashCode());
                }

                return;
            }

            Vector3 mainWeaponLocation = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item1;
            float mainHandAngle = ShowMeYourHandsMain.weaponLocations[mainHandWeapon].Item2;
            Vector3 offhandWeaponLocation = Vector3.zero;
            float offHandAngle = mainHandAngle;
            float mainMeleeExtra = 0f;
            float offMeleeExtra = 0f;
            bool mainMelee = false;
            bool offMelee = false;

            if (offHandWeapon != null)
            {
                if (!ShowMeYourHandsMain.weaponLocations.ContainsKey(offHandWeapon))
                {
                    if (ShowMeYourHandsMod.instance.Settings.VerboseLogging)
                    {
                        Log.ErrorOnce(
                            $"[ShowMeYourHands]: Could not find the position for {offHandWeapon.def.label} from the mod {offHandWeapon.def.modContentPack.Name}, equipped by {___pawn.Name}. Please report this issue to the author of Show Me Your Hands if possible.",
                            offHandWeapon.def.GetHashCode());
                    }
                }
                else
                {
                    offhandWeaponLocation = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item1;
                    offHandAngle = ShowMeYourHandsMain.weaponLocations[offHandWeapon].Item2;
                }
            }

            bool idle = false;
            if (___pawn.Rotation == Rot4.South || ___pawn.Rotation == Rot4.North)
            {
                idle = true;
            }

            mainHandAngle -= 90f;
            offHandAngle -= 90f;
            if (___pawn.Rotation == Rot4.West || aimAngle1 is > 200f and < 340f)
            {
                flipped = true;
            }

            if (mainHandWeapon.def.IsMeleeWeapon)
            {
                mainMelee = true;
                mainMeleeExtra = 0.0001f;
                if (idle && offHandWeapon != null) //Dual wield idle vertical
                {
                    if (___pawn.Rotation == Rot4.South)
                    {
                        mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                    }
                }
                else
                {
                    if (flipped)
                    {
                        mainHandAngle -= 180f;
                        mainHandAngle -= mainHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        mainHandAngle += mainHandWeapon.def.equippedAngleOffset;
                    }
                }
            }
            else
            {
                if (flipped)
                {
                    mainHandAngle -= 180f;
                }
            }

            if (offHandWeapon?.def?.IsMeleeWeapon == true)
            {
                offMelee = true;
                offMeleeExtra = 0.0001f;
                if (idle && ___pawn.Rotation == Rot4.North) //Dual wield north
                {
                    offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                }
                else
                {
                    if (flipped)
                    {
                        offHandAngle -= 180f;
                        offHandAngle -= offHandWeapon.def.equippedAngleOffset;
                    }
                    else
                    {
                        offHandAngle += offHandWeapon.def.equippedAngleOffset;
                    }
                }
            }
            else
            {
                if (flipped)
                {
                    offHandAngle -= 180f;
                }
            }

            mainHandAngle %= 360f;
            offHandAngle %= 360f;

            float drawSize = 1f;
            LastDrawn = GenTicks.TicksAbs;

            if (ShowMeYourHandsMod.instance.Settings.RepositionHands && mainHandWeapon.def.graphicData != null &&
                mainHandWeapon?.def?.graphicData?.drawSize.x != 1f)
            {
                drawSize = mainHandWeapon.def.graphicData.drawSize.x;
            }

            if (!PawnExtensions.pawnBodySizes.ContainsKey(___pawn) || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
            {
                float bodySize = 1f;
                if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
                {
                    if (___pawn.RaceProps != null)
                    {
                        bodySize = ___pawn.RaceProps.baseBodySize;
                    }

                    if (ShowMeYourHandsMain.BabysAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                    {
                        bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] { ___pawn });
                    }
                }

                PawnExtensions.pawnBodySizes[___pawn] = 0.8f * bodySize;
            }

            // ToDo Integrate: y >= 0 ? matSingle : offSingle

            // Only put the second hand on when aiming or not moving => free left hand for running
            //  bool leftOnWeapon = true;// aiming || !animator.IsMoving;

            if (MainHand != Vector3.zero)
            {
                float x = MainHand.x * drawSize;
                float z = MainHand.z * drawSize;
                float y = MainHand.y < 0 ? -0.0001f : 0.001f;

                if (flipped)
                {
                    x *= -1;
                }

                if (___pawn.Rotation == Rot4.North && !mainMelee && !aiming)
                {
                    z += 0.1f;
                }

                this.FirstHandPosition = mainWeaponLocation + AdjustRenderOffsetFromDir(___pawn, mainHandWeapon as ThingWithComps);
                FirstHandPosition += new Vector3(x, y + mainMeleeExtra, z).RotatedBy(mainHandAngle);
                this.FirstHandQuat = Quaternion.AngleAxis(mainHandAngle, Vector3.up);
            }
            else
            {
                this.FirstHandPosition = Vector3.zero;
            }


            if (OffHand != Vector3.zero)
            {
                float x2 = OffHand.x * drawSize;
                float z2 = OffHand.z * drawSize;
                float y2 = OffHand.y < 0 ? -0.0001f : 0.001f;


                if (offHandWeapon != null)
                {
                    drawSize = 1f;

                    if (ShowMeYourHandsMod.instance.Settings.RepositionHands && offHandWeapon.def.graphicData != null &&
                        offHandWeapon.def?.graphicData?.drawSize.x != 1f)
                    {
                        drawSize = offHandWeapon.def.graphicData.drawSize.x;
                    }

                    x2 = OffHand.x * drawSize;
                    z2 = OffHand.z * drawSize;

                    if (flipped)
                    {
                        x2 *= -1;
                    }

                    if (idle && !offMelee)
                    {
                        if (___pawn.Rotation == Rot4.South)
                        {
                            z2 += 0.05f;
                        }
                        else
                        {
                            z2 -= 0.05f;
                        }
                    }


                    this.SecondHandPosition = offhandWeaponLocation + AdjustRenderOffsetFromDir(___pawn, offHandWeapon as ThingWithComps);
                    this.SecondHandPosition += new Vector3(x2, y2 + offMeleeExtra, z2).RotatedBy(offHandAngle);

                    this.SecondHandQuat = Quaternion.AngleAxis(offHandAngle, Vector3.up);
                }

            }
            else
            {
                this.SecondHandPosition = Vector3.zero;
            }
        }


        public float GetBodysizeScaling(out Mesh pawnBodyMesh)
        {
            if (bodysizeScaling == 0f || GenTicks.TicksAbs % GenTicks.TickLongInterval == 0)
            {
                float bodySize = 1f;
                if (pawn.story == null) // mechanoids and animals
                {
                    if (this.pawn.kindDef.lifeStages.Any())
                    {
                        Vector2 maxSize = this.pawn.kindDef.lifeStages.Last().bodyGraphicData.drawSize;
                        Vector2 sizePaws = this.pawn.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize;
                        bodySize = sizePaws.x / maxSize.x;
                    }

                    bodysizeScaling = bodySize;
                }
                else if (ShowMeYourHandsMod.instance.Settings.ResizeHands)
                {
                    if (pawn.RaceProps != null)
                    {
                        bodySize = pawn.RaceProps.baseBodySize;
                    }

                    if (ShowMeYourHandsMain.BabysAndChildrenLoaded && ShowMeYourHandsMain.GetBodySizeScaling != null)
                    {
                        bodySize = (float)ShowMeYourHandsMain.GetBodySizeScaling.Invoke(null, new object[] { pawn });
                    }
                    bodysizeScaling = 0.8f * bodySize;
                }

                this.pawnBodyMesh = MeshMakerPlanes.NewPlaneMesh(bodysizeScaling* this.BodyAnim.extremitySize);
                this.pawnBodyMeshFlipped = MeshMakerPlanes.NewPlaneMesh(bodysizeScaling* this.BodyAnim.extremitySize, true);
            }
            pawnBodyMesh = this.pawnBodyMesh;
            return bodysizeScaling;
        }

        private float PawnMovedPercent(Pawn pawn)
        {
            this.IsMoving = false;
            Pawn_PathFollower pather = pawn?.pather;
            if (pather == null)
            {
                return 0f;
            }

            if (!pather.Moving) return 0f;

            if (pawn.stances.FullBodyBusy)
            {
                return 0f;
            }

            if (pather.BuildingBlockingNextPathCell() != null)
            {
                return 0f;
            }

            if (pather.NextCellDoorToWaitForOrManuallyOpen() != null)
            {
                return 0f;
            }

            if (pather.WillCollideWithPawnOnNextPathCell())
            {
                return 0f;
            }

            this.IsMoving = true;
            return 1f - pather.nextCellCostLeft / pather.nextCellCostTotal;

        }

        public bool IsRider = false;

        public void SetWalkCycle(WalkCycleDef walkCycleDef)
        {
            this.WalkCycle = walkCycleDef;
        }

        public float HeadffsetZ
        {
            get
            {
                if (ShowMeYourHandsMod.instance.Settings.UseFeet)
                {
                    WalkCycleDef walkCycle = this.WalkCycle;
                    if (walkCycle != null)
                    {
                        SimpleCurve curve = walkCycle.HeadOffsetZ;
                        if (curve.PointsCount > 0)
                        {
                            return curve.Evaluate(this.MovedPercent);
                        }
                    }
                }

                return 0f;
            }
        }

        // unused since 1.3
        public float HeadAngleX
        {
            get
            {
                if (ShowMeYourHandsMod.instance.Settings.UseFeet)
                {
                    WalkCycleDef walkCycle = this.WalkCycle;
                    if (walkCycle != null)
                    {
                        SimpleCurve curve = walkCycle.HeadAngleX;
                        if (curve.PointsCount > 0)
                        {
                            return curve.Evaluate(this.MovedPercent);
                        }
                    }
                }

                return 0f;
            }
        }

        public float BodyOffsetZ
        {
            get
            {
                if (ShowMeYourHandsMod.instance.Settings.UseFeet)
                {
                    WalkCycleDef walkCycle = this.WalkCycle;

                    SimpleCurve curve = walkCycle.BodyOffsetZ;
                    if (curve.PointsCount > 0)
                    {
                        return curve.Evaluate(this.MovedPercent);
                    }
                }

                return 0f;
            }
        }

        public bool IsMoving;



        public Quaternion SecondHandQuat;
        public Quaternion FirstHandQuat;

        internal bool MeshFlipped;
        internal float LastWeaponAngle;
        internal readonly int[] LastPosUpdate = new int[(int)TweenThing.Max];
        internal int LastAngleTick;

        public Color HandColorLeft;
        public Color HandColorRight;
        public Color FootColorLeft;
        public Color FootColorRight;



        public float Offset_Angle = 0f;

        public Vector3 Offset_Pos = Vector3.zero;
        [CanBeNull] private WalkCycleDef _walkCycle;
        private BodyAnimator _bodyAnimator;
        private List<PawnBodyDrawer> _pawnBodyDrawers;
        private float _movedPercent;

        public Rot4 CurrentRotation
        {
            get => _currentRotationOverride ?? pawn.Rotation;
            set => _currentRotationOverride = value;
        }



        public string TexNameHand()
        {
            string texNameHand;
            // Mechanoid
            if (this.pawn.story == null)
            {
                PawnKindLifeStage curKindLifeStage = this.pawn.ageTracker.CurKindLifeStage;

                //skinColor = curKindLifeStage.bodyGraphicData.color;
                texNameHand = PawnExtensions.PathAnimals + "Paws/" + this.Props.handType + PawnExtensions.STR_Hand;
            }
            else
            {
                //skinColor = this._pawn.story.SkinColor;
                texNameHand = PawnExtensions.PathHumanlike + "Hands/" + this.Props.handType + PawnExtensions.STR_Hand;
            }

            return texNameHand;
        }

        public string TexNameFoot()
        {
            string texNameFoot;
            if (pawn.RaceProps.Humanlike)
            {
                texNameFoot = PawnExtensions.PathHumanlike + "Feet/" + this.Props.handType + PawnExtensions.STR_Foot;
            }
            else
            {
                texNameFoot = PawnExtensions.PathAnimals + "Paws/" + this.Props.handType + PawnExtensions.STR_Foot;
            }

            return texNameFoot;
        }
    }

    public static class HarmonyPatchesFS
    {
        // public static bool AnimatorIsOpen()
        // { return Find.WindowStack.IsOpen(typeof(MainTabWindow_WalkAnimator));// MainTabWindow_WalkAnimator.IsOpen;// || MainTabWindow_PoseAnimator.IsOpen;}
    }
}