﻿using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;

namespace CultOfCthulhu
{
    // RimWorld.JobGiver_EnterTransporter
    public class JobGiver_EnterTransportersPawn : ThinkNode_JobGiver
    {
        private static readonly List<CompTransporterPawn> tmpTransporters = new List<CompTransporterPawn>();

        // I decided to just take one method instead of the entire utility.
        // RimWorld.TransporterUtility
        public static void GetTransportersInGroup(int transportersGroup, Map map, List<CompTransporterPawn> outTransporters)
        {
            outTransporters.Clear();
            if (transportersGroup < 0)
            {
                return;
            }
            IEnumerable<Pawn> listSel = from Pawn pawns in map.mapPawns.AllPawnsSpawned
                                                     where pawns is PawnFlyer
                                                     select pawns;
            var list = new List<Pawn>(listSel);
            for (var i = 0; i < list.Count; i++)
            {
                CompTransporterPawn compTransporter = list[i].TryGetComp<CompTransporterPawn>();
                if (compTransporter.groupID == transportersGroup)
                {
                    //Cthulhu.Utility.DebugReport("Added Transporter: " + list[i].Label);
                    outTransporters.Add(compTransporter);
                }
            }
        }

        //Back to RimWorld.JobGiver_EnterTransporter
        protected override Job TryGiveJob(Pawn pawn)
        {
            Cthulhu.Utility.DebugReport("JobGiver_EnterTransporterPawn Called");
            var transportersGroup = pawn.mindState.duty.transportersGroup;
            GetTransportersInGroup(transportersGroup, pawn.Map, tmpTransporters);
            CompTransporterPawn compTransporter = FindMyTransporter(tmpTransporters, pawn);
            return compTransporter == null || !pawn.CanReserveAndReach(compTransporter.parent, PathEndMode.Touch, Danger.Deadly, 1)
                ? null
                : new Job(CultsDefOf.Cults_EnterTransporterPawn, compTransporter.parent);
        }

        private CompTransporterPawn FindMyTransporter(List<CompTransporterPawn> transporters, Pawn me)
        {
            for (var i = 0; i < transporters.Count; i++)
            {
                List<TransferableOneWay> leftToLoad = transporters[i].leftToLoad;
                if (leftToLoad != null)
                {
                    for (var j = 0; j < leftToLoad.Count; j++)
                    {
                        if (leftToLoad[j].AnyThing is Pawn)
                        {
                            List<Thing> things = leftToLoad[j].things;
                            for (var k = 0; k < things.Count; k++)
                            {
                                if (things[k] == me)
                                {
                                    return transporters[i];
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
