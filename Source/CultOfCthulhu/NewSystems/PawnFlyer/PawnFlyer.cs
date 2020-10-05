using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace CultOfCthulhu
{
    public class PawnFlyer : Pawn
    {
        private CompTransporterPawn compTransporterPawn;

        private CompLaunchablePawn compLaunchablePawn;

        public override void SpawnSetup(Map map, bool bla)
        {
            compTransporterPawn = this.TryGetComp<CompTransporterPawn>();
            compLaunchablePawn = this.TryGetComp<CompLaunchablePawn>();
            DecrementMapIndex();
            base.SpawnSetup(map, bla);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerator<Gizmo> enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }

            if (Faction == Faction.OfPlayer && !Dead && !Dead)
            {
                if (compTransporterPawn.LoadingInProgressOrReadyToLaunch)
                {
                    Command_Action command_Action = new Command_Action
                    {
                        defaultLabel = "CommandLaunchGroup".Translate(),
                        defaultDesc = "CommandLaunchGroupDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Icons/Commands/FlyingTarget", true),
                        action = delegate
                        {
                            if (compTransporterPawn.AnyInGroupHasAnythingLeftToLoad)
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmSendNotCompletelyLoadedPods".Translate(
                                compTransporterPawn.FirstThingLeftToLoadInGroup.LabelCap
                                ), new Action(compLaunchablePawn.StartChoosingDestination), false, null));
                            }
                            else
                            {
                                compLaunchablePawn.StartChoosingDestination();
                            }
                        }
                    };
                    if (compLaunchablePawn.AnyInGroupIsUnderRoof)
                    {
                        command_Action.Disable("CommandLaunchGroupFailUnderRoof".Translate());
                    }
                    yield return command_Action;
                }

                if (compTransporterPawn.LoadingInProgressOrReadyToLaunch)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "CommandCancelLoad".Translate(),
                        defaultDesc = "CommandCancelLoadDesc".Translate(),
                        icon = CompTransporterPawn.CancelLoadCommandTex,
                        action = delegate
                        {
                            SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
                            compTransporterPawn.CancelLoad();
                        }
                    };
                }
                Command_LoadToTransporterPawn command_LoadToTransporter = new Command_LoadToTransporterPawn();
                int num = 0;
                for (int i = 0; i < Find.Selector.NumSelected; i++)
                {
                    if (Find.Selector.SelectedObjectsListForReading[i] is Thing thing && thing.def == def)
                    {
                        CompLaunchablePawn compLaunchable = thing.TryGetComp<CompLaunchablePawn>();
                        if (compLaunchable != null)
                        {
                            num++;
                        }
                    }
                }
                command_LoadToTransporter.defaultLabel = "CommandLoadTransporter".Translate(
                num.ToString()
                );
                command_LoadToTransporter.defaultDesc = "CommandLoadTransporterDesc".Translate();
                command_LoadToTransporter.icon = CompTransporterPawn.LoadCommandTex;
                command_LoadToTransporter.transComp = compTransporterPawn;
                CompLaunchablePawn launchable = compTransporterPawn.Launchable;
                yield return command_LoadToTransporter;
            }
            yield break;
        }
    }
}
