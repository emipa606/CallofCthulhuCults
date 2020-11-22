// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine; // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse; // RimWorld universal objects are here (like 'Building')
using Verse.AI; // Needed when you do something with the AI
using Verse.AI.Group;
using Verse.Sound; // Needed when you do something with Sound
using Verse.Noise; // Needed when you do something with Noises
using RimWorld; // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet; // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace CultOfCthulhu
{
    public class JobDriver_HoldSacrifice : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        private const TargetIndex TakeeIndex = TargetIndex.A;
        private const TargetIndex AltarIndex = TargetIndex.B;

        protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        protected Building_SacrificialAltar DropAltar => (Building_SacrificialAltar)job.GetTarget(TargetIndex.B).Thing;

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Commence fail checks!

            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);

            yield return Toils_Reserve.Reserve(TakeeIndex, 1);
            yield return Toils_Reserve.Reserve(AltarIndex, Building_SacrificialAltar.LyingSlotsCount);

            yield return new Toil
            {
                initAction = delegate
                {
                    DropAltar.ChangeState(Building_SacrificialAltar.State.sacrificing,
                        Building_SacrificialAltar.SacrificeState.gathering);
                }
            };

            //Toil 1: Go to prisoner.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => job.def == JobDefOf.Arrest && !Takee.CanBeArrestedBy(pawn))
                .FailOn(() =>
                    !pawn.CanReach(DropAltar, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    if (job.def.makeTargetPrisoner)
                    {
                        var pawn = (Pawn) job.targetA.Thing;
                        Lord lord = pawn.GetLord();
                        if (lord != null)
                        {
                            lord.Notify_PawnAttemptArrested(pawn);
                        }
                        GenClamor.DoClamor(pawn, 10f, ClamorDefOf.Harm);
                        if (job.def == JobDefOf.Arrest && !pawn.CheckAcceptArrest(this.pawn))
                        {
                            this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        }
                    }
                }
            };
            //Toil 2: Carry prisoner.
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            //Toil 3: Go to the altar.
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            //Toil 4: Release the prisoner.
            yield return Toils_Reserve.Release(TargetIndex.B);
            //Toil 5: Restrain the prisoner.
            yield return new Toil
            {
                initAction = delegate
                {
                    //In-case this fails...
                    IntVec3 position = DropAltar.Position;
                    pawn.carryTracker.TryDropCarriedThing(position, ThingPlaceMode.Direct, out Thing thing, null);
                    if (!DropAltar.Destroyed && DropAltar.AnyUnoccupiedLyingSlot)
                    {
                        Takee.Position = DropAltar.GetLyingSlotPos();
                        Takee.Notify_Teleported(false);
                        Takee.stances.CancelBusyStanceHard();
                        var job = new Job(CultsDefOf.Cults_WaitTiedDown, DropAltar);
                        Takee.jobs.StartJob(job);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            //Toil 6: Time to chant ominously
            var chantingTime = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = CultUtility.ritualDuration
            };
            chantingTime.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            chantingTime.PlaySustainerOrSound(CultsDefOf.RitualChanting);
            Texture2D deitySymbol = ((CosmicEntityDef) DropAltar.SacrificeData.Entity.def).Symbol;
            chantingTime.initAction = delegate
            {
                if (deitySymbol != null)
                {
                    MoteMaker.MakeInteractionBubble(pawn, null, ThingDefOf.Mote_Speech, deitySymbol);
                }


                //STATE - SACRIFICING
                DropAltar.ChangeState(Building_SacrificialAltar.State.sacrificing,
                    Building_SacrificialAltar.SacrificeState.sacrificing);
            };

            yield return chantingTime;

            //Toil 8: Execution of Prisoner
            yield return new Toil
            {
                initAction = delegate
                {
                    //BodyPartDamageInfo value = new BodyPartDamageInfo(this.Takee.health.hediffSet.GetBrain(), false, quiet);
                    Takee.TakeDamage(new DamageInfo(DamageDefOf.ExecutionCut, 99999, 0f, -1f, pawn,
                        Cthulhu.Utility.GetHeart(Takee.health.hediffSet)));
                    if (!Takee.Dead)
                    {
                        Takee.Kill(null);
                    }
                    //ThoughtUtility.GiveThoughtsForPawnExecuted(this.Takee, PawnExecutionKind.GenericHumane);
                    TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, new object[]
                    {
                        pawn,
                        Takee
                    });
                    CultUtility.SacrificeExecutionComplete(DropAltar);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            AddFinishAction(() =>
            {
                //It's a day to remember
                var taleToAdd = TaleDef.Named("HeldSermon");
                if ((pawn.IsColonist || pawn.HostFaction == Faction.OfPlayer) && taleToAdd != null)
                {
                    TaleRecorder.RecordTale(taleToAdd, new object[]
                    {
                        pawn,
                    });
                }

                //When the ritual is finished -- then let's give the thoughts
                /*
                if (DropAltar.currentSacrificeState == Building_SacrificialAltar.SacrificeState.finished)
                {
                    if (this.pawn == null) return;
                    if (DropAltar.sacrifice != null)
                    {
                        CultUtility.AttendSacrificeTickCheckEnd(this.pawn, DropAltar.sacrifice, true);
                    }
                    else
                    {
                        CultUtility.AttendSacrificeTickCheckEnd(this.pawn, null);
                    }
                }
                */
            });
        }
    }
}