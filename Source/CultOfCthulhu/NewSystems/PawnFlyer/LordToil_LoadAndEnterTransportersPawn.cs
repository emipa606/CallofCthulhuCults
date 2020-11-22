using System;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace CultOfCthulhu
{
    public class LordToil_LoadAndEnterTransportersPawn : LordToil
    {
        private readonly int transportersGroup = -1;

        public override bool AllowSatisfyLongNeeds => false;

        public LordToil_LoadAndEnterTransportersPawn(int transportersGroup)
        {
            Cthulhu.Utility.DebugReport("LordToil_LoadAndCenterTranpsortersPawn Called");
            this.transportersGroup = transportersGroup;
        }

        public override void UpdateAllDuties()
        {
            for (var i = 0; i < lord.ownedPawns.Count; i++)
            {
                var pawnDuty = new PawnDuty(CultsDefOf.Cults_LoadAndEnterTransportersPawn)
                {
                    transportersGroup = transportersGroup
                };
                lord.ownedPawns[i].mindState.duty = pawnDuty;
            }
        }
    }
}
