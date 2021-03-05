using Cthulhu;
using Verse;
using Verse.AI.Group;

namespace CultOfCthulhu
{
    public class LordJob_LoadAndEnterTransportersPawn : LordJob
    {
        public int transportersGroup = -1;

        public LordJob_LoadAndEnterTransportersPawn()
        {
            Utility.DebugReport("LoadAndEnterTransportersPawn LordJob Constructed");
        }

        public LordJob_LoadAndEnterTransportersPawn(int transportersGroup)
        {
            Utility.DebugReport("LoadAndEnterTransportersPawn LordJob Constructed");
            this.transportersGroup = transportersGroup;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref transportersGroup, "transportersGroup");
        }

        public override StateGraph CreateGraph()
        {
            var stateGraph = new StateGraph();
            var lordToil_LoadAndEnterTransporters = new LordToil_LoadAndEnterTransportersPawn(transportersGroup);
            stateGraph.StartingToil = lordToil_LoadAndEnterTransporters;
            var lordToil_End = new LordToil_End();
            stateGraph.AddToil(lordToil_End);
            var transition = new Transition(lordToil_LoadAndEnterTransporters, lordToil_End);
            transition.AddTrigger(new Trigger_PawnLost());
            //transition.AddPreAction(new TransitionAction_Message("MessageFailedToLoadTransportersBecauseColonistLost".Translate(), MessageTypeDefOf.NegativeEvent));
            transition.AddPreAction(new TransitionAction_Custom(CancelLoadingProcess));
            stateGraph.AddTransition(transition);
            return stateGraph;
        }

        private void CancelLoadingProcess()
        {
            var list = lord.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn);
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    if (list[i] is Pawn || list[i] is PawnFlyer)
                    {
                        var compTransporter = list[i].TryGetComp<CompTransporterPawn>();
                        if (compTransporter != null)
                        {
                            if (compTransporter.groupID == transportersGroup)
                            {
                                compTransporter.CancelLoad();
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}