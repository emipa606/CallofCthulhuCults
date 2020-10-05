using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI;
using Verse;
using RimWorld;
using UnityEngine;

namespace CultOfCthulhu
{
    public class JobDriver_TiedDown : JobDriver_Wait
    {

        protected Building_SacrificialAltar DropAltar
        {
            get
            {
                return (Building_SacrificialAltar)job.GetTarget(TargetIndex.A).Thing;
            }
        }


        protected override IEnumerable<Toil> MakeNewToils()
        {

            yield return new Toil
            {
                initAction = delegate
                {
                    pawn.Reserve(pawn.Position, job);// De ReserveDestinationFor(this.pawn, this.pawn.Position);
                    pawn.pather.StopDead();
                    JobDriver curDriver = pawn.jobs.curDriver;
                    pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
                    curDriver.asleep = false;
                },
                tickAction = delegate
                {
                    if (job.expiryInterval == -1 && job.def == JobDefOf.Wait_Combat && !pawn.Drafted)
                    {
                        Log.Error(pawn + " in eternal WaitCombat without being drafted.");
                        ReadyForNextToil();
                        return;
                    }
                    if ((Find.TickManager.TicksGame + pawn.thingIDNumber) % 4 == 0)
                    {
                        //base.CheckForAutoAttack();
                    }
                    
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
        }
    }
}