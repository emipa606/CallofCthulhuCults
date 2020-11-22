using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;

namespace CultOfCthulhu
{
    public class Dialog_LoadTransportersPawn : Window
    {
        private enum Tab
        {
            Pawns,
            Items
        }

        private const float TitleRectHeight = 40f;

        private const float BottomAreaHeight = 55f;

        private readonly Map map;

        private readonly List<CompTransporterPawn> transporters;

        private List<TransferableOneWay> transferables;

        private TransferableOneWayWidget pawnsTransfer;

        private TransferableOneWayWidget itemsTransfer;

        private Dialog_LoadTransportersPawn.Tab tab;

        private float lastMassFlashTime = -9999f;

        private bool massUsageDirty = true;

        private float cachedMassUsage;

        private bool daysWorthOfFoodDirty = true;

        private Pair<float, float> cachedDaysWorthOfFood;

        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

        private static readonly List<TabRecord> tabsList = new List<TabRecord>();

        public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

        protected override float Margin => 0f;

        private BiomeDef Biome => map.Biome;


        private Pair<ThingDef, float> ForagedFoodPerDay
        {
            get
            {
                if (foragedFoodPerDayDirty)
                {
                    foragedFoodPerDayDirty = false;
                    var stringBuilder = new StringBuilder();
                    cachedForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(transferables,
                        Biome, Faction.OfPlayer, stringBuilder);
                    cachedForagedFoodPerDayExplanation = stringBuilder.ToString();
                }
                return cachedForagedFoodPerDay;
            }
        }

        private float Visibility
        {
            get
            {
                if (visibilityDirty)
                {
                    visibilityDirty = false;
                    var stringBuilder = new StringBuilder();
                    cachedVisibility = CaravanVisibilityCalculator.Visibility(transferables, stringBuilder);
                    cachedVisibilityExplanation = stringBuilder.ToString();
                }
                return cachedVisibility;
            }
        }


        private int PawnCapacity
        {
            get
            {
                var num = 0;
                for (var i = 0; i < transporters.Count; i++)
                {
                    var result = 1; //In-case PawnFlyer doesn't work out
                    if (transporters[i].parent is PawnFlyer pawnFlyer)
                    {
                        if (pawnFlyer.def is PawnFlyerDef pawnFlyerDef)
                        {
                            result = pawnFlyerDef.flightPawnLimit;
                        }
                    }
                    num += result;
                }
                return num;
            }
        }

        /// <summary>
        /// Modified to use PawnFlyerDef
        /// </summary>
        private float MassCapacity
        {
            get
            {
                var num = 0f;
                for (var i = 0; i < transporters.Count; i++)
                {
                    var result = 150f; //In-case PawnFlyer doesn't work out
                    if (transporters[i].parent is PawnFlyer pawnFlyer)
                    {
                        result = pawnFlyer.GetStatValue(StatDefOf.CarryingCapacity);
                        //PawnFlyerDef pawnFlyerDef = pawnFlyer.def as PawnFlyerDef;
                        //if (pawnFlyerDef != null)
                        //{
                        //    result = pawnFlyerDef.flightCarryCapacity;
                        //}
                    }
                    num += result;
                }
                return num;
            }
        }

        private string TransportersLabel => Find.ActiveLanguageWorker.Pluralize(transporters[0].parent.Label);

        private string TransportersLabelCap => TransportersLabel.CapitalizeFirst();

        private float MassUsage
        {
            get
            {
                if (massUsageDirty)
                {
                    massUsageDirty = false;
                    cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables,
                        IgnorePawnsInventoryMode.DontIgnore, true, false);
                }
                return cachedMassUsage;
            }
        }

        //private float DaysWorthOfFood
        //{
        //    get
        //    {
        //        if (this.daysWorthOfFoodDirty)
        //        {
        //            this.daysWorthOfFoodDirty = false;
        //            this.cachedDaysWorthOfFood = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(this.transferables);
        //        }
        //        return this.cachedDaysWorthOfFood;
        //    }
        //}

        private bool tilesPerDayDirty = true;
        private float cachedTilesPerDay;

        private string cachedTilesPerDayExplanation;

        private float TilesPerDay
        {
            get
            {
                if (tilesPerDayDirty)
                {
                    tilesPerDayDirty = false;
                    var stringBuilder = new StringBuilder();
                    cachedTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(transferables, MassUsage,
                        MassCapacity, map.Tile, -1, stringBuilder);
                    cachedTilesPerDayExplanation = stringBuilder.ToString();
                }
                return cachedTilesPerDay;
            }
        }

        public Dialog_LoadTransportersPawn(Map map, List<CompTransporterPawn> transporters)
        {
            this.map = map;
            this.transporters = new List<CompTransporterPawn>();
            this.transporters.AddRange(transporters);
            //this.closeOnEscapeKey = true;
            closeOnAccept = false;
            closeOnCancel = false;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            CalculateAndRecacheTransferables();
        }

        private bool foragedFoodPerDayDirty = true;

        private Pair<ThingDef, float> cachedForagedFoodPerDay;

        private string cachedForagedFoodPerDayExplanation;

        private bool visibilityDirty = true;

        private float cachedVisibility;

        private string cachedVisibilityExplanation;


        private Pair<float, float> DaysWorthOfFood
        {
            get
            {
                if (daysWorthOfFoodDirty)
                {
                    daysWorthOfFoodDirty = false;
                    var first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, map.Tile,
                        IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, Faction.OfPlayer, null, 0f, 3500);
                    cachedDaysWorthOfFood = new Pair<float, float>(first,
                        DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, map.Tile,
                            IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, null, 0f, 3500));
                }
                return cachedDaysWorthOfFood;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var rect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "LoadTransporters".Translate(
                TransportersLabel
            ));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            CaravanUIUtility.DrawCaravanInfo(
                new CaravanUIUtility.CaravanInfo(MassUsage, MassCapacity, "", TilesPerDay,
                    cachedTilesPerDayExplanation, DaysWorthOfFood, ForagedFoodPerDay,
                    cachedForagedFoodPerDayExplanation, Visibility, cachedVisibilityExplanation), null,
                map.Tile, null, lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), false, null,
                false);
            tabsList.Clear();
            tabsList.Add(new TabRecord("PawnsTab".Translate(),
                delegate { this.tab = Tab.Pawns; },
                this.tab == Tab.Pawns));
            tabsList.Add(new TabRecord("ItemsTab".Translate(),
                delegate { this.tab = Tab.Items; },
                this.tab == Tab.Items));
            inRect.yMin += 119f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, tabsList, 200f);
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax -= 59f;
            var flag = false;
            Dialog_LoadTransportersPawn.Tab tab = this.tab;
            if (tab != Tab.Pawns)
            {
                if (tab == Tab.Items)
                {
                    itemsTransfer.OnGUI(inRect2, out flag);
                }
            }
            else
            {
                pawnsTransfer.OnGUI(inRect2, out flag);
            }
            if (flag)
            {
                CountToTransferChanged();
            }
            GUI.EndGroup();
//            Rect rect = new Rect(0f, 0f, inRect.width, 40f);
//            Text.Font = GameFont.Medium;
//            Text.Anchor = TextAnchor.MiddleCenter;
//            Widgets.Label(rect, "LoadTransporters".Translate(new object[]
//            {
//                this.TransportersLabel
//            }));
//            Text.Font = GameFont.Small;
//            Text.Anchor = TextAnchor.UpperLeft;
//            Dialog_LoadTransportersPawn.tabsList.Clear();
//            Dialog_LoadTransportersPawn.tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate
//            {
//                this.tab = Dialog_LoadTransportersPawn.Tab.Pawns;
//            }, this.tab == Dialog_LoadTransportersPawn.Tab.Pawns));
//            //Dialog_LoadTransportersPawn.tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate
//            //{
//            //    this.tab = Dialog_LoadTransportersPawn.Tab.Items;
//            //}, this.tab == Dialog_LoadTransportersPawn.Tab.Items));
//            inRect.yMin += 72f;
//            Widgets.DrawMenuSection(inRect);
//            TabDrawer.DrawTabs(inRect, Dialog_LoadTransportersPawn.tabsList);
//            inRect = inRect.ContractedBy(17f);
//            GUI.BeginGroup(inRect);
//            Rect rect2 = inRect.AtZero();
//            Rect rect3 = rect2;
//            rect3.xMin += rect2.width - this.pawnsTransfer.TotalNumbersColumnsWidths;
//            rect3.y += 32f;
//            TransferableUIUtility.DrawMassInfo(rect3, this.MassUsage, this.MassCapacity, "TransportersMassUsageTooltip".Translate(), this.lastMassFlashTime, true);
//            //CaravanUIUtility.DrawDaysWorthOfFoodInfo(new Rect(rect3.x, rect3.y + 22f, rect3.width, rect3.height), this.DaysWorthOfFood, true);
//            this.DoBottomButtons(rect2);
//            Rect inRect2 = rect2;
//            inRect2.yMax -= 59f;
//            bool flag = false;
//            Dialog_LoadTransportersPawn.Tab tab = this.tab;
//            if (tab != Dialog_LoadTransportersPawn.Tab.Pawns)
//            {
//                if (tab == Dialog_LoadTransportersPawn.Tab.Items)
//                {
//                    this.itemsTransfer.OnGUI(inRect2, out flag);
//                }
//            }
//            else
//            {
//                this.pawnsTransfer.OnGUI(inRect2, out flag);
//            }
//            if (flag)
//            {
//                this.CountToTransferChanged();
//            }
//            GUI.EndGroup();
        }

        public override bool CausesMessageBackground()
        {
            return true;
        }

        private void AddToTransferables(Thing t)
        {
            TransferableOneWay transferableOneWay =
                TransferableUtility.TransferableMatching<TransferableOneWay>(t, transferables,
                    TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(t);
        }

        private void DoBottomButtons(Rect rect)
        {
            var rect2 = new Rect((rect.width / 2f) - (BottomButtonSize.x / 2f), rect.height - 55f,
                BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true) && TryAccept())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Close(false);
            }
            var rect3 = new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x,
                BottomButtonSize.y);
            if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                CalculateAndRecacheTransferables();
            }
            var rect4 = new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
            if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
            {
                Close(true);
            }
            if (Prefs.DevMode)
            {
                var num = 200f;
                var num2 = BottomButtonSize.y / 2f;
                var rect5 = new Rect(rect.width - num, rect.height - 55f, num, num2);
                if (Widgets.ButtonText(rect5, "Dev: Load instantly", true, false, true) && DebugTryLoadInstantly())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    Close(false);
                }
                var rect6 = new Rect(rect.width - num, rect.height - 55f + num2, num, num2);
                if (Widgets.ButtonText(rect6, "Dev: Select everything", true, false, true))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    SetToLoadEverything();
                }
            }
        }

        private void CalculateAndRecacheTransferables()
        {
            transferables = new List<TransferableOneWay>();
            AddPawnsToTransferables();
            AddItemsToTransferables();
            pawnsTransfer = new TransferableOneWayWidget(null, Faction.OfPlayer.Name, TransportersLabelCap,
                "FormCaravanColonyThingCountTip".Translate(), true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                true, () => MassCapacity - MassUsage, 24f, false, map.Tile, true);
            CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);
            itemsTransfer = new TransferableOneWayWidget(from x in transferables
                where x.ThingDef.category != ThingCategory.Pawn
                select x, Faction.OfPlayer.Name, TransportersLabelCap,
                "FormCaravanColonyThingCountTip".Translate(), true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
                true, () => MassCapacity - MassUsage, 24f, false, map.Tile, true);
            CountToTransferChanged();
        }

        private bool DebugTryLoadInstantly()
        {
            CreateAndAssignNewTransportersGroup();
            int i;
            for (i = 0; i < transferables.Count; i++)
            {
                TransferableUtility.Transfer(transferables[i].things, transferables[i].CountToTransfer,
                    delegate(Thing splitPiece, IThingHolder originalThing)
                    {
                        transporters[i % transporters.Count].GetDirectlyHeldThings().TryAdd(splitPiece, true);
                    });
            }
            return true;
        }

        private bool TryAccept()
        {
            List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
            if (!CheckForErrors(pawnsFromTransferables))
            {
                Cthulhu.Utility.DebugReport("TryAccept Failed");
                return false;
            }
            Cthulhu.Utility.DebugReport("TryAccept Succeeded");
            var transportersGroup = CreateAndAssignNewTransportersGroup();
            AssignTransferablesToRandomTransporters();
            IEnumerable<Pawn> enumerable = from x in pawnsFromTransferables
                where x.IsColonist && !x.Downed
                select x;
            if (enumerable.Any<Pawn>())
            {
                Cthulhu.Utility.DebugReport("Pawn List Succeeded");
                LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadAndEnterTransportersPawn(transportersGroup),
                    map, enumerable);
                foreach (Pawn current in enumerable)
                {
                    if (current.Spawned)
                    {
                        current.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
            }
            Messages.Message("MessageTransportersLoadingProcessStarted".Translate(), transporters[0].parent,
                MessageTypeDefOf.PositiveEvent);
            return true;
        }

        private void AssignTransferablesToRandomTransporters()
        {
            Cthulhu.Utility.DebugReport("AssignTransferablesToRandomTransporters Called");
            TransferableOneWay transferableOneWay =
                transferables.MaxBy((TransferableOneWay x) => x.CountToTransfer);
            var num = 0;
            for (var i = 0; i < transferables.Count; i++)
            {
                if (transferables[i] != transferableOneWay)
                {
                    if (transferables[i].CountToTransfer > 0)
                    {
                        transporters[num % transporters.Count].AddToTheToLoadList(transferables[i],
                            transferables[i].CountToTransfer);
                        num++;
                    }
                }
            }
            if (num < transporters.Count)
            {
                var num2 = transferableOneWay.CountToTransfer;
                var num3 = num2 / (transporters.Count - num);
                for (var j = num; j < transporters.Count; j++)
                {
                    var num4 = (j != transporters.Count - 1) ? num3 : num2;
                    if (num4 > 0)
                    {
                        transporters[j].AddToTheToLoadList(transferableOneWay, num4);
                    }
                    num2 -= num4;
                }
            }
            else
            {
                transporters[num % transporters.Count]
                    .AddToTheToLoadList(transferableOneWay, transferableOneWay.CountToTransfer);
            }
        }

        private int CreateAndAssignNewTransportersGroup()
        {
            Cthulhu.Utility.DebugReport("CreateAndAssignNewTransportersGroup Called");
            var nextTransporterGroupID = Find.UniqueIDsManager.GetNextTransporterGroupID();
            for (var i = 0; i < transporters.Count; i++)
            {
                transporters[i].groupID = nextTransporterGroupID;
            }
            return nextTransporterGroupID;
        }

        private bool CheckForErrors(List<Pawn> pawns)
        {
            if (!transferables.Any((TransferableOneWay x) => x.CountToTransfer != 0))
            {
                Messages.Message("CantSendEmptyTransportPods".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (MassUsage > MassCapacity)
            {
                FlashMass();
                Messages.Message("TooBigTransportersMassUsage".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (pawns.Count > PawnCapacity)
            {
                Messages.Message("OverPawnRiderLimit".Translate(
                    PawnCapacity.ToString()
                ), MessageTypeDefOf.RejectInput);
                return false;
            }

            Pawn pawn = pawns.Find((Pawn x) => !x.MapHeld.reachability.CanReach(x.PositionHeld,
                transporters[0].parent, PathEndMode.Touch,
                TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)));
            if (pawn != null)
            {
                Messages.Message("PawnCantReachTransporters".Translate(
                    pawn.LabelShort
                ).CapitalizeFirst(), MessageTypeDefOf.RejectInput);
                return false;
            }
            Map map = transporters[0].parent.Map;
            for (var i = 0; i < transferables.Count; i++)
            {
                if (transferables[i].ThingDef.category == ThingCategory.Item)
                {
                    var countToTransfer = transferables[i].CountToTransfer;
                    var num = 0;
                    if (countToTransfer > 0)
                    {
                        for (var j = 0; j < transferables[i].things.Count; j++)
                        {
                            Thing thing = transferables[i].things[j];
                            if (map.reachability.CanReach(thing.Position, transporters[0].parent,
                                PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                            {
                                num += thing.stackCount;
                                if (num >= countToTransfer)
                                {
                                    break;
                                }
                            }
                        }
                        if (num < countToTransfer)
                        {
                            if (countToTransfer == 1)
                            {
                                Messages.Message("TransporterItemIsUnreachableSingle".Translate(
                                    transferables[i].ThingDef.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                            else
                            {
                                Messages.Message("TransporterItemIsUnreachableMulti".Translate(
                                    countToTransfer,
                                    transferables[i].ThingDef.label
                                ), MessageTypeDefOf.RejectInput);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void AddPawnsToTransferables()
        {
            List<Pawn> list = CaravanFormingUtility.AllSendablePawns(map, false, false);
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].TryGetComp<CompLaunchablePawn>() == null)
                {
                    AddToTransferables(list[i]);
                }
            }
        }

        private void AddItemsToTransferables()
        {
            List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(map, false, false);
            for (var i = 0; i < list.Count; i++)
            {
                AddToTransferables(list[i]);
            }
        }

        private void FlashMass()
        {
            lastMassFlashTime = Time.time;
        }

        private void SetToLoadEverything()
        {
            for (var i = 0; i < transferables.Count; i++)
            {
                transferables[i].AdjustTo(transferables[i].GetMaximumToTransfer()); // SetToTransferMaxToDest();
                //TransferableUIUtility.ClearEditBuffer(this.transferables[i]);
            }
            CountToTransferChanged();
        }

        private void CountToTransferChanged()
        {
            massUsageDirty = true;
            daysWorthOfFoodDirty = true;
        }
    }
}