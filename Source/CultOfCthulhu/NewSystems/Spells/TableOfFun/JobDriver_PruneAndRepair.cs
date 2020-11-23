using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace CultOfCthulhu
{
    public class JobDriver_PruneAndRepair : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public static int remainingDuration = 20000; // 6 in-game hours

        private const float WarmupTicks = 80f;

        private const float TicksBetweenRepairs = 16f;

        protected float ticksToNextRepair;


        protected Building_SacrificialAltar Altar
        {
            get
            {
                return (Building_SacrificialAltar)job.GetTarget(TargetIndex.A).Thing;
            }
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);

            //Toil 1: Go to the pruning site.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            //Toil 2: Begin pruning.
            Toil toil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = remainingDuration
            };
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.initAction = delegate
            {
                ticksToNextRepair = 80f;
            };
            toil.tickAction = delegate
            {
                Pawn actor = pawn;
                actor.skills.Learn(SkillDefOf.Construction, 0.5f, false);
                actor.skills.Learn(SkillDefOf.Plants, 0.5f, false);
                float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                ticksToNextRepair -= statValue;
                if (ticksToNextRepair <= 0f)
                {
                    ticksToNextRepair += 16f;
                    TargetThingA.HitPoints++;
                    TargetThingA.HitPoints = Mathf.Min(TargetThingA.HitPoints, TargetThingA.MaxHitPoints);
                    //if (this.TargetThingA.HitPoints == this.TargetThingA.MaxHitPoints)
                    //{
                    //    actor.records.Increment(RecordDefOf.ThingsRepaired);
                    //    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                    //}
                }
            };
            toil.WithEffect(TargetThingA.def.repairEffect, TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            yield return toil;


            //Toil 3 Unreserve
            yield return Toils_Reserve.Release(TargetIndex.A);

            //Toil 4: Transform the altar once again.
            yield return new Toil
            {
                initAction = delegate
                {
                    PruneResult(); 
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };


            yield break;
        }

        public void PruneResult()
        {
            Altar.NightmarePruned(pawn);
        }
    }
}
