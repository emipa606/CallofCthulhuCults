using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CultOfCthulhu
{
    public class CosmicEntityDef : ThingDef
    {
        private readonly string symbol;
        private readonly string version = "0";
        public List<FavoredThing> displeasingOfferings = new List<FavoredThing>();
        public readonly List<ThingDef> favoredApparel = new List<ThingDef>();
        public List<FavoredThing> favoredWorshipperRaces = new List<FavoredThing>();

        public readonly bool favorsOutdoorWorship = false;
        public IncidentDef finalSpellDef;
        public List<FavoredThing> hereticWorshipperRaces = new List<FavoredThing>();
        public List<FavoredThing> pleasingOfferings = new List<FavoredThing>();

        [Unsaved] private Texture2D symbolTex;

        public readonly List<IncidentDef> tier1SpellDefs = new List<IncidentDef>();
        public readonly List<IncidentDef> tier2SpellDefs = new List<IncidentDef>();
        public readonly List<IncidentDef> tier3SpellDefs = new List<IncidentDef>();

        public string Portrait { get; } = string.Empty;

        public string Domains { get; } = string.Empty;

        public string DescriptionLong { get; } = string.Empty;

        public string Titles { get; } = string.Empty;

        public Texture2D Symbol
        {
            get
            {
                if (symbolTex == null)
                {
                    symbolTex = ContentFinder<Texture2D>.Get(symbol);
                }

                return symbolTex;
            }
        }

        public int Version => int.TryParse(version, out var x) ? x : 0;
#pragma warning disable IDE0032 // Use auto property
#pragma warning restore IDE0032 // Use auto property
    }
}