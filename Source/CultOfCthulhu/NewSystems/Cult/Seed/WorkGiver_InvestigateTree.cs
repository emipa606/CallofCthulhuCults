using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CultOfCthulhu
{
    public class WorkGiver_InvestigateTree : WorkGiver_Investigate
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(CultsDefOf.Cults_PlantTreeNightmare);
    }
}
