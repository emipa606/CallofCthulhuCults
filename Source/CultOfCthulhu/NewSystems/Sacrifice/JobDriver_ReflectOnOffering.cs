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
    class JobDriver_ReflectOnOffering : JobDriver
    {

        protected Building_SacrificialAltar altar
        {
            get
            {
                return (Building_SacrificialAltar)job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Toil 0 -- Go here first to reflect.
            if (altar != null)
            {
                if (pawn == altar.preacher)
                {
                    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
                }
            }

            //Toil 1 Celebrate or recoil
            yield return new Toil
            {
                initAction = delegate
                {
                    //Do something? Ia ia!
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            //Toil 2 Reflect on worship
            Toil reflectingTime = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = CultUtility.reflectDuration
            };
            //chantingTime.PlaySustainerOrSound(DefDatabase<SoundDef>.GetNamed("Estate_GramophoneWindup"));
            yield return reflectingTime;

            //Toil 3 Reset the altar and clear variables.
            yield return new Toil
            {
                initAction = delegate
                {
                    if (altar != null)
                    {
                        if (altar.currentOfferingState == Building_SacrificialAltar.OfferingState.offering)
                        {
                            altar.ChangeState(Building_SacrificialAltar.State.offering, Building_SacrificialAltar.OfferingState.finished);
                            //Map.GetComponent<MapComponent_SacrificeTracker>().ClearVariables();
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

    }
}
