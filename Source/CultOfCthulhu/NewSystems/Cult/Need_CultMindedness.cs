using System;
using System.Collections.Generic;

using RimWorld;
using UnityEngine;
using Verse;

namespace CultOfCthulhu
{

    public class Need_CultMindedness : RimWorld.Need
    {

        //public static ThingDef ColanderThingDef;
        
        public const float BaseGainPerTickRate = 150f;
        public const float BaseFallPerTick = 1E-05f;
        public const float ThreshVeryLow = 0.1f;
        public const float ThreshLow = 0.3f;
        public const float ThreshSatisfied = 0.5f;
        public const float ThreshHigh = 0.7f;
        public const float ThreshVeryHigh = 0.9f;

        private bool baseSet = false;
        public int ticksUntilBaseSet = 500;
        private int lastGainTick;
        readonly WorldComponent_GlobalCultTracker globalCultTracker = Find.World.GetComponent<WorldComponent_GlobalCultTracker>();

        static Need_CultMindedness()
        {
            //ColanderThingDef = 
            //<ThingDef>.GetNamed("Apparel_Colander");
        }
        
        public override int GUIChangeArrow
        {
            get
            {
                return GainingNeed ? 1 : -1;
            }
        }

        public override float CurInstantLevel
        {
            get
            {
                return CurLevel;
            }
        }

        private bool GainingNeed
        {
            get
            {
                return Find.TickManager.TicksGame < lastGainTick + 10;
            }
        }

        public Need_CultMindedness(Pawn pawn) : base(pawn)
        {
            lastGainTick = -999;
            threshPercents = new List<float>
            {
                ThreshLow,
                ThreshHigh
            };
        }

        public override void SetInitialLevel()
        {
            CurLevel = ThreshSatisfied;
        }
        

        public void GainNeed(float amount)
        {
            amount /= 120f;
            amount *= 0.01f;
            amount = Mathf.Min(amount, 1f - CurLevel);
            curLevelInt += amount;
            lastGainTick = Find.TickManager.TicksGame;
        }

        public override void NeedInterval()
        {
            ////Log.Messag("Need Interval");
            if (!CultTracker.Get.ExposedToCults) return;
            if (pawn == null) return;
            if (!pawn.IsPrisonerOfColony && !pawn.IsColonist) return;
            if (!baseSet)
            {
                if (ticksUntilBaseSet <= 0) SetBaseLevels();
                ticksUntilBaseSet -= 150;
                return;
            }
            if (CultTracker.Get.PlayerCult != null)
            {
                if (CultTracker.Get.PlayerCult.founder == pawn ||
                    CultTracker.Get.PlayerCult.leader == pawn) return;
            }
            curLevelInt -= 0.00005f;
            if (curLevelInt <= 0) curLevelInt = 0;
        }

        public void SetBaseLevels()
        {
            baseSet = true;
            float temp = CurLevel;
            if (pawn == null) return;
            temp += CultUtility.GetBaseCultistModifier(pawn);
            if (temp > 0.99f) temp = 0.99f;
            if (temp < 0.01f) temp = 0.01f;

            if (pawn?.Faction?.def?.defName == "ROM_TheAgency")
            {
                Cthulhu.Utility.DebugReport(pawn.Name.ToStringFull + " is a member of the agency. Cult levels set to 1%.");
                temp = 0.01f;
            }
            CurLevel = temp;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref baseSet, "baseSet", false, false);
            Scribe_Values.Look<int>(ref ticksUntilBaseSet, "ticksUntilBaseSet", 1000, false);
        }


        public override string GetTipString()
        {
            return base.GetTipString();
        }


        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = int.MaxValue, float customMargin = -1F, bool drawArrows = true, bool doTooltip = true)
        {
            if (CultTracker.Get.ExposedToCults)
            {
                //base.DrawOnGUI(rect, maxThresholdMarkers, customMargin, drawArrows, doTooltip);
                if (rect.height > 70f)
                {
                    float num = (rect.height - 70f) / 2f;
                    rect.height = 70f;
                    rect.y += num;
                }
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                }
                TooltipHandler.TipRegion(rect, new TipSignal(() => GetTipString(), rect.GetHashCode()));
                float num2 = 14f;
                float num3 = num2 + 15f;
                if (rect.height < 50f)
                {
                    num2 *= Mathf.InverseLerp(0f, 50f, rect.height);
                }
                Text.Font = (rect.height <= 55f) ? GameFont.Tiny : GameFont.Small;
                Text.Anchor = TextAnchor.LowerLeft;
                Rect rect2 = new Rect(rect.x + num3 + rect.width * 0.1f, rect.y, rect.width - num3 - rect.width * 0.1f, rect.height / 2f);
                Widgets.Label(rect2, LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                Rect rect3 = new Rect(rect.x, rect.y + rect.height / 2f, rect.width, rect.height / 2f);
                rect3 = new Rect(rect3.x + num3, rect3.y, rect3.width - num3 * 2f, rect3.height - num2);
                Widgets.FillableBar(rect3, CurLevelPercentage, Buttons.RedTex);
                //else Widgets.FillableBar(rect3, this.CurLevelPercentage);
                //Widgets.FillableBarChangeArrows(rect3, this.GUIChangeArrow);
                if (threshPercents != null)
                {
                    for (int i = 0; i < threshPercents.Count; i++)
                    {
                        DrawBarThreshold(rect3, threshPercents[i]);
                    }
                }
                float curInstantLevelPercentage = CurInstantLevelPercentage;
                if (curInstantLevelPercentage >= 0f)
                {
                    DrawBarInstantMarkerAt(rect3, curInstantLevelPercentage);
                }
                if (!def.tutorHighlightTag.NullOrEmpty())
                {
                    UIHighlighter.HighlightOpportunity(rect, def.tutorHighlightTag);
                }
                Text.Font = GameFont.Small;
            }
        }

        private void DrawBarThreshold(Rect barRect, float threshPct)
        {
            float num = (float)((barRect.width <= 60f) ? 1 : 2);
            Rect position = new Rect(barRect.x + barRect.width * threshPct - (num - 1f), barRect.y + barRect.height / 2f, num, barRect.height / 2f);
            Texture2D image;
            if (threshPct < CurLevelPercentage)
            {
                image = BaseContent.BlackTex;
                GUI.color = new Color(1f, 1f, 1f, 0.9f);
            }
            else
            {
                image = BaseContent.GreyTex;
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
            }
            GUI.DrawTexture(position, image);
            GUI.color = Color.white;
        }

    }

}
