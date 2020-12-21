using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CultOfCthulhu
{
    public class CompFavoredObject : ThingComp
    {
        public List<FavoredEntry> Deities => Props.deities;

        public CompProperties_FavoredObject Props => (CompProperties_FavoredObject)props;
    }
}
