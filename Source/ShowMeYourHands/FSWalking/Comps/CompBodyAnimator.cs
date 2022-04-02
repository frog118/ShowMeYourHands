using FacialStuff.Animator;
using FacialStuff.GraphicsFS;
using FacialStuff.Tweener;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using FacialStuff.AnimatorWindows;
using ShowMeYourHands;
using ShowMeYourHands.FSWalking;
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

        private static Dictionary<Thing, Color> colorDictionary;


        public bool Deactivated;
        public bool IgnoreRenderer;

        [CanBeNull] public BodyAnimDef BodyAnim;

        public BodyPartStats BodyStat;

        public float JitterMax = 0.35f;
        public readonly Vector3Tween[] Vector3Tweens = new Vector3Tween[(int)TweenThing.Max];

        //   [CanBeNull] public PawnPartsTweener PartTweener;

        [CanBeNull] public PawnBodyGraphic pawnBodyGraphic;

        [CanBeNull] public PoseCycleDef PoseCycle;

        public Vector3 FirstHandPosition;
        public Vector3 SecondHandPosition;

        [CanBeNull]
        public WalkCycleDef WalkCycle { get; private set; }


        #endregion Public Fields

        #region Private Fields

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

        public BodyAnimator BodyAnimator { get; private set; }




        public JitterHandler Jitterer
            => GetHiddenValue(typeof(Pawn_DrawTracker), this.pawn.Drawer, "jitterer", _infoJitterer) as
                JitterHandler;

        [NotNull]
        public Pawn pawn => this.parent as Pawn;

        public List<PawnBodyDrawer> pawnBodyDrawers { get; private set; }

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
                    thingComp.CompAnimator = this;
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
                thingComp.CompAnimator = this;
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
            this.pawn.CheckForAddedOrMissingParts();
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

        public float MovedPercent { get; private set; }
        public float BodyAngle;

        public float LastAimAngle = 143f;


        //  public float lastWeaponAngle = 53f;
        public readonly Vector3[] LastPosition = new Vector3[(int)TweenThing.Max];

        public readonly FloatTween AimAngleTween = new();
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

                this.pawnBodyMesh = MeshMakerPlanes.NewPlaneMesh(bodysizeScaling);
                this.pawnBodyMeshFlipped = MeshMakerPlanes.NewPlaneMesh(bodysizeScaling, true);

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

            if (pather.Moving)
            {
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

            return 0f;
        }
        public bool IsRider { get; set; } = false;


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
                    if (walkCycle != null)
                    {
                        SimpleCurve curve = walkCycle.BodyOffsetZ;
                        if (curve.PointsCount > 0)
                        {
                            return curve.Evaluate(this.MovedPercent);
                        }
                    }
                }

                return 0f;
            }
        }

        public bool IsMoving { get; private set; }

        public Color HandColor
        {
            get
            {
                if (GenTicks.TicksAbs % 100 != 0 && handColor != default)
                {
                    return handColor;
                }

                if (pawn == null || !pawn.GetCompAnim(out CompBodyAnimator anim))
                {
                    return Color.white;
                }
                string texNameHand = TexNameHand();

                handColor = getHandColor(pawn, out bool hasGloves, out Color secondColor);
                if (anim?.pawnBodyGraphic?.HandGraphicRight == null || anim.pawnBodyGraphic.HandGraphicRight.color != handColor)
                {
                    if (hasGloves)
                    {
                        anim.pawnBodyGraphic.HandGraphicRight = GraphicDatabase.Get<Graphic_Multi>(texNameHand, ShaderDatabase.Cutout, //"HandClean"
                            new Vector2(1f, 1f),
                            handColor, handColor);
                    }
                    else
                    {
                        anim.pawnBodyGraphic.HandGraphicRight = GraphicDatabase.Get<Graphic_Multi>(texNameHand, ShaderDatabase.CutoutSkin,
                            new Vector2(1f, 1f),
                            handColor, handColor);
                    }
                }

                if (anim.pawnBodyGraphic.HandGraphicLeft != null && anim.pawnBodyGraphic.HandGraphicLeft.color == handColor)
                {
                    return handColor;
                }

                if (hasGloves)
                {
                    anim.pawnBodyGraphic.HandGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(texNameHand,
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        handColor, handColor);
                }
                else
                {
                    if (secondColor != default)
                    {
                        anim.pawnBodyGraphic.HandGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(texNameHand,
                            ShaderDatabase.CutoutSkin,
                            new Vector2(1f, 1f),
                            secondColor, secondColor);
                    }
                    else
                    {
                        anim.pawnBodyGraphic.HandGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(texNameHand, ShaderDatabase.CutoutSkin,
                            new Vector2(1f, 1f),
                            handColor, handColor);
                    }
                }

                return handColor;
            }
            set => handColor = value;
        }

        public Color FootColor
        {
            get
            {
                if (GenTicks.TicksAbs % 100 != 0 && footColor != default)
                {
                    return footColor;
                }

                if (pawn == null || !pawn.GetCompAnim(out CompBodyAnimator anim))
                {
                    return Color.white;
                }
                string texNameFoot = TexNameFoot();

                footColor = getFeetColor(pawn, out bool hasShoes, out Color secondColor);
                if (anim?.pawnBodyGraphic?.FootGraphicRight == null || anim.pawnBodyGraphic.FootGraphicRight.color != footColor)
                {
                    if (hasShoes)
                    {
                        // ToDo TEXTURES!!!!
                        anim.pawnBodyGraphic.FootGraphicRight = GraphicDatabase.Get<Graphic_Multi>(texNameFoot, ShaderDatabase.Cutout,
                            new Vector2(1f, 1f),
                            footColor, footColor);
                    }
                    else
                    {
                        anim.pawnBodyGraphic.FootGraphicRight = GraphicDatabase.Get<Graphic_Multi>(texNameFoot, ShaderDatabase.CutoutSkin,
                            new Vector2(1f, 1f),
                            footColor, footColor);
                    }
                }

                if (anim.pawnBodyGraphic.FootGraphicLeft != null && anim.pawnBodyGraphic.FootGraphicLeft.color == footColor)
                {
                    return footColor;
                }
                
                if (hasShoes)
                {
                    anim.pawnBodyGraphic.FootGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(texNameFoot,
                        ShaderDatabase.Cutout,
                        new Vector2(1f, 1f),
                        footColor, footColor);
                }
                else
                {
                    if (secondColor != default)
                    {
                        anim.pawnBodyGraphic.FootGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(texNameFoot,
                            ShaderDatabase.CutoutSkin,
                            new Vector2(1f, 1f),
                            secondColor, secondColor);
                    }
                    else
                    {
                        anim.pawnBodyGraphic.FootGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(texNameFoot, ShaderDatabase.Cutout,
                            new Vector2(1f, 1f),
                            footColor, footColor);
                    }
                }

                return footColor;
            }
            set => footColor = value;
        }


        public Quaternion SecondHandQuat;
        public Quaternion FirstHandQuat;

        internal bool MeshFlipped;
        internal float LastWeaponAngle;
        internal readonly int[] LastPosUpdate = new int[(int)TweenThing.Max];
        internal int LastAngleTick;
        private Color handColor;
        private Color footColor;
        private bool pawnsMissingAFoot;
        private Rot4? _currentRotationOverride = null;
        public float Offset_Angle = 0f;
        public Vector3 Offset_Pos = new();

        public Rot4 CurrentRotation
        {
            get => _currentRotationOverride ?? pawn.Rotation;
            set => _currentRotationOverride = value;
        }
        private Color getHandColor(Pawn pawn, out bool hasGloves, out Color secondColor)
        {
            hasGloves = false;
            secondColor = default;
            List<Hediff_AddedPart> addedHands = null;

            if (pawn.story == null) // mechanoid or animal
            {
                PawnKindLifeStage curKindLifeStage = pawn.ageTracker.CurKindLifeStage;

                return curKindLifeStage.bodyGraphicData.color;
            }

            if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts ||
                ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
            {
                addedHands = pawn.health?.hediffSet?.GetHediffs<Hediff_AddedPart>()
                    .Where(x => x.Part.def == ShowMeYourHandsMain.HandDef ||
                                x.Part.parts.Any(record => record.def == ShowMeYourHandsMain.HandDef)).ToList();
            }

            if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts && pawn.health is { hediffSet: { } })
            {
                /*

                pawn.GetCompAnim(out CompBodyAnimator anim);
                pawnsMissingAHand = pawn.health
                        .hediffSet
                        .GetNotMissingParts().Count(record => record.def == ShowMeYourHandsMain.HandDef) +
                    addedHands?.Count < 2;*/
            }

            if (!ShowMeYourHandsMod.instance.Settings.MatchArmorColor || !(from apparel in pawn.apparel.WornApparel
                    where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                    select apparel).Any())
            {
                if (!ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
                {
                    return pawn.story.SkinColor;
                }

                if (addedHands == null || !addedHands.Any())
                {
                    return pawn.story.SkinColor;
                }

                Color mainColor = (Color)default;

                foreach (Hediff_AddedPart hediffAddedPart in addedHands)
                {
                    if (!ShowMeYourHandsMain.HediffColors.ContainsKey(hediffAddedPart.def))
                    {
                        continue;
                    }

                    if (mainColor == default)
                    {
                        mainColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
                        continue;
                    }

                    secondColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
                }

                if (mainColor == default)
                {
                    return pawn.story.SkinColor;
                }

                if (secondColor == default)
                {
                    secondColor = pawn.story.SkinColor;
                }

                return mainColor;
            }

            IEnumerable<Apparel> handApparel = from apparel in pawn.apparel.WornApparel
                where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Hands")
                select apparel;

            //ShowMeYourHandsMain.LogMessage($"Found gloves on {pawn.NameShortColored}: {string.Join(",", handApparel)}");

            Thing outerApparel = null;
            int highestDrawOrder = 0;
            foreach (Apparel thing in handApparel)
            {
                int thingOutmostLayer =
                    thing.def.apparel.layers.OrderByDescending(def => def.drawOrder).First().drawOrder;
                if (outerApparel != null && highestDrawOrder >= thingOutmostLayer)
                {
                    continue;
                }

                highestDrawOrder = thingOutmostLayer;
                outerApparel = thing;
            }

            if (outerApparel == null)
            {
                return pawn.story.SkinColor;
            }

            hasGloves = true;
            if (colorDictionary == null)
            {
                colorDictionary = new Dictionary<Thing, Color>();
            }

            if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
            {
                CompColorable comp = outerApparel.TryGetComp<CompColorable>();
                if (comp.Active)
                {
                    return comp.Color;
                }
            }

            if (colorDictionary.ContainsKey(outerApparel))
            {
                return colorDictionary[outerApparel];
            }

            if (outerApparel.Stuff != null && outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
            {
                colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
            }
            else
            {
                colorDictionary[outerApparel] =
                    AverageColorFromTexture((Texture2D)outerApparel.Graphic.MatSingle.mainTexture);
            }

            return colorDictionary[outerApparel];
        }

        private Color getFeetColor(Pawn pawn, out bool hasShoes, out Color secondColor)
        {
            hasShoes = false;
            secondColor = default;
            List<Hediff_AddedPart> addedFeet = null;

            if (pawn.story == null) // mechanoid or animal
            {
                PawnKindLifeStage curKindLifeStage = pawn.ageTracker.CurKindLifeStage;

                return curKindLifeStage.bodyGraphicData.color;
            }



            if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts ||
                ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
            {
                addedFeet = pawn.health?.hediffSet?.GetHediffs<Hediff_AddedPart>()
                    .Where(x => x.Part.def == ShowMeYourHandsMain.FootDef ||
                                x.Part.parts.Any(record => record.def == ShowMeYourHandsMain.FootDef)).ToList();
            }

            if (ShowMeYourHandsMod.instance.Settings.MatchHandAmounts && pawn.health is { hediffSet: { } })
            {
                /*
                pawn.GetCompAnim(out CompBodyAnimator anim);

                pawnsMissingAFoot = pawn.health
                        .hediffSet
                        .GetNotMissingParts().Count(record => record.def == ShowMeYourHandsMain.FootDef) +
                    addedFeet?.Count < 2;
                */
            }

            if (!ShowMeYourHandsMod.instance.Settings.MatchArmorColor || !(from apparel in pawn.apparel.WornApparel
                                                                           where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Feet")
                                                                           select apparel).Any())
            {
                if (!ShowMeYourHandsMod.instance.Settings.MatchArtificialLimbColor)
                {
                    return pawn.story.SkinColor;
                }

                if (addedFeet == null || !addedFeet.Any())
                {
                    return pawn.story.SkinColor;
                }

                Color mainColor = (Color)default;

                foreach (Hediff_AddedPart hediffAddedPart in addedFeet)
                {
                    if (!ShowMeYourHandsMain.HediffColors.ContainsKey(hediffAddedPart.def))
                    {
                        continue;
                    }

                    if (mainColor == default)
                    {
                        mainColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
                        continue;
                    }

                    secondColor = ShowMeYourHandsMain.HediffColors[hediffAddedPart.def];
                }

                if (mainColor == default)
                {
                    return pawn.story.SkinColor;
                }

                if (secondColor == default)
                {
                    secondColor = pawn.story.SkinColor;
                }

                return mainColor;
            }

            IEnumerable<Apparel> footApparel = from apparel in pawn.apparel.WornApparel
                                               where apparel.def.apparel.bodyPartGroups.Any(def => def.defName == "Feet")
                                               select apparel;

            //ShowMeYourHandsMain.LogMessage($"Found gloves on {pawn.NameShortColored}: {string.Join(",", footApparel)}");

            Thing outerApparel = null;
            int highestDrawOrder = 0;
            if (!footApparel.Any())
            {
                return pawn.story.SkinColor;
            }
                foreach (Apparel thing in footApparel)
                {
                    int thingOutmostLayer =
                        thing.def.apparel.layers.OrderByDescending(def => def.drawOrder).First().drawOrder;
                    if (outerApparel != null && highestDrawOrder >= thingOutmostLayer)
                    {
                        continue;
                    }

                    highestDrawOrder = thingOutmostLayer;
                    outerApparel = thing;
                }

            if (outerApparel == null)
            {
                return pawn.story.SkinColor;
            }

            hasShoes = true;
            if (colorDictionary == null)
            {
                colorDictionary = new Dictionary<Thing, Color>();
            }

            if (ShowMeYourHandsMain.IsColorable.Contains(outerApparel.def))
            {
                CompColorable comp = outerApparel.TryGetComp<CompColorable>();
                if (comp.Active)
                {
                    return comp.Color;
                }
            }

            if (colorDictionary.ContainsKey(outerApparel))
            {
                return colorDictionary[outerApparel];
            }

            if (outerApparel.Stuff != null && outerApparel.Graphic.Shader != ShaderDatabase.CutoutComplex)
            {
                colorDictionary[outerApparel] = outerApparel.def.GetColorForStuff(outerApparel.Stuff);
            }
            else
            {
                colorDictionary[outerApparel] =
                    AverageColorFromTexture((Texture2D)outerApparel.Graphic.MatSingle.mainTexture);
            }

            return colorDictionary[outerApparel];
        }


        private Color32 AverageColorFromTexture(Texture2D texture)
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D tex = new(texture.width, texture.height);
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return AverageColorFromColors(tex.GetPixels32());
        }

        private Color32 AverageColorFromColors(Color32[] colors)
        {
            Dictionary<Color32, int> shadeDictionary = new();
            foreach (Color32 texColor in colors)
            {
                if (texColor.a < 50)
                {
                    // Ignore low transparency
                    continue;
                }

                Rgb currentRgb = new() { B = texColor.b, G = texColor.b, R = texColor.r };

                if (currentRgb.Compare(new Rgb { B = 0, G = 0, R = 0 }, new Cie1976Comparison()) < 2)
                {
                    // Ignore black pixels
                    continue;
                }

                if (shadeDictionary.Count == 0)
                {
                    shadeDictionary[texColor] = 1;
                    continue;
                }


                bool added = false;
                foreach (Color32 rgb in shadeDictionary.Keys.Where(rgb =>
                             currentRgb.Compare(new Rgb { B = rgb.b, G = rgb.b, R = rgb.r }, new Cie1976Comparison()) < 2))
                {
                    shadeDictionary[rgb]++;
                    added = true;
                    break;
                }

                if (!added)
                {
                    shadeDictionary[texColor] = 1;
                }
            }

            if (shadeDictionary.Count == 0)
            {
                return new Color32(0, 0, 0, MaxValue);
            }

            Color32 greatestValue = shadeDictionary.Aggregate((rgb, max) => rgb.Value > max.Value ? rgb : max).Key;
            greatestValue.a = MaxValue;
            return greatestValue;
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