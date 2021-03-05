// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------

using System.Collections.Generic;
using Cthulhu;
using RimWorld;
using Verse;
using Verse.AI;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
// Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
// RimWorld universal objects are here (like 'Building')
// Needed when you do something with the AI
// Needed when you do something with Sound
// Needed when you do something with Noises
// RimWorld specific functions are found here (like 'Building_Battery')

// RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace CultOfCthulhu
{
    public class JobDriver_WriteTheBook : JobDriver
    {
        private readonly TargetIndex Executioner = TargetIndex.A;
        private bool atTypeWriter;

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(Executioner);
            if (CultUtility.AreOccultGrimoiresAvailable(pawn.Map))
            {
                pawn.Map.GetComponent<MapComponent_LocalCultTracker>().CurrentSeedState = CultSeedState.NeedTable;
            }
            else
            {
                var destination = IntVec3.Invalid;

                //First, let's try and find a typewriter.
                //If we find one, let's go to it and start typing.
                Thing Typewriter = null;
                if (Utility.IsIndustrialAgeLoaded())
                {
                    Utility.DebugReport("Industrial age check");
                    if (pawn.Map != null)
                    {
                        if (pawn.Map.listerBuildings != null)
                        {
                            foreach (var thing in pawn.Map.listerBuildings.allBuildingsColonist)
                            {
                                if (thing.def.defName == "Estate_TableTypewriter")
                                {
                                    Typewriter = thing;
                                    Utility.DebugReport("Found typewriter");
                                    var gotoDestination =
                                        Toils_Goto.GotoCell(Typewriter.InteractionCell, PathEndMode.OnCell);
                                    atTypeWriter = true;
                                    yield return gotoDestination;
                                    goto SkipRoom;
                                }
                            }
                        }
                    }
                }

                //If we don't have a typewriter, then let's go to our personal room or near our bed.
                var destinationRoom = pawn.ownership.OwnedRoom;
                Building destinationBed = pawn.ownership.OwnedBed;
                if (destinationRoom != null)
                {
                    if (destinationRoom.Cells.TryRandomElement(out destination))
                    {
                        var cellInsideRoom = IntVec3.Invalid;
                        if (Utility.IsRandomWalkable8WayAdjacentOf(destination, Map, out cellInsideRoom))
                        {
                            Toil gotoRoom;
                            gotoRoom = Toils_Goto.GotoCell(cellInsideRoom, PathEndMode.OnCell);
                            yield return gotoRoom;
                        }
                    }
                }
                else if (destinationBed != null)
                {
                    var cellNearBed = IntVec3.Invalid;
                    if (Utility.IsRandomWalkable8WayAdjacentOf(destinationBed.Position, Map, out cellNearBed))
                    {
                        Toil gotoBedArea;
                        gotoBedArea = Toils_Goto.GotoCell(cellNearBed, PathEndMode.OnCell);
                    }
                }

                SkipRoom:

                var altarToil = new Toil();
                altarToil.defaultCompleteMode = ToilCompleteMode.Delay;
                if (atTypeWriter)
                {
                    altarToil.PlaySustainerOrSound(SoundDef.Named("Estate_SoundManualTypewriter"));
                }
                else
                {
                    altarToil.PlaySustainerOrSound(SoundDef.Named("PencilWriting"));
                }

                altarToil.WithProgressBarToilDelay(TargetIndex.A);
                altarToil.defaultDuration = job.def.joyDuration;
                altarToil.AddPreTickAction(() =>
                {
                    if (Typewriter != null)
                    {
                        pawn.rotationTracker.FaceCell(Typewriter.Position);
                        pawn.GainComfortFromCellIfPossible();
                    }
                });
                altarToil.AddPreInitAction(() => Messages.Message(pawn.LabelCap + "WritingStrangeSymbols".Translate(),
                    MessageTypeDefOf.NeutralEvent));
                yield return altarToil;

                var finishedAction = new Toil
                {
                    defaultCompleteMode = ToilCompleteMode.Instant,
                    initAction = delegate
                    {
                        Map.GetComponent<MapComponent_LocalCultTracker>().CurrentSeedState =
                            CultSeedState.FinishedWriting;
                    }
                };
                yield return finishedAction;

                AddFinishAction(() =>
                {
                    if (Map.GetComponent<MapComponent_LocalCultTracker>().CurrentSeedState ==
                        CultSeedState.FinishedWriting)
                    {
                        CultUtility.FinishedTheBook(pawn);
                    }
                });
            }
        }
    }
}