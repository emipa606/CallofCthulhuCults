using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;
using Verse.Sound;
using RimWorld;

namespace CultOfCthulhu
{
    internal class Building_LandedShip : Building
    {

        public float pointsLeft = 300f;

        protected int age;

        private Lord lord;
        
        private static readonly HashSet<IntVec3> reachableCells = new HashSet<IntVec3>();

        public override void SpawnSetup(Map map, bool bla)
        {
            base.SpawnSetup(map, bla);
            TrySpawnMadSailors();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref pointsLeft, "pointsLeft", 0f, false);
            Scribe_Values.Look<int>(ref age, "age", 0, false);
            Scribe_References.Look<Lord>(ref lord, "defenseLord", false);
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            stringBuilder.AppendLine("AwokeDaysAgo".Translate(
                age.TicksToDays().ToString("F1")
            ));
            return stringBuilder.ToString();
        }

        public override void Tick()
        {
            base.Tick();
            age++;
        }
       
        private void TrySpawnMadSailors()
        {
            var lordList = new List<Pawn>();
            Faction faction = Find.FactionManager.FirstFactionOfDef(CultsDefOf.Cults_Sailors);
            Cthulhu.Utility.DebugReport(faction.ToString());
            //Log.Message("Building_LandedShip LordJob_DefendPoint");
            var lordJob = new LordJob_DefendPoint(Position);
            if (pointsLeft <= 0f)
            {
                return;
            }
            if (lord == null)
            {
                lord = LordMaker.MakeNewLord(faction, lordJob, Map, lordList);
            }
            while (pointsLeft > 0f)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(this)
                     where cell.Walkable(Map)
                     select cell).TryRandomElement(out IntVec3 center))
                {
                    var request = new PawnGenerationRequest(CultsDefOf.Cults_Sailor, faction, PawnGenerationContext.NonPlayer, Map.Tile, false, false, false, false, true, true, 20f, false, true, true, false, false, false, false, false, 0, null, 0, null, null, null);
                    Pawn pawn = PawnGenerator.GeneratePawn(request);
                    if (GenPlace.TryPlaceThing(pawn, center, Map, ThingPlaceMode.Near, null))
                    {

                        if (LordUtility.GetLord(pawn) != null)
                        {
                            LordUtility.GetLord(pawn).Cleanup();
                            LordUtility.GetLord(pawn).CurLordToil.Cleanup();
                            LordUtility.GetLord(pawn).LordJob.Cleanup();
                        }
                        lord.AddPawn(pawn);
                        pointsLeft -= pawn.kindDef.combatPower;
                        Cthulhu.Utility.ApplySanityLoss(pawn, 1f);
                        continue;
                    }
                    //Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                }
            }
            pointsLeft = 0f;
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera();
            return;
        }
        
    }
}
