using System;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace CultOfCthulhu
{
    public class Dialog_RenameCult : Dialog_Rename
    {

        private readonly Map map;

        public Dialog_RenameCult(Map newMap)
        {
            map = newMap;
            if (map != null)
            {
                curName = CultTracker.Get.PlayerCult.name;
            }
            else
            {
                Cthulhu.Utility.ErrorReport("Missing map to declare as home area");
            }
        }

        protected override AcceptanceReport NameIsValid(string name)
        {
            AcceptanceReport result = base.NameIsValid(name);
            if (!result.Accepted)
            {
                return result;
            }
            return name.Length == 0 || (!CultUtility.CheckValidCultName(name)) ? "NameIsInvalid".Translate() : (AcceptanceReport)true;
        }

        protected override void SetName(string name)
        {
            if (map != null)
            {
                CultTracker.Get.PlayerCult.name = name;
            }
            else
            {
                Cthulhu.Utility.ErrorReport("Map Reference Null Exception");
            }
        }
    }
}
