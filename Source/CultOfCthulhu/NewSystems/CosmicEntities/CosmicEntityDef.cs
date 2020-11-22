using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;

namespace CultOfCthulhu
{
    public class CosmicEntityDef : ThingDef
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private readonly string symbol;
#pragma warning restore IDE0044 // Add readonly modifier
        private readonly string version = "0";
        public List<IncidentDef> tier1SpellDefs = new List<IncidentDef>();
        public List<IncidentDef> tier2SpellDefs = new List<IncidentDef>();
        public List<IncidentDef> tier3SpellDefs = new List<IncidentDef>();
        public IncidentDef finalSpellDef;
        public List<ThingDef> favoredApparel = new List<ThingDef>();
        public List<FavoredThing> pleasingOfferings = new List<FavoredThing>();
        public List<FavoredThing> displeasingOfferings = new List<FavoredThing>();
        public List<FavoredThing> favoredWorshipperRaces = new List<FavoredThing>();
        public List<FavoredThing> hereticWorshipperRaces = new List<FavoredThing>();

        public bool favorsOutdoorWorship = false;

        [Unsaved]
        private Texture2D symbolTex;

        public string Portrait { get; } = "";
        public string Domains { get; } = "";
        public string DescriptionLong { get; } = "";
        public string Titles { get; } = "";

        public Texture2D Symbol
        {
            get
            {
                if (symbolTex == null)
                {
                    symbolTex = ContentFinder<Texture2D>.Get(symbol, true);
                }
                return symbolTex;
            }
        }

        public int Version => int.TryParse(version, out var x) ? x : 0;
    }
}
