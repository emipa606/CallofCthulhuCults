using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CultOfCthulhu
{
    internal class ThingWithComps_CultGrimoire : ThingWithComps
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            var buildable = CultsDefOf.Cults_ForbiddenKnowledgeCenter; //ThingDef.Named("ForbiddenKnowledgeCenter");
            var des = FindDesignator(buildable);
            var stuff = ThingDefOf.WoodLog;
            if (des == null)
            {
                yield break;
            }

            if (!des.Visible)
            {
                yield break;
            }

            var command_Action = new Command_Action
            {
                action = delegate
                {
                    SoundDefOf.ThingSelected.PlayOneShotOnCamera();
                    //des.SetStuffDef(stuff);
                    des.ProcessInput(new Event());
                    Find.DesignatorManager.Select(des);
                },
                defaultLabel = "CommandBuildFKC".Translate(),
                defaultDesc = "CommandBuildFKCDesc".Translate(),
                icon = des.icon,
                iconProportions = des.iconProportions,
                iconDrawScale = des.iconDrawScale,
                iconTexCoords = des.iconTexCoords
            };
            if (stuff != null)
            {
                command_Action.defaultIconColor = stuff.stuffProps.color;
            }
            else
            {
                command_Action.defaultIconColor = buildable.uiIconColor;
            }

            command_Action.hotKey = KeyBindingDefOf.Misc11;
            yield return command_Action;
        }


        private static Designator_Build FindDesignator(BuildableDef buildable)
        {
            var allDefsListForReading = DefDatabase<DesignationCategoryDef>.AllDefsListForReading;
            for (var i = 0; i < allDefsListForReading.Count; i++)
            {
                foreach (var current in allDefsListForReading[i].ResolvedAllowedDesignators)
                {
                    if (current is Designator_Build designator_Build && designator_Build.PlacingDef == buildable)
                    {
                        return designator_Build;
                    }
                }
            }

            return null;
        }
    }
}