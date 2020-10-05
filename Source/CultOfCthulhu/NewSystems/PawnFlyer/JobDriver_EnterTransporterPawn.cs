using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using RimWorld;

namespace CultOfCthulhu
{
    //RimWorld.JobDriver_EnterTransporter
    public class JobDriver_EnterTransporterPawn : JobDriver
    {
        private readonly TargetIndex TransporterInd = TargetIndex.A;

        private CompTransporterPawn Transporter
        {
            get
            {
                Thing thing = job.GetTarget(TransporterInd).Thing;
                return thing?.TryGetComp<CompTransporterPawn>();
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TransporterInd);
            yield return Toils_Reserve.Reserve(TransporterInd, 1);
            yield return Toils_Goto.GotoThing(TransporterInd, PathEndMode.Touch);
            yield return new Toil
            {
                initAction = delegate
                {
                    Cthulhu.Utility.DebugReport("EnterTransporterPawn Called");
                    CompTransporterPawn transporter = Transporter;
                    pawn.DeSpawn();
                    transporter.GetDirectlyHeldThings().TryAdd(pawn, true);
                    transporter.Notify_PawnEnteredTransporterOnHisOwn(pawn);
                }
            };
            yield break;
        }
    }
}
