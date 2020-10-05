using System;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace CultOfCthulhu
{
    public class Dialog_RenameTemple : Dialog_Rename
    {
        private readonly Building_SacrificialAltar altar;

        private readonly Map map;

        public Dialog_RenameTemple(Building_SacrificialAltar altar)
        {
            this.altar = altar;
            curName = altar.RoomName;
            map = altar.Map;
        }
        
        protected override AcceptanceReport NameIsValid(string name)
        {
            AcceptanceReport result = base.NameIsValid(name);
            if (!result.Accepted)
            {
                return result;
            }
            return name.Length == 0 || name.Length > 27 ? "NameIsInvalid".Translate() : (AcceptanceReport)true;
        }

        protected override void SetName(string name)
        {
            altar.RoomName = name;
        }
    }
}
