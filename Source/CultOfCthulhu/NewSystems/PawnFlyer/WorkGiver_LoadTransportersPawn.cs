using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace CultOfCthulhu
{
    public class WorkGiver_LoadTransportersPawn : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest. ForGroup(ThingRequestGroup.Pawn);
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t == null) return false;

            if (!(t is Pawn pawn2) || pawn2 == pawn)
            {
                return false;
            }
            if (!pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly, 1))
            {
                return false;
            }

            CompTransporterPawn transporter = t.TryGetComp<CompTransporterPawn>();
            return transporter != null && LoadTransportersPawnJobUtility.HasJobOnTransporter(pawn, transporter);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompTransporterPawn transporter = t.TryGetComp<CompTransporterPawn>();
            return t == null ? null : LoadTransportersPawnJobUtility.JobOnTransporter(pawn, transporter);
        }


    }


}
