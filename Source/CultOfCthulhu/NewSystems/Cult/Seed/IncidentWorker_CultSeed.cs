using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CultOfCthulhu
{
    class IncidentWorker_CultSeed : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            var map = (Map)parms.target;
            MapComponent_LocalCultTracker tracker = GetTracker(map);
            return tracker.CurrentSeedState <= CultSeedState.NeedSeed;
        }

        private MapComponent_LocalCultTracker GetTracker(Map map)
        {
            MapComponent_LocalCultTracker result = map.GetComponent<MapComponent_LocalCultTracker>();
            if (map.GetComponent<MapComponent_LocalCultTracker>() == null)
            {
                result = new MapComponent_LocalCultTracker(map);
                map.components.Add(result);
            }
            return result;
        }
    }
}
