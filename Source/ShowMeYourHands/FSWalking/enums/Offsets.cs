﻿// ReSharper disable InconsistentNaming

using UnityEngine;
using Verse;

namespace FacialStuff
{
    public static class Offsets
    {
        //// total max with repetitions: LayerSpacing = 0.46875f;


        public const float YOffset_Head = 0.02734375f;


        public const float YOffset_HandsFeetOver = 0.008687258f; // FS
        public const float YOffset_Behind = 0.00390625f;


        public const float YOffset_CarriedThing = 0.0390625f;

        // Verse.Listing_Standard
        public static float Slider(this Listing_Standard listing, float value, float leftValue, float rightValue, bool middleAlignment = false, string label = null, string leftAlignedLabel = null, string rightAlignedLabel = null, float roundTo = -1f)
        {
            Rect rect = listing.GetRect(22f);
            float result = Widgets.HorizontalSlider(rect, value, leftValue, rightValue, middleAlignment, label, leftAlignedLabel, rightAlignedLabel, roundTo);
            listing.Gap(listing.verticalSpacing);
            return result;
        }
    }
}