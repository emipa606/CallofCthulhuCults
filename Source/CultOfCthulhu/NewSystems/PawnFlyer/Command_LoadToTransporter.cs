using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace CultOfCthulhu
{
    [StaticConstructorOnStartup]
    public class Command_LoadToTransporterPawn : Command
    {
        public CompTransporterPawn transComp;

        private List<CompTransporterPawn> transporters;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            if (transporters == null)
            {
                transporters = new List<CompTransporterPawn>();
            }
            if (!transporters.Contains(transComp))
            {
                transporters.Add(transComp);
            }

            _ = transComp.Launchable;
            for (int j = 0; j < transporters.Count; j++)
            {
                if (transporters[j] != transComp)
                {
                    if (!transComp.Map.reachability.CanReach(transComp.parent.Position, transporters[j].parent, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                    {
                        Messages.Message("MessageTransporterUnreachable".Translate(), transporters[j].parent, MessageTypeDefOf.RejectInput);
                        return;
                    }
                }
            }
            Find.WindowStack.Add(new Dialog_LoadTransportersPawn(transComp.Map, transporters));
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            Command_LoadToTransporterPawn command_LoadToTransporter = (Command_LoadToTransporterPawn)other;
            if (command_LoadToTransporter.transComp.parent.def != transComp.parent.def)
            {
                return false;
            }
            if (transporters == null)
            {
                transporters = new List<CompTransporterPawn>();
            }
            transporters.Add(command_LoadToTransporter.transComp);
            return false;
        }
    }
}
