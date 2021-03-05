using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CultOfCthulhu
{
    public class Plant_TreeOfMadness : Plant
    {
        private static readonly FloatRange QuietIntervalDays = new FloatRange(1.5f, 2.5f);
        private bool isMuted;
        public bool isQuiet;
        private bool setup;
        private Sustainer sustainerAmbient;
        private int ticksUntilQuiet = 960;

        public override void SpawnSetup(Map map, bool bla)
        {
            ticksUntilQuiet += (int) (QuietIntervalDays.RandomInRange * 60000f);
            base.SpawnSetup(map, bla);
        }

        public override void Tick()
        {
            base.Tick();
            Setup();
            DoTickWork();
        }

        public void Setup()
        {
            if (!setup)
            {
                setup = true;
                if (!def.building.soundAmbient.NullOrUndefined() && sustainerAmbient == null)
                {
                    var info = SoundInfo.InMap(this);
                    sustainerAmbient = def.building.soundAmbient.TrySpawnSustainer(info);
                }
            }
        }

        public void DoTickWork()
        {
            if (isQuiet)
            {
                return;
            }

            ticksUntilQuiet--;
            if (ticksUntilQuiet <= 0)
            {
                isQuiet = true;
                sustainerAmbient.End();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            if (sustainerAmbient != null)
            {
                sustainerAmbient.End();
            }
        }


        public Thought_Memory GiveObservedThought()
        {
            if (this.StoringThing() == null)
            {
                Thought_MemoryObservation thought_MemoryObservation;
                thought_MemoryObservation =
                    (Thought_MemoryObservation) ThoughtMaker.MakeThought(
                        DefDatabase<ThoughtDef>.GetNamed("Cults_ObservedNightmareTree"));
                thought_MemoryObservation.Target = this;
                var Dave = thought_MemoryObservation.pawn;
                if (Dave == null)
                {
                    return null;
                }

                if (!Dave.IsColonist)
                {
                    return thought_MemoryObservation;
                }

                if (Dave.needs.TryGetNeed<Need_CultMindedness>().CurLevel > 0.7)
                {
                    thought_MemoryObservation =
                        (Thought_MemoryObservation) ThoughtMaker.MakeThought(
                            DefDatabase<ThoughtDef>.GetNamed("Cults_ObservedNightmareTreeCultist"));
                }

                return thought_MemoryObservation;
            }

            return null;
        }


        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            ///This code returns all the other float menu options first!
            var enumerator = base.GetFloatMenuOptions(myPawn).GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                yield return current;
            }

            if (CultUtility.AreCultObjectsAvailable(Map) == false)
            {
                if (CultUtility.IsSomeoneInvestigating(Map) == false)
                {
                    void action0()
                    {
                        var job = new Job(CultsDefOf.Cults_Investigate, myPawn, this)
                        {
                            playerForced = true
                        };
                        myPawn.jobs.TryTakeOrderedJob(job);
                        //mypawn.CurJob.EndCurrentJob(JobCondition.InterruptForced);
                    }

                    yield return new FloatMenuOption("Cults_Investigate".Translate(), action0);
                }
            }
        }


        public void MuteToggle()
        {
            isMuted = !isMuted;
            if (sustainerAmbient != null && isMuted)
            {
                sustainerAmbient.End();
            }
            else if (!def.building.soundAmbient.NullOrUndefined() && sustainerAmbient == null)
            {
                var info = SoundInfo.InMap(this);
                sustainerAmbient = new Sustainer(def.building.soundAmbient, info);
            }
            else
            {
                Log.Warning("Cults :: Mute toggle threw an exception on the eerie tree.");
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            var enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                yield return current;
            }

            var toggleDef = new Command_Toggle
            {
                hotKey = KeyBindingDefOf.Command_TogglePower,
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Commands/Mute"),
                defaultLabel = "Mute".Translate(),
                defaultDesc = "MuteDesc".Translate(),
                isActive = () => isMuted,
                toggleAction = delegate { MuteToggle(); }
            };
            yield return toggleDef;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // Save and load the work variables, so they don't default after loading
            //Scribe_Values.Look<bool>(ref isMuted, "isMuted", false);
        }
    }
}