using System;
using System.Globalization;
using UnityEngine;

namespace ShowMeYourHands;

internal class SaveableFloat
{
    public SaveableFloat(float x)
    {
        this.x = x;
    }

    private float x { get; }

    public override string ToString()
    {
        return string.Format("({0:F3})", new object[]
        {
            x,
        });
    }

    public static SaveableFloat FromString(string Str)
    {
        Str = Str.TrimStart('(');
        Str = Str.TrimEnd(')');
        string[] array = Str.Split(',');
        CultureInfo invariantCulture = CultureInfo.InvariantCulture;
        float x = Convert.ToSingle(array[0], invariantCulture);
        return new SaveableFloat(x);
    }

}