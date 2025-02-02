﻿using UnityEngine;
using Verse;

namespace FacialStuff.GraphicsFS
{
    [StaticConstructorOnStartup]
    public static class FaceTextures
    {
        public static readonly Texture2D BlankTexture;

        public static readonly Texture2D MaskTexFullheadFrontBack;
        public static readonly Texture2D MaskTexFullheadFrontBack256;
        public static readonly Texture2D MaskTexFullheadFrontBack512;
        public static readonly Texture2D MaskTexFullheadSide;
        public static readonly Texture2D MaskTexFullheadSide256;
        public static readonly Texture2D MaskTexFullheadSide512;
        public static readonly Texture2D MaskTexUpperheadSide;
        public static readonly Texture2D MaskTexUpperheadSide256;
        public static readonly Texture2D MaskTexUpperheadSide512;
        public static readonly Texture2D MaskTexUppherheadFrontBack;
        public static readonly Texture2D MaskTexUppherheadFrontBack256;
        public static readonly Texture2D MaskTexUppherheadFrontBack512;
        public static readonly Texture2D RedTexture;
        public static readonly Color SkinRottingMultiplyColor = new(0.35f, 0.38f, 0.3f);

        /*
                private static Texture2D _maskTexAverageSide;
        */

        private static readonly Texture2D _maskTexNarrowFrontBack;
        static FaceTextures()
        {
            MaskTexUppherheadFrontBack = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_south"));

            MaskTexUpperheadSide = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_east"));

            MaskTexFullheadFrontBack = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_south"));

            MaskTexFullheadSide = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_east"));

            MaskTexUppherheadFrontBack256 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_256_south"));

            MaskTexUpperheadSide256 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_256_east"));

            MaskTexFullheadFrontBack256 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_256_south"));

            MaskTexFullheadSide256 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_256_east"));
         
            MaskTexUppherheadFrontBack512 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_512_south"));

            MaskTexUpperheadSide512 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Upperhead_512_east"));

            MaskTexFullheadFrontBack512 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_512_south"));

            MaskTexFullheadSide512 = MakeReadable(ContentFinder<Texture2D>.Get("MaskTex/MaskTex_Fullhead_512_east"));

            BlankTexture = new Texture2D(128, 128, TextureFormat.ARGB32, false);

            // The RedTexture is used as a mask texture, in case hair/eyes have no mask on their own
            RedTexture = new Texture2D(128, 128, TextureFormat.ARGB32, false);

            for (int x = 0; x < BlankTexture.width; x++)
            {
                for (int y = 0; y < BlankTexture.height; y++)
                {
                    BlankTexture.SetPixel(x, y, Color.clear);
                }
            }
            for (int x = 0; x < RedTexture.width; x++)
            {
                for (int y = 0; y < RedTexture.height; y++)
                {
                    RedTexture.SetPixel(x, y, Color.red);
                }
            }

            BlankTexture.name = "Blank";
            RedTexture.name = "Red";

            BlankTexture.Compress(false);
            BlankTexture.Apply(false, true);

            RedTexture.Compress(false);
            RedTexture.Apply(false, true);
        }

        public static Texture2D MakeReadable(Texture2D texture)
        {
            RenderTexture previous = RenderTexture.active;

            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);

            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;

            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new(texture.width, texture.width, TextureFormat.ARGB32, false);

            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();

            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            return myTexture2D;

            // "myTexture2D" now has the same pixels from "texture" and it's readable.
        }
    }
}