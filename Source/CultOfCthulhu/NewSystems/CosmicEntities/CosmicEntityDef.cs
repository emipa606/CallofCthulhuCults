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
        private readonly string portrait = "";
        private readonly string titles = "";
        private readonly string domains = "";
        private readonly string descriptionLong = "";

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

        public string Portrait => portrait;
        public string Domains => domains;
        public string DescriptionLong => descriptionLong;
        public string Titles => titles;

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

        public int Version
        {
            get
            {
                return Int32.TryParse(version, out int x) ? x : 0;
            }
        }
    }
}
