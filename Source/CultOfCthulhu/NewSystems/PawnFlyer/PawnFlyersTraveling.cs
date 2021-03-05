using System.Collections.Generic;
using Cthulhu;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CultOfCthulhu
{
    public class PawnFlyersTraveling : WorldObject
    {
        private static readonly List<Pawn> tmpPawns = new List<Pawn>();

        private bool arrived;

        public PawnsArrivalModeDef arriveMode;

        public bool attackOnArrival;

        public IntVec3 destinationCell = IntVec3.Invalid;


        public int destinationTile = -1;

        private int initialTile = -1;
        public PawnFlyer pawnFlyer;

        private List<ActiveDropPodInfo> pods = new List<ActiveDropPodInfo>();

        private float traveledPct;

        private float TravelSpeed => PawnFlyerDef.flightSpeed;

        private PawnFlyerDef PawnFlyerDef => pawnFlyer.def as PawnFlyerDef;

        private Vector3 Start => Find.WorldGrid.GetTileCenter(initialTile);

        private Vector3 End => Find.WorldGrid.GetTileCenter(destinationTile);

        public override Vector3 DrawPos => Vector3.Slerp(Start, End, traveledPct);

        private float TraveledPctStepPerTick
        {
            get
            {
                var start = Start;
                var end = End;
                if (start == end)
                {
                    return 1f;
                }

                var num = GenMath.SphericalDistance(start.normalized, end.normalized);
                return num == 0f ? 1f : 0.00025f / num;
            }
        }

        //There is always the byakhee
        private bool PodsHaveAnyPotentialCaravanOwner => true;

        public bool PodsHaveAnyFreeColonist
        {
            get
            {
                for (var i = 0; i < pods.Count; i++)
                {
                    var innerContainer = pods[i].innerContainer;
                    for (var j = 0; j < innerContainer.Count; j++)
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
                for (var i = 0; i < pods.Count; i++)
                {
                    var innerContainer = pods[i].innerContainer;
                    for (var j = 0; j < innerContainer.Count; j++)
                    {
                        if (innerContainer[j] is Pawn pawn)
                        {
                            yield return pawn;
                        }
                        else if (innerContainer[j] is PawnFlyer pawnFlyer)
                        {
                            yield return pawnFlyer;
                        }
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            //Pawn
            Scribe_References.Look(ref pawnFlyer, "pawnFlyer");

            //Vanilla
            Scribe_Collections.Look(ref pods, "pods", LookMode.Deep);
            Scribe_Values.Look(ref destinationTile, "destinationTile");
            Scribe_Values.Look(ref destinationCell, "destinationCell");
            Scribe_Values.Look(ref arriveMode, "arriveMode", PawnsArrivalModeDefOf.EdgeDrop);
            Scribe_Values.Look(ref attackOnArrival, "attackOnArrival");
            Scribe_Values.Look(ref arrived, "arrived");
            Scribe_Values.Look(ref initialTile, "initialTile");
            Scribe_Values.Look(ref traveledPct, "traveledPct");
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
            var innerContainer = contents.innerContainer;
            for (var i = 0; i < innerContainer.Count; i++)
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
                        Find.WorldPawns.PassToWorld(pawn);
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
                        Find.WorldPawns.PassToWorld(pawnFlyer);
                    }
                }
            }

            contents.savePawnsWithReferenceMode = true;
        }

        public bool ContainsPawn(Pawn p)
        {
            for (var i = 0; i < pods.Count; i++)
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
            for (var i = 0; i < pods.Count; i++)
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
            Utility.DebugReport("Arrived");
            if (arrived)
            {
                return;
            }

            arrived = true;
            var map = Current.Game.FindMap(destinationTile);
            if (map != null)
            {
                SpawnDropPodsInMap(map);
            }
            else if (!PodsHaveAnyPotentialCaravanOwner)
            {
                var caravan = Find.WorldObjects.PlayerControlledCaravanAt(destinationTile);
                if (caravan != null)
                {
                    GivePodContentsToCaravan(caravan);
                }
                else
                {
                    for (var i = 0; i < pods.Count; i++)
                    {
                        pods[i].innerContainer.ClearAndDestroyContentsOrPassToWorld();
                    }

                    RemoveAllPods();
                    Find.WorldObjects.Remove(this);
                    Messages.Message("MessageTransportPodsArrivedAndLost".Translate(),
                        new GlobalTargetInfo(destinationTile), MessageTypeDefOf.NegativeEvent);
                }
            }
            else
            {
                var mapParent = Find.WorldObjects.MapParentAt(destinationTile);
                if (mapParent != null && attackOnArrival)
                {
                    LongEventHandler.QueueLongEvent(delegate
                    {
                        var orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(mapParent.Tile, null);
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
            Utility.DebugReport("SpawnDropPodsInMap Called");
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

            for (var i = 0; i < pods.Count; i++)
            {
                Utility.DebugReport("PawnFlyerIncoming Generation Started");
                DropCellFinder.TryFindDropSpotNear(intVec, map, out var c, false, true);
                var pawnFlyerIncoming =
                    (PawnFlyersIncoming) ThingMaker.MakeThing(PawnFlyerDef.incomingDef);
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

            Messages.Message(text, new TargetInfo(intVec, map), MessageTypeDefOf.PositiveEvent);
        }

        private void GivePodContentsToCaravan(Caravan caravan)
        {
            for (var i = 0; i < pods.Count; i++)
            {
                var tmpContainedThings = new List<Thing>();
                //PawnFlyersTraveling.tmpContainedThing.Clear();

                tmpContainedThings.AddRange(pods[i].innerContainer);
                //this.pods[i].innerContainer.
                for (var j = 0; j < tmpContainedThings.Count; j++)
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
                        var pawn2 = CaravanInventoryUtility.FindPawnToMoveInventoryTo(tmpContainedThings[j],
                            caravan.PawnsListForReading, null);
                        var flag = false;
                        if (pawn2 != null)
                        {
                            flag = pawn2.inventory.innerContainer.TryAdd(tmpContainedThings[j]);
                        }

                        if (!flag)
                        {
                            tmpContainedThings[j].Destroy();
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
            for (var i = 0; i < pods.Count; i++)
            {
                var innerContainer = pods[i].innerContainer;
                for (var j = 0; j < innerContainer.Count; j++)
                {
                    if (innerContainer[j] is Pawn pawn)
                    {
                        tmpPawns.Add(pawn);
                    }
                    else if (innerContainer[j] is PawnFlyer pawnFlyer)
                    {
                        tmpPawns.Add(pawnFlyer);
                    }
                }
            }

            if (!GenWorldClosest.TryFindClosestPassableTile(destinationTile, out var startingTile))
            {
                startingTile = destinationTile;
            }

            var o = CaravanMaker.MakeCaravan(tmpPawns, Faction.OfPlayer, startingTile, true);
            o.AddPawn(pawnFlyer, false);
            for (var k = 0; k < pods.Count; k++)
            {
                var innerContainer2 = pods[k].innerContainer;
                for (var l = 0; l < innerContainer2.Count; l++)
                {
                    if (!(innerContainer2[l] is Pawn))
                    {
                        var pawn2 = CaravanInventoryUtility.FindPawnToMoveInventoryTo(innerContainer2[l],
                            tmpPawns, null);
                        pawn2.inventory.innerContainer.TryAdd(innerContainer2[l]);
                    }
                    else
                    {
                        var pawn3 = innerContainer2[l] as Pawn;
                        if (!pawn3.IsPrisoner)
                        {
                            if (pawn3.Faction != pawnFlyer.Faction)
                            {
                                pawn3.SetFaction(pawnFlyer.Faction);
                            }
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
            for (var i = 0; i < pods.Count; i++)
            {
                var innerContainer = pods[i].innerContainer;
                for (var j = 0; j < innerContainer.Count; j++)
                {
                    var pawn = innerContainer[j] as Pawn;
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
            for (var i = 0; i < pods.Count; i++)
            {
                pods[i].savePawnsWithReferenceMode = false;
            }

            pods.Clear();
        }
    }
}