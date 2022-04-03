// ReSharper disable StyleCop.SA1401

using System;
using UnityEngine;
using Verse;

namespace FacialStuff.GraphicsFS
{
    public class PawnBodyGraphic
    {
        #region Public Fields
        private readonly Pawn _pawn;

        public readonly CompBodyAnimator CompAni;
        public Graphic FootGraphicLeft;
        public Graphic FootGraphicLeftCol;
        public Graphic FootGraphicLeftShadow;
        public Graphic FootGraphicRight;
        public Graphic FootGraphicRightCol;
        public Graphic FootGraphicRightShadow;
        public Graphic FrontPawGraphicLeft;
        public Graphic FrontPawGraphicLeftCol;
        public Graphic FrontPawGraphicLeftShadow;
        public Graphic FrontPawGraphicRight;
        public Graphic FrontPawGraphicRightCol;
        public Graphic FrontPawGraphicRightShadow;
        public Graphic HandGraphicLeft;
        public Graphic HandGraphicLeftCol;
        public Graphic HandGraphicLeftShadow;
        public Graphic HandGraphicRight;
        public Graphic HandGraphicRightCol;
        public Graphic HandGraphicRightShadow;

        #endregion Public Fields

        #region Private Fields


        private readonly Color _shadowColor = new(0.54f, 0.56f, 0.6f);

        #endregion Private Fields

        #region Public Constructors

        public PawnBodyGraphic(CompBodyAnimator compAni)
        {
            this.CompAni = compAni;
            this._pawn = compAni.pawn;
            this._pawn.CheckForAddedOrMissingPartsAndSetColors();
            this.Initialize();
        }

        #endregion Public Constructors

        #region Public Methods

        public void Initialize()
        {
            LongEventHandler.ExecuteWhenFinished(
                                                 () =>
                                                 {
                                                     this.InitializeGraphicsFeet();

                                                     this.InitializeGraphicsHand();

                                                     this.InitializeGraphicsFrontPaws();
                                                 });
        }

        #endregion Public Methods

        #region Private Methods

        private void InitializeGraphicsFeet()
        {
            string texNameFoot = CompAni.TexNameFoot();
            
            // no story, either animal or not humanoid biped
            
            Color rightColorFoot = Color.red;
            Color leftColorFoot = Color.blue;

            Color skinColor;
            bool animalOverride = this._pawn.story == null;
            if (animalOverride)
            {
                PawnKindLifeStage curKindLifeStage = this._pawn.ageTracker.CurKindLifeStage;

                skinColor = curKindLifeStage.bodyGraphicData.color;
            }
            else
            {
                skinColor = _pawn.story.SkinColor;

            }

            Color rightFootShadowColor = (animalOverride ? skinColor : this.CompAni.FootColorRight) * this._shadowColor;
            Color leftFootShadowColor = (animalOverride ? skinColor : this.CompAni.FootColorLeft) * this._shadowColor;


            Vector2 drawSize = new(1f,1f);
            var stats = this.CompAni.BodyStat;
            this.FootGraphicRight = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                GetShader(stats.FootRight),
                drawSize,
                animalOverride? skinColor : this.CompAni.FootColorRight,
                animalOverride ? skinColor : this.CompAni.FootColorRight);

            this.FootGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                GetShader(stats.FootLeft),
                drawSize,
                animalOverride ? skinColor : this.CompAni.FootColorLeft,
                animalOverride ? skinColor : this.CompAni.FootColorLeft);

            this.FootGraphicRightShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                GetShader(stats.FootRight),
                drawSize,
                rightFootShadowColor,
                rightFootShadowColor);

            this.FootGraphicLeftShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                GetShader(stats.FootLeft),
                drawSize,
                leftFootShadowColor,
                leftFootShadowColor);

            this.FootGraphicRightCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                GetShader(stats.FootRight),
                drawSize,
                animalOverride ? skinColor : rightColorFoot,
                animalOverride ? skinColor : rightColorFoot);

            this.FootGraphicLeftCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                GetShader(stats.FootLeft),
                drawSize,
                animalOverride ? skinColor : leftColorFoot,
                animalOverride ? skinColor : leftColorFoot);
        }

        private void InitializeGraphicsFrontPaws()
        {
            if (!this.CompAni.Props.quadruped)
            {
                return;

            }
            string texNameFoot = PawnExtensions.PathAnimals + "Paws/" + this.CompAni.Props.handType + PawnExtensions.STR_Foot;

            Color skinColor;
            if (this._pawn.story != null)
            {
                skinColor = this._pawn.story.SkinColor;
            }
            else
            {
                PawnKindLifeStage curKindLifeStage = this._pawn.ageTracker.CurKindLifeStage;

                skinColor = curKindLifeStage.bodyGraphicData.color;
            }

            Color rightColorFoot = Color.cyan;
            Color leftColorFoot = Color.magenta;

            Color rightFootColor = skinColor;
            Color leftFootColor = skinColor;
            Color metal = new(0.51f, 0.61f, 0.66f);

            switch (this.CompAni.BodyStat.FootRight)
            {
                case PartStatus.Artificial:
                    rightFootColor = metal;
                    break;
            }

            switch (this.CompAni.BodyStat.FootLeft)
            {
                case PartStatus.Artificial:
                    leftFootColor = metal;
                    break;
            }

            Color rightFootColorShadow = rightFootColor * this._shadowColor;
            Color leftFootColorShadow = leftFootColor * this._shadowColor;


            Vector2 drawSize = new(1f, 1f);
            this.FrontPawGraphicRight = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightFootColor,
                skinColor);

            this.FrontPawGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftFootColor,
                skinColor);

            this.FrontPawGraphicRightShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightFootColorShadow,
                skinColor);

            this.FrontPawGraphicLeftShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftFootColorShadow,
                skinColor);

            this.FrontPawGraphicRightCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightColorFoot,
                skinColor);

            this.FrontPawGraphicLeftCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftColorFoot,
                skinColor);
        }

        private void InitializeGraphicsHand()
        {
            if (!this.CompAni.Props.bipedWithHands)
            {
                return;
            }

            string texNameHand = this.CompAni.TexNameHand();

            Color rightColorHand = Color.cyan;
            Color leftColorHand = Color.magenta;





            Color metal = new(0.51f, 0.61f, 0.66f);

            var leftHandColor = this.CompAni.HandColorLeft;
            var rightHandColor = this.CompAni.HandColorRight;

            Color leftHandColorShadow = leftHandColor * this._shadowColor;
            Color rightHandColorShadow = rightHandColor * this._shadowColor;

            var stats = this.CompAni.BodyStat;
            Vector2 drawSize = new(1f, 1f);
            this.HandGraphicRight = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandRight),
                drawSize,
                rightHandColor,
                rightHandColor);

            this.HandGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandLeft),
                drawSize,
                leftHandColor,
                leftHandColor);

            this.HandGraphicRightShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandRight),
                drawSize,
                rightHandColorShadow,
                rightHandColorShadow);

            this.HandGraphicLeftShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandLeft),
                drawSize,
                leftHandColorShadow,
                leftHandColorShadow);

            // for development
            this.HandGraphicRightCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandRight),
                drawSize,
                rightColorHand,
                rightColorHand);

            this.HandGraphicLeftCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                GetShader(stats.HandLeft),
                drawSize,
                leftColorHand,
                leftColorHand);
        }

        private Shader GetShader(PartStatus partStatus)
        {
            switch (partStatus)
            {
                case PartStatus.Natural:
                    return ShaderDatabase.CutoutSkin;
                case PartStatus.Missing:
                case PartStatus.Artificial:
                case PartStatus.DisplayOverBeard:
                case PartStatus.Apparel:
                default:
                    break;
            }
            return ShaderDatabase.Cutout;

        }

        #endregion Private Methods
    }
}