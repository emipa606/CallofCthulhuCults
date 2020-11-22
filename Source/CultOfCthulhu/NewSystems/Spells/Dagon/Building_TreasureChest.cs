using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Diagnostics;
using Verse;

namespace CultOfCthulhu
{
    public class Building_TreasureChest : Building, IOpenable, IThingHolder, IStoreSettingsParent
    {
        protected ThingOwner innerContainer;

        protected StorageSettings storageSettings;

        protected bool contentsKnown;

        protected bool SpawnedStorage = false;

        public bool HasAnyContents => innerContainer.Count > 0;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            //foreach (Gizmo g2 in StorageSettingsClipboard.CopyPasteGizmosFor(this.storageSettings))
            //{
            //    yield return g2;
            //}
        }

        public override void PostMake()
        {
            base.PostMake();
            //this.innerContainer = new ThingOwner<Thing>(this, false);
            storageSettings = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
            {
                storageSettings.CopyFrom(def.building.defaultStorageSettings);
            }
            if (SpawnedStorage == false)
            {
                SpawnedStorage = true;
                if (def == CultsDefOf.Cults_TreasureChest)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        Thing thing1 = ThingMaker.MakeThing(ThingDefOf.Gold, null);
                        thing1.stackCount = Rand.Range(20, 40);
                        GetDirectlyHeldThings().TryAdd(thing1);

                        Thing thing2 = ThingMaker.MakeThing(ThingDefOf.Silver, null);
                        thing2.stackCount = Rand.Range(40, 60);
                        GetDirectlyHeldThings().TryAdd(thing2);

                        Thing thing3 = ThingMaker.MakeThing(ThingDef.Named("Jade"), null);
                        thing3.stackCount = Rand.Range(10, 40);
                        GetDirectlyHeldThings().TryAdd(thing3);
                    }
                    if (Rand.Value > 0.8f)
                    {
                        Thing thing4 = ThingMaker.MakeThing(ThingDef.Named("SculptureSmall"), ThingDefOf.Gold);
                        thing4.stackCount = 1;
                        GetDirectlyHeldThings().TryAdd(thing4);
                    }
                }
                if (def == CultsDefOf.Cults_TreasureChest_Relic)
                {
                    if (Rand.Range(1, 100) > 50)
                    {
                        GetDirectlyHeldThings().TryAdd(GenerateLegendaryWeapon());
                    }
                    else
                    {
                        GetDirectlyHeldThings().TryAdd(GenerateLegendaryArmor());
                    }
                }
            }
        }

        //Selects a random weapon type and improves it to a legendary status
        public ThingWithComps GenerateLegendaryWeapon()
        {
            if (!(from td in DefDatabase<ThingDef>.AllDefs
                  where HandlesWeaponDefs(td)
                  select td).TryRandomElement(out ThingDef def))
            {
                return null;
            }
            var thingWithComps = (ThingWithComps)ThingMaker.MakeThing(def, null);
            CompQuality compQuality = thingWithComps.TryGetComp<CompQuality>();
            compQuality.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);
            return thingWithComps;
        }

        //Industrial Level Legendary Weapons
        public bool HandlesWeaponDefs(ThingDef thingDef)
        {
            return thingDef.IsRangedWeapon && thingDef.tradeability != Tradeability.None && thingDef.techLevel <= TechLevel.Industrial;
        }

        //Same as weapon generation code
        public ThingWithComps GenerateLegendaryArmor()
        {
            if (!(from td in DefDatabase<ThingDef>.AllDefs
                  where HandlesArmorDefs(td)
                  select td).TryRandomElement(out ThingDef def))
            {
                return null;
            }
            var thingWithComps = (ThingWithComps)ThingMaker.MakeThing(def, null);
            thingWithComps.stackCount = 1;
            CompQuality compQuality = thingWithComps.TryGetComp<CompQuality>();
            compQuality.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);
            return thingWithComps;
        }

        //Industrial Level Legendary Armor
        private bool HandlesArmorDefs(ThingDef td)
        {
            return td == ThingDefOf.Apparel_ShieldBelt || (td.tradeability != Tradeability.None && td.techLevel <= TechLevel.Industrial && td.IsApparel && (td.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt, null) > 0.15f || td.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp, null) > 0.15f));
        }

        public Thing ContainedThing => (innerContainer.Count != 0) ? innerContainer[0] : null;

        public bool CanOpen => HasAnyContents;

        public bool StorageTabVisible => false;

        public Building_TreasureChest()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public override void TickRare()
        {
            base.TickRare();
            innerContainer.ThingOwnerTickRare(true);
        }

        public override void Tick()
        {
            base.Tick();
            innerContainer.ThingOwnerTick(true);
        }

        public virtual void Open()
        {
            if (!HasAnyContents)
            {
                return;
            }
            EjectContents();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingOwner>(ref innerContainer, "innerContainer", new object[]
            {
                this
            });
            Scribe_Values.Look<bool>(ref contentsKnown, "contentsKnown", false, false);
            Scribe_Deep.Look<StorageSettings>(ref storageSettings, "storageSettings", new object[]
                {
                    this
                });
            Scribe_Values.Look<bool>(ref SpawnedStorage, "SpawnedStorage", false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (Faction != null && Faction.IsPlayer)
            {
                contentsKnown = true;
            }
        }

        public override bool ClaimableBy(Faction fac)
        {
            if (innerContainer.Any)
            {
                for (var i = 0; i < innerContainer.Count; i++)
                {
                    if (innerContainer[i].Faction == fac)
                    {
                        return true;
                    }
                }
                return false;
            }
            return base.ClaimableBy(fac);
        }

        public virtual bool Accepts(Thing thing)
        {
            return innerContainer.Count < 10 && innerContainer.CanAcceptAnyOf(thing, true);
        }

        public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!Accepts(thing))
            {
                return false;
            }
            bool flag;
            if (thing.holdingOwner != null)
            {
                thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount, true);
                flag = true;
            }
            else
            {
                flag = innerContainer.TryAdd(thing, true);
            }
            if (flag)
            {
                if (thing.Faction != null && thing.Faction.IsPlayer)
                {
                    contentsKnown = true;
                }
                return true;
            }
            return false;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
            {
                if (mode != DestroyMode.Deconstruct)
                {
                    var list = new List<Pawn>();
                    foreach (Thing current in innerContainer)
                    {
                        if (current is Pawn pawn)
                        {
                            list.Add(pawn);
                        }
                    }
                    foreach (Pawn current2 in list)
                    {
                        HealthUtility.DamageUntilDowned(current2);
                    }
                }
                EjectContents();
            }
            innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            base.Destroy(mode);
        }

        public virtual void EjectContents()
        {
            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
            contentsKnown = true;
        }

        public override string GetInspectString()
        {
            var text = base.GetInspectString();
            string str;
            if (!contentsKnown)
            {
                str = "UnknownLower".Translate();
            }
            else
            {
                str = innerContainer.ContentsString;
            }
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            return text + "CasketContains".Translate() + ": " + str;
        }

        //public virtual IThingHolder ParentHolder
        //{
        //    get
        //    {
        //        return base.ParentHolder;
        //    }
        //}

        public StorageSettings GetStoreSettings()
        {
            return storageSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
        }
    }
}
