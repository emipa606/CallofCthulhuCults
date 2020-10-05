using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CultOfCthulhu
{
    public class CultInfluence : IExposable
    {
        public Settlement settlement = null;

        public float influence = 0f;

        public bool dominant = false;

        public CultInfluence()
        {

        }

        public CultInfluence(Settlement newSettlement, float newInfluence)
        {
            settlement = newSettlement;
            influence = newInfluence;
            if (newInfluence == 1.0f) dominant = true;
        }

        public void ExposeData()
        {
            Scribe_References.Look<Settlement>(ref settlement, "settlement", false);
            Scribe_Values.Look<float>(ref influence, "influence", 0f, false);
            Scribe_Values.Look<bool>(ref dominant, "dominant", false, false);
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                "(",
                settlement,
                ", influence=",
                influence.ToString("F1"),
                (!dominant) ? string.Empty : " dominant",
                ")"
            });
        }
    }
}
