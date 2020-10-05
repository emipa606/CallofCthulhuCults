using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace CultOfCthulhu
{
    public class PawnFlyersTraveling : WorldObject
    {
        public PawnFlyer pawnFlyer;

        private float TravelSpeed
        {
            get { return PawnFlyerDef.flightSpeed; }
        }

        private PawnFlyerDef PawnFlyerDef
        {
            get { return pawnFlyer.def as PawnFlyerDef; }
        }


        public int destinationTile = -1;

        public IntVec3 destinationCell = IntVec3.Invalid;

        public PawnsArrivalModeDef arriveMode;

        public bool attackOnArrival;

        private List<ActiveDropPodInfo> pods = new List<ActiveDropPodInfo>();

        private bool arrived;

        private int initialTile = -1;

        private float traveledPct;

        private static readonly List<Pawn> tmpPawns = new List<Pawn>();

        private Vector3 Start
        {
            get { return Find.WorldGrid.GetTileCenter(initialTile); }
        }

        private Vector3 End
        {
            get { return Find.WorldGrid.GetTileCenter(destinationTile); }
        }

        public override Vector3 DrawPos
        {
            get { return Vector3.Slerp(Start, End, traveledPct); }
        }

        private float TraveledPctStepPerTick
        {
            get
            {
                Vector3 start = Start;
                Vector3 end = End;
                if (start == end)
                {
                    return 1f;
                }
                float num = GenMath.SphericalDistance(start.normalized, end.normalized);
                return num == 0f ? 1f : 0.00025f / num;
            }
        }

        //There is always the byakhee
        private bool PodsHaveAnyPotentialCaravanOwner
        {
            get { return true; }
        }

        public bool PodsHaveAnyFreeColonist
        {
            get
            {
                for (int i = 0; i < pods.Count; i++)
                {
                    ThingOwner innerContainer = pods[i].innerContainer;
                    for (int j = 0; j < innerContainer.Count; j++)
                    {
                        if (innerContainer[j] is Pawn pawn && pawn.IsColonist && pawn.HostFaction == null)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public IEnumerable<Pawn> Pawns
        {
            get
            {
                for (int i = 0; i < pods.Count; i++)
                {
                    ThingOwner innerContainer = pods[i].innerContainer;
                    for (int j = 0; j < innerContainer.Count; j++)
                    {
                        if (innerContainer[j] is Pawn pawn)
                        {
                            yield return pawn;
                        }
                        else if (innerContainer[j] is PawnFlyer pawnFlyer)
                        {
                            yield return (Pawn)pawnFlyer;
                        }
                    }
                }
                yield break;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            //Pawn
            Scribe_References.Look<PawnFlyer>(ref pawnFlyer, "pawnFlyer");

            //Vanilla
            Scribe_Collections.Look<ActiveDropPodInfo>(ref pods, "pods", LookMode.Deep, new object[0]);
            Scribe_Values.Look<int>(ref destinationTile, "destinationTile", 0, false);
            Scribe_Values.Look<IntVec3>(ref destinationCell, "destinationCell", default, false);
            Scribe_Values.Look<PawnsArrivalModeDef>(ref arriveMode, "arriveMode", PawnsArrivalModeDefOf.EdgeDrop,
                false);
            Scribe_Values.Look<bool>(ref attackOnArrival, "attackOnArrival", false, false);
            Scribe_Values.Look<bool>(ref arrived, "arrived", false, false);
            Scribe_Values.Look<int>(ref initialTile, "initialTile", 0, false);
            Scribe_Values.Look<float>(ref traveledPct, "traveledPct", 0f, false);
        }

        public override void PostAdd()
        {
            base.PostAdd();
            initialTile = Tile;
        }

        public override void Tick()
        {
            base.Tick();
            traveledPct += TraveledPctStepPerTick;
            if (traveledPct >= 1f)
            {
                traveledPct = 1f;
                Arrived();
            }
        }

        public void AddPod(ActiveDropPodInfo contents, bool justLeftTheMap)
        {
            contents.parent = null;
            pods.Add(contents);
            ThingOwner innerContainer = contents.innerContainer;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                if (innerContainer[i] is Pawn pawn && !pawn.IsWorldPawn())
                {
                    if (!Spawned)
                    {
                        Log.Warning("Passing pawn " + pawn +
                                    " to world, but the TravelingTransportPod is not spawned. This means that WorldPawns can discard this pawn which can cause bugs.");
                    }
                    if (justLeftTheMap)
                    {
                        pawn.ExitMap(false, Rot4.Random);
                    }
                    else
                    {
                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                    }
                }
                if (innerContainer[i] is PawnFlyer pawnFlyer && !pawnFlyer.IsWorldPawn())
                {
                    if (!Spawned)
                    {
                        Log.Warning("Passing pawn " + pawnFlyer +
                                    " to world, but the TravelingTransportPod is not spawned. This means that WorldPawns can discard this pawn which can cause bugs.");
                    }
                    if (justLeftTheMap)
                    {
                        pawnFlyer.ExitMap(false, Rot4.Random);
                    }
                    else
                    {
                        Find.WorldPawns.PassToWorld(pawnFlyer, PawnDiscardDecideMode.Decide);
                    }
                }
            }
            contents.savePawnsWithReferenceMode = true;
        }

        public bool ContainsPawn(Pawn p)
        {
            for (int i = 0; i < pods.Count; i++)
            {
                if (pods[i].innerContainer.Contains(p))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsPawnFlyer(PawnFlyer p)
        {
            for (int i = 0; i < pods.Count; i++)
            {
                if (pods[i].innerContainer.Contains(p))
                {
                    return true;
                }
            }
            return false;
        }

        private void Arrived()
        {
            Cthulhu.Utility.DebugReport("Arrived");
            if (arrived)
            {
                return;
            }
            arrived = true;
            Map map = Current.Game.FindMap(destinationTile);
            if (map != null)
            {
                SpawnDropPodsInMap(map, null);
            }
            else if (!PodsHaveAnyPotentialCaravanOwner)
            {
                Caravan caravan = Find.WorldObjects.PlayerControlledCaravanAt(destinationTile);
                if (caravan != null)
                {
                    GivePodContentsToCaravan(caravan);
                }
                else
                {
                    for (int i = 0; i < pods.Count; i++)
                    {
                        pods[i].innerContainer.ClearAndDestroyContentsOrPassToWorld(DestroyMode.Vanish);
                    }
                    RemoveAllPods();
                    Find.WorldObjects.Remove(this);
                    Messages.Message("MessageTransportPodsArrivedAndLost".Translate(),
                        new GlobalTargetInfo(destinationTile), MessageTypeDefOf.NegativeEvent);
                }
            }
            else
            {
                MapParent mapParent = Find.WorldObjects.MapParentAt(destinationTile);
                if (mapParent != null && attackOnArrival)
                {
                    LongEventHandler.QueueLongEvent(delegate
                    {
                        Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(mapParent.Tile, null);
                        string extraMessagePart = null;
                        if (!mapParent.Faction.HostileTo(Faction.OfPlayer))
                        {
                            mapParent.Faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Hostile);
                            //mapParent.Faction.SetHostileTo(Faction.OfPlayer, true);
                            extraMessagePart = "MessageTransportPodsArrived_BecameHostile".Translate(
                                mapParent.Faction.Name
                            ).CapitalizeFirst();
                        }
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                        SpawnDropPodsInMap(mapParent.Map, extraMessagePart);
                    }, "GeneratingMapForNewEncounter", false, null);
                }
                else
                {
                    SpawnCaravanAtDestinationTile();
                }
            }
        }

        private void SpawnDropPodsInMap(Map map, string extraMessagePart = null)
        {
            Cthulhu.Utility.DebugReport("SpawnDropPodsInMap Called");
            RemoveAllPawnsFromWorldPawns();
            IntVec3 intVec;
            if (destinationCell.IsValid && destinationCell.InBounds(map))
            {
                intVec = destinationCell;
            }
            else if (arriveMode == PawnsArrivalModeDefOf.CenterDrop)
            {
                if (!DropCellFinder.TryFindRaidDropCenterClose(out intVec, map))
                {
                    intVec = DropCellFinder.FindRaidDropCenterDistant(map);
                }
            }
            else
            {
                if (arriveMode != PawnsArrivalModeDefOf.EdgeDrop && arriveMode != PawnsArrivalModeDefOf.EdgeDrop)
                {
                    Log.Warning("Unsupported arrive mode " + arriveMode);
                }
                intVec = DropCellFinder.FindRaidDropCenterDistant(map);
            }
            for (int i = 0; i < pods.Count; i++)
            {
                Cthulhu.Utility.DebugReport("PawnFlyerIncoming Generation Started");
                DropCellFinder.TryFindDropSpotNear(intVec, map, out IntVec3 c, false, true);
                PawnFlyersIncoming pawnFlyerIncoming =
                    (PawnFlyersIncoming) ThingMaker.MakeThing(PawnFlyerDef.incomingDef, null);
                pawnFlyerIncoming.pawnFlyer = pawnFlyer;
                pawnFlyerIncoming.Contents = pods[i];
                GenSpawn.Spawn(pawnFlyerIncoming, c, map);
            }
            RemoveAllPods();
            Find.WorldObjects.Remove(this);
            string text = "MessageTransportPodsArrived".Translate();
            if (extraMessagePart != null)
            {
                text = text + " " + extraMessagePart;
            }
            Messages.Message(text, new TargetInfo(intVec, map, false), MessageTypeDefOf.PositiveEvent);
        }

        private void GivePodContentsToCaravan(Caravan caravan)
        {
            for (int i = 0; i < pods.Count; i++)
            {
                List<Thing> tmpContainedThings = new List<Thing>();
                //PawnFlyersTraveling.tmpContainedThing.Clear();

                tmpContainedThings.AddRange(pods[i].innerContainer);
                //this.pods[i].innerContainer.
                for (int j = 0; j < tmpContainedThings.Count; j++)
                {
                    pods[i].innerContainer.Remove(tmpContainedThings[j]);
                    tmpContainedThings[j].holdingOwner = null;
                    if (tmpContainedThings[j] is Pawn pawn)
                    {
                        caravan.AddPawn(pawn, true);
                    }
                    else if (tmpContainedThings[j] is PawnFlyer pawnFlyer)
                    {
                        caravan.AddPawn(pawnFlyer, true);
                    }
                    else
                    {
                        Pawn pawn2 = CaravanInventoryUtility.FindPawnToMoveInventoryTo(tmpContainedThings[j],
                            caravan.PawnsListForReading, null, null);
                        bool flag = false;
                        if (pawn2 != null)
                        {
                            flag = pawn2.inventory.innerContainer.TryAdd(tmpContainedThings[j], true);
                        }
                        if (!flag)
                        {
                            tmpContainedThings[j].Destroy(DestroyMode.Vanish);
                        }
                    }
                }
            }
            RemoveAllPods();
            Find.WorldObjects.Remove(this);
            Messages.Message("MessageTransportPodsArrivedAndAddedToCaravan".Translate(), caravan,
                MessageTypeDefOf.PositiveEvent);
        }


        private void SpawnCaravanAtDestinationTile()
        {
            tmpPawns.Clear();
            for (int i = 0; i < pods.Count; i++)
            {
                ThingOwner innerContainer = pods[i].innerContainer;
                for (int j = 0; j < innerContainer.Count; j++)
                {
                    if (innerContainer[j] is Pawn pawn)
                    {
                        tmpPawns.Add(pawn);
                    }
                    else if (innerContainer[j] is PawnFlyer pawnFlyer)
                    {
                        tmpPawns.Add((Pawn)pawnFlyer);
                    }
                }
            }
            if (!GenWorldClosest.TryFindClosestPassableTile(destinationTile, out int startingTile))
            {
                startingTile = destinationTile;
            }
            Caravan o = CaravanMaker.MakeCaravan(tmpPawns, Faction.OfPlayer, startingTile, true);
            o.AddPawn((Pawn) pawnFlyer, false);
            for (int k = 0; k < pods.Count; k++)
            {
                ThingOwner innerContainer2 = pods[k].innerContainer;
                for (int l = 0; l < innerContainer2.Count; l++)
                {
                    if (!(innerContainer2[l] is Pawn))
                    {
                        Pawn pawn2 = CaravanInventoryUtility.FindPawnToMoveInventoryTo(innerContainer2[l],
                            tmpPawns, null, null);
                        pawn2.inventory.innerContainer.TryAdd(innerContainer2[l], true);
                    }
                    else
                    {
                        Pawn pawn3 = innerContainer2[l] as Pawn;
                        if (!pawn3.IsPrisoner)
                        {
                            if (pawn3.Faction != pawnFlyer.Faction)
                                pawn3.SetFaction(pawnFlyer.Faction);
                        }
                    }
                }
            }
            RemoveAllPods();
            Find.WorldObjects.Remove(this);
            Messages.Message("MessageTransportPodsArrived".Translate(), o, MessageTypeDefOf.PositiveEvent);
        }

        private void RemoveAllPawnsFromWorldPawns()
        {
            for (int i = 0; i < pods.Count; i++)
            {
                ThingOwner innerContainer = pods[i].innerContainer;
                for (int j = 0; j < innerContainer.Count; j++)
                {
                    Pawn pawn = innerContainer[j] as Pawn;
                    Pawn pawnFlyer = innerContainer[j] as PawnFlyer;
                    if (pawn != null && pawn.IsWorldPawn())
                    {
                        Find.WorldPawns.RemovePawn(pawn);
                    }
                    else if (pawnFlyer != null && pawn.IsWorldPawn())
                    {
                        Find.WorldPawns.RemovePawn(pawnFlyer);
                    }
                }
            }
        }

        private void RemoveAllPods()
        {
            for (int i = 0; i < pods.Count; i++)
            {
                pods[i].savePawnsWithReferenceMode = false;
            }
            pods.Clear();
        }
    }
}