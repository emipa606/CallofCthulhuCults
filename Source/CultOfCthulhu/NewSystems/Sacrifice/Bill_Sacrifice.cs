using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CultOfCthulhu
{
    public class Bill_Sacrifice : IExposable
    {
        private Pawn sacrifice;
        private Pawn executioner;
        private List<Pawn> congregation;
        private CosmicEntity entity;
        private IncidentDef spell;

        public Pawn Sacrifice => sacrifice;
        public Pawn Executioner => executioner;
        public List<Pawn> Congregation { get => congregation; set => congregation = value; }
        public CosmicEntity Entity => entity;
        public IncidentDef Spell => spell;
        public CultUtility.SacrificeType Type => (Sacrifice?.RaceProps?.Animal ?? false) ? CultUtility.SacrificeType.animal : CultUtility.SacrificeType.human;

        public Bill_Sacrifice()
        {

        }

        public Bill_Sacrifice(Pawn newSacrifice, Pawn newExecutioner, CosmicEntity newEntity, IncidentDef newSpell)
        {
            sacrifice = newSacrifice;
            executioner = newExecutioner;
            entity = newEntity;
            spell = newSpell;
        }

        public void ExposeData()
        {
            Scribe_References.Look<Pawn>(ref sacrifice, "sacrifice");
            Scribe_References.Look<Pawn>(ref executioner, "executioner");
            Scribe_Collections.Look<Pawn>(ref congregation, "congregation", LookMode.Reference);
            Scribe_References.Look<CosmicEntity>(ref entity, "entity");
            Scribe_Defs.Look<IncidentDef>(ref spell, "spell");
        }
    }
}
