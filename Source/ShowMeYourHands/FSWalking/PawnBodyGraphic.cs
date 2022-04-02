﻿// ReSharper disable StyleCop.SA1401

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
            Color skinColor = CompAni.FootColor;

            /*
            // no story, either animal or not humanoid biped
            if (this._pawn.story == null)
            {
                PawnKindLifeStage curKindLifeStage = this._pawn.ageTracker.CurKindLifeStage;

                skinColor = curKindLifeStage.bodyGraphicData.color;
            }
            else
            {
                skinColor = this._pawn.story.SkinColor;
            }
            */
            Color rightColorFoot = Color.red;
            Color leftColorFoot = Color.blue;

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

            Color rightFootShadowColor = rightFootColor * this._shadowColor;
            Color leftFootShadowColor = leftFootColor * this._shadowColor;


            Vector2 drawSize = new(1f,1f);

            this.FootGraphicRight = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightFootColor,
                skinColor);

            this.FootGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftFootColor,
                skinColor);

            this.FootGraphicRightShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightFootShadowColor,
                skinColor);

            this.FootGraphicLeftShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftFootShadowColor,
                skinColor);

            this.FootGraphicRightCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightColorFoot,
                skinColor);

            this.FootGraphicLeftCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameFoot,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftColorFoot,
                skinColor);
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
            
            Color skinColor = CompAni.HandColor;

            Color rightHandColor = skinColor;
            Color leftHandColor = skinColor;

            Color metal = new(0.51f, 0.61f, 0.66f);

            switch (this.CompAni.BodyStat.HandRight)
            {
                case PartStatus.Artificial:
                    rightHandColor = metal;
                    break;
            }

            switch (this.CompAni.BodyStat.HandLeft)
            {
                case PartStatus.Artificial:
                    leftHandColor = metal;
                    break;
            }

            Color leftHandColorShadow = leftHandColor * this._shadowColor;
            Color rightHandColorShadow = rightHandColor * this._shadowColor;

            Vector2 drawSize = new(1f, 1f);
            this.HandGraphicRight = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightHandColor,
                skinColor);

            this.HandGraphicLeft = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftHandColor,
                skinColor);

            this.HandGraphicRightShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightHandColorShadow,
                skinColor);

            this.HandGraphicLeftShadow = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftHandColorShadow,
                skinColor);

            // for development
            this.HandGraphicRightCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                ShaderDatabase.CutoutSkin,
                drawSize,
                rightColorHand,
                skinColor);

            this.HandGraphicLeftCol = GraphicDatabase.Get<Graphic_Multi>(
                texNameHand,
                ShaderDatabase.CutoutSkin,
                drawSize,
                leftColorHand,
                skinColor);
        }


        #endregion Private Methods
    }
}