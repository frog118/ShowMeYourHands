using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShowMeYourHands.FSWalking.Animator
{
    [StaticConstructorOnStartup]
    public static class AnimatorTextures
    {
        public static readonly Texture2D BackgroundAnimTex = ContentFinder<Texture2D>.Get("UI/walkbg-01");
        public static readonly Texture2D BackgroundTex = ContentFinder<Texture2D>.Get("UI/gradient");

    }
}
