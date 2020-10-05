using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace CultOfCthulhu
{
    public class HediffComp_SanityLoss : HediffComp
    {

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn != null)
            {
                if (Pawn.RaceProps != null)
                {
                    if (Pawn.RaceProps.IsMechanoid)
                    {
                        MakeSane();
                    }
                }
            }

            if (Cthulhu.Utility.IsCosmicHorrorsLoaded())
            {
                if (Pawn.GetType().ToString() == "CosmicHorrorPawn")
                {
                    MakeSane();
                }
            }
        }

        
        public void MakeSane()
        {
            parent.Severity -= 1f;
            Pawn.health.Notify_HediffChanged(parent);
        }
    }

}
