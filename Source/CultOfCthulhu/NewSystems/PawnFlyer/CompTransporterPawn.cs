using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;
using System.Linq;

namespace CultOfCthulhu
{
    [StaticConstructorOnStartup]

    public class CompTransporterPawn : ThingComp, IThingHolder
    {
        public int groupID = -1;

        private ThingOwner innerContainer;

        public List<TransferableOneWay> leftToLoad;

        private CompLaunchablePawn cachedCompLaunchablePawn;

        public static readonly Texture2D CancelLoadCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);

        public static readonly Texture2D LoadCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", true);

        public static readonly Texture2D SelectPreviousInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectPreviousTransporter", true);

        public static readonly Texture2D SelectAllInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectAllTransporters", true);

        public static readonly Texture2D SelectNextInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectNextTransporter", true);

        public static List<CompTransporterPawn> tmpTransportersInGroup = new List<CompTransporterPawn>();

        public CompProperties_TransporterPawn Props
        {
            get
            {
                return (CompProperties_TransporterPawn)props;
            }
        }

        public Map Map
        {
            get
            {
                return parent.MapHeld;
            }
        }

        public bool Spawned
        {
            get
            {
                return parent.Spawned;
            }
        }

        public bool AnythingLeftToLoad
        {
            get
            {
                return FirstThingLeftToLoad != null;
            }
        }

        public bool LoadingInProgressOrReadyToLaunch
        {
            get
            {
                return groupID >= 0;
            }
        }

        public bool AnyInGroupHasAnythingLeftToLoad
        {
            get
            {
                return FirstThingLeftToLoadInGroup != null;
            }
        }

        public CompLaunchablePawn Launchable
        {
            get
            {
                if (cachedCompLaunchablePawn == null)
                {
                    cachedCompLaunchablePawn = parent.GetComp<CompLaunchablePawn>();
                }
                return cachedCompLaunchablePawn;
            }
        }

        public Thing FirstThingLeftToLoad
        {
            get
            {
                if (leftToLoad == null)
                {
                    return null;
                }
                TransferableOneWay transferableOneWay = leftToLoad.Find((TransferableOneWay x) => x.CountToTransfer != 0 && x.HasAnyThing);
                return transferableOneWay?.AnyThing;
            }
        }

        public Thing FirstThingLeftToLoadInGroup
        {
            get
            {
                List<CompTransporterPawn> list = TransportersInGroup(parent.Map);
                for (int i = 0; i < list.Count; i++)
                {
                    Thing firstThingLeftToLoad = list[i].FirstThingLeftToLoad;
                    if (firstThingLeftToLoad != null)
                    {
                        return firstThingLeftToLoad;
                    }
                }
                return null;
            }
        }

        public CompTransporterPawn()
        {
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref groupID, "groupID", 0, false);
            Scribe_Deep.Look<ThingOwner>(ref innerContainer, "innerContainer", new object[]
            {
                this
            });
            Scribe_Collections.Look<TransferableOneWay>(ref leftToLoad, "leftToLoad", LookMode.Deep, new object[0]);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public IntVec3 GetPosition()
        {
            return parent.PositionHeld;
        }

        public Map GetMap()
        {
            return parent.MapHeld;
        }

        public List<CompTransporterPawn> TransportersInGroup(Map map)
        {
            if (!LoadingInProgressOrReadyToLaunch)
            {
                return null;
            }

            tmpTransportersInGroup.Clear();
            if (groupID < 0)
            {
                return null;
            }
            IEnumerable<Pawn> listSel = from Pawn pawns in map.mapPawns.AllPawnsSpawned
                                        where pawns is PawnFlyer
                                        select pawns;
            List<Pawn> list = new List<Pawn>(listSel);
            for (int i = 0; i < list.Count; i++)
            {
                CompTransporterPawn compTransporter = list[i].TryGetComp<CompTransporterPawn>();
                if (compTransporter.groupID == groupID)
                {
                    tmpTransportersInGroup.Add(compTransporter);
                }
            }

            return tmpTransportersInGroup;
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            IEnumerator<Gizmo> enumerator = base.CompGetGizmosExtra().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }
            if (LoadingInProgressOrReadyToLaunch)
            {
                yield return new Command_Action
                {
                    defaultLabel = "CommandCancelLoad".Translate(),
                    defaultDesc = "CommandCancelLoadDesc".Translate(),
                    icon = CancelLoadCommandTex,
                    action = delegate
                    {
                        SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
                        CancelLoad();
                    }
                };
            }
            Command_LoadToTransporterPawn Command_LoadToTransporterPawn = new Command_LoadToTransporterPawn();
            int num = 0;
            for (int i = 0; i < Find.Selector.NumSelected; i++)
            {
                if (Find.Selector.SelectedObjectsListForReading[i] is Thing thing && thing.def == parent.def)
                {
                    CompLaunchablePawn CompLaunchablePawn = thing.TryGetComp<CompLaunchablePawn>();
                    if (CompLaunchablePawn == null)
                    {
                        num++;
                    }
                }
            }
            Command_LoadToTransporterPawn.defaultLabel = "CommandLoadTransporter".Translate(
                num.ToString()
            );
            Command_LoadToTransporterPawn.defaultDesc = "CommandLoadTransporterDesc".Translate();
            Command_LoadToTransporterPawn.icon = LoadCommandTex;
            Command_LoadToTransporterPawn.transComp = this;
            CompLaunchablePawn launchable = Launchable;
            //if (launchable != null)
            //{
            //    if (!launchable.ConnectedToFuelingPort)
            //    {
            //        Command_LoadToTransporterPawn.Disable("CommandLoadTransporterFailNotConnectedToFuelingPort".Translate());
            //    }
            //    else if (!launchable.FuelingPortSourceHasAnyFuel)
            //    {
            //        Command_LoadToTransporterPawn.Disable("CommandLoadTransporterFailNoFuel".Translate());
            //    }
            //}
            yield return Command_LoadToTransporterPawn;
            yield break;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (CancelLoad(map))
            {
                Messages.Message("MessageTransportersLoadCanceled_TransporterDestroyed".Translate(), MessageTypeDefOf.NegativeEvent);
            }
        }

        public void AddToTheToLoadList(TransferableOneWay t, int count)
        {
            if (!t.HasAnyThing || t.CountToTransfer <= 0)
            {
                return;
            }
            if (leftToLoad == null)
            {
                leftToLoad = new List<TransferableOneWay>();
            }
            if (TransferableUtility.TransferableMatching<TransferableOneWay>(t.AnyThing, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking) != null)
            {
                Log.Error("Transferable already exists.");
                return;
            }
            TransferableOneWay transferableOneWay = new TransferableOneWay();
            leftToLoad.Add(transferableOneWay);
            transferableOneWay.things.AddRange(t.things);
            transferableOneWay.AdjustTo(count);
        }

        public void Notify_ThingAdded(Thing t)
        {
            SubtractFromToLoadList(t, t.stackCount);
        }

        public void Notify_PawnEnteredTransporterOnHisOwn(Pawn p)
        {
            SubtractFromToLoadList(p, 1);
        }

        public bool CancelLoad()
        {
            return CancelLoad(Map);
        }

        public bool CancelLoad(Map map)
        {
            if (!LoadingInProgressOrReadyToLaunch)
            {
                return false;
            }
            TryRemoveLord(map);
            List<CompTransporterPawn> list = TransportersInGroup(map);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].CleanUpLoadingVars(map);
            }
            CleanUpLoadingVars(map);
            return true;
        }

        // RimWorld.TransporterUtility
        public static Lord FindLord(int transportersGroup, Map map)
        {
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                if (lords[i].LordJob is LordJob_LoadAndEnterTransportersPawn lordJob_LoadAndEnterTransporters && lordJob_LoadAndEnterTransporters.transportersGroup == transportersGroup)
                {
                    return lords[i];
                }
            }
            return null;
        }

        public void TryRemoveLord(Map map)
        {
            if (!LoadingInProgressOrReadyToLaunch)
            {
                return;
            }
            Lord lord = FindLord(groupID, map);
            if (lord != null)
            {
                map.lordManager.RemoveLord(lord);
            }
        }

        public void CleanUpLoadingVars(Map map)
        {
            groupID = -1;
            innerContainer.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
            if (leftToLoad != null)
            {
                leftToLoad.Clear();
            }
        }

        private void SubtractFromToLoadList(Thing t, int count)
        {
            if (leftToLoad == null)
            {
                return;
            }
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay == null)
            {
                return;
            }
            transferableOneWay.AdjustBy(-count);
            if (transferableOneWay.CountToTransfer <= 0)
            {
                leftToLoad.Remove(transferableOneWay);
            }
            if (!AnyInGroupHasAnythingLeftToLoad)
            {
                Messages.Message("MessageFinishedLoadingTransporters".Translate(), parent, MessageTypeDefOf.PositiveEvent);
            }
        }

        private void SelectPreviousInGroup()
        {
            List<CompTransporterPawn> list = TransportersInGroup(Map);
            int num = list.IndexOf(this);
            CameraJumper.TryJumpAndSelect(list[GenMath.PositiveMod(num - 1, list.Count)].parent);
        }

        private void SelectAllInGroup()
        {
            List<CompTransporterPawn> list = TransportersInGroup(Map);
            Selector selector = Find.Selector;
            selector.ClearSelection();
            for (int i = 0; i < list.Count; i++)
            {
                selector.Select(list[i].parent, true, true);
            }
        }

        private void SelectNextInGroup()
        {
            List<CompTransporterPawn> list = TransportersInGroup(Map);
            int num = list.IndexOf(this);
            CameraJumper.TryJumpAndSelect(list[(num + 1) % list.Count].parent);
        }
    }
}
