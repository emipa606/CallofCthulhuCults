using Cthulhu;
using Verse.AI;
using Verse.AI.Group;

namespace CultOfCthulhu
{
    public class LordToil_LoadAndEnterTransportersPawn : LordToil
    {
        private readonly int transportersGroup = -1;

        public LordToil_LoadAndEnterTransportersPawn(int transportersGroup)
        {
            Utility.DebugReport("LordToil_LoadAndCenterTranpsortersPawn Called");
            this.transportersGroup = transportersGroup;
        }

        public override bool AllowSatisfyLongNeeds => false;

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