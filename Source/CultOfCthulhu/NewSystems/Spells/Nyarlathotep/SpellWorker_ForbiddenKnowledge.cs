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
using Verse.AI;          // Needed when you do something with the AI
using Verse.AI.Group;
using Verse.Sound;       // Needed when you do something with Sound
using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace CultOfCthulhu
{
    public class SpellWorker_ForbiddenKnowledge : SpellWorker 
    {
        private Reason failReason = Reason.Null;

        private enum Reason
        {
            Null = 0,
            NoBenches,
            NoResearchProject
        };


        protected Building_ResearchBench ResearchStation(Map map)
        {
            IEnumerable<Building_ResearchBench> benches = map.listerBuildings.AllBuildingsColonistOfClass<Building_ResearchBench>();
            if (benches != null)
            {
                if (benches.TryRandomElement<Building_ResearchBench>(out Building_ResearchBench bench))
                {
                    return bench;
                }
            }
            return null;
        }

        protected ResearchProjectDef ResearchProject()
        {
            return Find.ResearchManager.currentProj;
        }

        public override bool CanSummonNow(Map map)
        {
            var flag = false;
            if (ResearchStation(map) != null && ResearchProject() != null)
            {
                flag = true;
            }

            if (ResearchStation(map) == null)
            {
                failReason = Reason.NoBenches;
                flag = false;
            }
            if (ResearchProject() == null)
            {
                failReason = Reason.NoResearchProject;
                flag = false;
            }

            if (flag)
            {

                //Cthulhu.Utility.DebugReport("CanFire: " + this.def.defName);
                return true;
            }
            else if (failReason == Reason.NoBenches)
            {
                Messages.Message("There are no research benches to be found.", MessageTypeDefOf.RejectInput);
                failReason = Reason.Null;
                return false;
            }
            else if (failReason == Reason.NoResearchProject)
            {
                Messages.Message("There are no research projects currently being researched.", MessageTypeDefOf.RejectInput);
                failReason = Reason.Null;
                return false;
            }
            //Cthulhu.Utility.DebugReport(this.ToString() + " Unknown error");
            failReason = Reason.Null;
            return false;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = parms.target as Map;

            //Set up variables
            var researchFinishedValue = ResearchProject().baseCost;
            _ = Find.ResearchManager.GetProgress(ResearchProject());
            var researchAddedProgress = 0f;
            
            researchAddedProgress += (researchFinishedValue + 1) / 2 *99;

            //Cthulhu.Utility.DebugReport("Research Added: " + researchAddedProgress.ToString());

            //Perform some research
            Find.ResearchManager.ResearchPerformed(researchAddedProgress, executioner(map));


            map.GetComponent<MapComponent_SacrificeTracker>().lastLocation = executioner(map).Position;
            Messages.Message("Nyarlathotep grants your colony forbidden knowledge.", MessageTypeDefOf.PositiveEvent);

            return true;
        }
    }
}
