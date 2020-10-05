﻿// ----------------------------------------------------------------------
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
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
using Verse.AI.Group;
using Verse.Sound;       // Needed when you do something with Sound
using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace CultOfCthulhu
{
    public class JobDriver_MidnightInquisition : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        private readonly TargetIndex InquisitorIndex = TargetIndex.A;
        private readonly TargetIndex PreacherIndex = TargetIndex.B;

        protected Pawn Preacher
        {
            get
            {
                return job.GetTarget(TargetIndex.B).Thing as Pawn;
            }
        }

        protected Pawn Inquisitor
        {
            get
            {
                return (Pawn)job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }


        private bool firstHit = true;
        private bool notifiedPlayer = false;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //
            Toil toil = new Toil
            {
                initAction = delegate
                {
                //Empty
            }
            };


            this.EndOnDespawnedOrNull(InquisitorIndex, JobCondition.Incompletable);
            this.EndOnDespawnedOrNull(PreacherIndex, JobCondition.Incompletable);
            //this.EndOnDespawnedOrNull(Build, JobCondition.Incompletable);
            yield return Toils_Reserve.Reserve(PreacherIndex, job.def.joyMaxParticipants);
            Toil gotoPreacher;
            gotoPreacher = Toils_Goto.GotoThing(PreacherIndex, PathEndMode.ClosestTouch);
            yield return gotoPreacher;

            if (Preacher.jobs.curDriver.asleep)
            {
                Toil watchToil = new Toil
                {
                    defaultCompleteMode = ToilCompleteMode.Delay,
                    defaultDuration = job.def.joyDuration
                };
                watchToil.AddPreTickAction(() =>
                {
                    pawn.rotationTracker.FaceCell(Preacher.Position);
                    pawn.GainComfortFromCellIfPossible();
                });
                yield return watchToil;
            }

            void hitAction()
            {
                Pawn prey = Preacher;
                bool surpriseAttack = firstHit;
                if (pawn.meleeVerbs.TryMeleeAttack(prey, job.verbToUse, surpriseAttack))
                {
                    if (!notifiedPlayer && PawnUtility.ShouldSendNotificationAbout(prey))
                    {
                        notifiedPlayer = true;
                        if (Prefs.AutomaticPauseMode > AutomaticPauseMode.Never && !Find.TickManager.Paused)
                        {
                            Find.TickManager.TogglePaused();
                        }
                        Messages.Message("MessageAttackedByPredator".Translate(
                            prey.LabelShort,
                            pawn.LabelShort
                        ).CapitalizeFirst(), prey, MessageTypeDefOf.ThreatBig);
                    }
                    pawn.Map.attackTargetsCache.UpdateTarget(pawn);
                }
                firstHit = false;
            }
            yield return Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, hitAction).JumpIfDespawnedOrNull(TargetIndex.A, toil).FailOn(() => Find.TickManager.TicksGame > startTick + 5000 && (job.GetTarget(TargetIndex.A).Cell - pawn.Position).LengthHorizontalSquared > 4f);
            yield return toil;

            AddFinishAction(() =>
            {
                //if (this.TargetB.HasThing)
                //{
                //    Find.Reservations.Release(this.job.targetC.Thing, pawn);
                //}
            });
        }
    }
}