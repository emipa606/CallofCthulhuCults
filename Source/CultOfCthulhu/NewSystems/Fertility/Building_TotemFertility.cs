// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
//using Verse.AI;          // Needed when you do something with the AI
//using Verse.AI.Group;
//using Verse.Sound;       // Needed when you do something with Sound
//using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
//using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace CultOfCthulhu
{
    public class Building_TotemFertility : Building
    {
        public float fertilityBonus = 0.5f;
        public float fertilityMax = 2.0f;
        public float ticksUntilDestroyed = float.MinValue;
        public float daysUntilDestroyed = GenDate.DaysPerQuadrum * 2;

        public bool cellsDirty = false;

        List<IntVec3> tempCells;
        public List<IntVec3> GrowableCells
        {
            get
            {
                if (tempCells.NullOrEmpty() || cellsDirty)
                {
                    cellsDirty = false;
                    tempCells = new List<IntVec3>(GenRadial.RadialCellsAround(Position, def.specialDisplayRadius, true));
                }
                return tempCells;
            }
        }
        
        public int TicksUntilDisappearing
        {
            get
            {
                if (ticksUntilDestroyed == float.MinValue)
                {
                    ticksUntilDestroyed = ticksUntilDestroyed = daysUntilDestroyed * 60000f;
                }
                return Mathf.RoundToInt(ticksUntilDestroyed);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
                        
            Scribe_Values.Look<float>(ref ticksUntilDestroyed, "ticksUntilDestroyed", -1f);
            Scribe_Values.Look<float>(ref daysUntilDestroyed, "daysUntilDestroyed", 7f);
            Scribe_Values.Look<float>(ref fertilityBonus, "fertilityBonus", 1.5f);
        }

        public override void Tick()
        {
            base.Tick();
            if (ticksUntilDestroyed > 0)
            {
                if (ticksUntilDestroyed < 100)
                {
                    DeSpawn();
                }
                else
                {
                    ticksUntilDestroyed -= 1;
                }
            }
        }


        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            // Add the inspections string from the base
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }
            if (!stringBuilder.ToString().Contains("installed"))
            { 
                stringBuilder.Append("FertilityTotemTimer".Translate(
                TicksUntilDisappearing.ToStringTicksToPeriodVague()
                ));
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void SpawnSetup(Map map, bool bla)
        {
            base.SpawnSetup(map, bla);
            var temp = new List<IntVec3>();
            foreach (IntVec3 vec in GrowableCells)
            {
                temp.Add(vec);
            }
            map.GetComponent<MapComponent_FertilityMods>().FertilityTotems.Add(this);
            map.GetComponent<MapComponent_FertilityMods>().FertilizeCells(temp);
            cellsDirty = true;

        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            base.DeSpawn(mode);
            var temp = new List<IntVec3>();
            foreach (IntVec3 vec in GrowableCells)
            {
                temp.Add(vec);
            }
            map.GetComponent<MapComponent_FertilityMods>().FertilityTotems.Remove(this);
            map.GetComponent<MapComponent_FertilityMods>().UnfertilizeCells(temp);
            cellsDirty = true;

        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerator<Gizmo> enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }
            yield return new Command_Action
            {
                action = new Action(MakeMatchingGrowZone),
                hotKey = KeyBindingDefOf.Misc2,
                defaultDesc = "CommandSunLampMakeGrowingZoneDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Growing"),
                defaultLabel = "CommandSunLampMakeGrowingZoneLabel".Translate()
            };
            yield break;
        }

        private void MakeMatchingGrowZone()
        {
            var designator = new Designator_ZoneAdd_Growing();
            designator.DesignateMultiCell(from tempCell in GrowableCells
                                          where designator.CanDesignateCell(tempCell).Accepted
                                          select tempCell);
        }
    }
}
