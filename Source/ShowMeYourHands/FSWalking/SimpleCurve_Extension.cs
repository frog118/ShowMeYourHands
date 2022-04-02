using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ShowMeYourHands.FSWalking
{ static class SimpleCurve_Extension
    {
        public static float EvaluateNoError(this SimpleCurve curve,  float x)
        {
            if (curve.PointsCount == 0)
            {
                return 0f;
            }
            return curve.Evaluate(x);
        }
    }
}
