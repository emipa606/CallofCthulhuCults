using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace CultOfCthulhu
{
    public class Building_SignOfDagon : Building
    {
        public override void SpawnSetup(Map map, bool bla)
        {
            //Log.Message("Building_SignOfDagon SpawnSetup");
            base.SpawnSetup(map, bla);
            Building_SignOfDagon toDestroy = null;
            foreach (Building bld in map.listerBuildings.allBuildingsColonist)
            {
                if (bld == this) continue;
                if (bld is Building_SignOfDagon dagon) toDestroy = dagon;
            }
            if (toDestroy != null) 
                toDestroy.Destroy(0);

            List<Pawn> list = map.GetComponent<MapComponent_SacrificeTracker>().defendTheBroodPawns;
            if (list == null)
            {
                return;
            }
            if (list.Count <= 0)
            {
                return;
            }
            Faction f;
            if (Cthulhu.Utility.IsCosmicHorrorsLoaded())
            {
                f = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("ROM_DeepOne"));
            }
            else
            {
                Messages.Message("Cosmic horrors mod is not loaded. Using insectoids instead.", MessageTypeDefOf.NegativeEvent);
                f = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("ROM_DeepOneAlt"));
            }

            Lord lord = null;
            //Log.Message("Building_SignOfDagon LordJob_DefendPoint");
            var lordJob = new LordJob_DefendPoint(Position);
            Cthulhu.Utility.TemporaryGoodwill(f, false);
            foreach (Pawn current in list)
            {
                if (lord == null)
                    lord = current.GetLord();
                if (lord != null)
                {
                    map.lordManager.RemoveLord(lord);
                }
            }
            LordMaker.MakeNewLord(f, lordJob, map, list);
        }
    }
}
