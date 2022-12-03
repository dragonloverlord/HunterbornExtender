﻿using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HunterbornExtender.Settings
{

    abstract public class PluginEntry
    {
        public EntryType Type { get; set; } = EntryType.Animal;
        public string Name { get; set; } = "Critter";
        public string ProperName { get; set; } = "Critter";
        public string SortName { get; set; } = "Critter";
        public IFormLinkGetter<IGlobalGetter> Toggle { get; set; } = new FormLink<IGlobalGetter>();
        public IFormLinkGetter<IMessageGetter> CarcassMessageBox { get; set; } = new FormLink<IMessageGetter>();
        public IFormLinkGetter<IItemGetter> Meat { get; set; } = new FormLink<IItemGetter>();
        public int CarcassSize { get; set; } = 1;
        public int CarcassWeight { get; set; } = 10;
        public int CarcassValue { get; set; } = 10;
        public int[] PeltCount { get; set; } = new int[] { 1, 1, 1, 1 };
        public int[] FurPlateCount { get; set; } = new int[] { 1, 1, 1, 1 };
        public List<Dictionary<IFormLinkGetter<IItemGetter>, int>> Materials { get; set; } = new();
        public List<IFormLinkGetter<IItemGetter>> Discard { get; set; } = new();
        public IFormLinkGetter<IFormListGetter> SharedDeathItems { get; set; } = new FormLink<IFormListGetter>();
        public IFormLinkGetter<IItemGetter> BloodType { get; set; } = new FormLink<IItemGetter>();
        public IFormLinkGetter<IItemGetter> Venom { get; set; } = new FormLink<IItemGetter>();
        public IFormLinkGetter<IVoiceTypeGetter> Voice { get; set; } = new FormLink<IVoiceTypeGetter>();

        public PluginEntry() // Json import appears to require a parameterless default constructor
        {

        }

        public PluginEntry(EntryType type, string name)
        {
            Type = type;
            Name = name;
            ProperName = name;
            SortName = name;
        }

        /*public PluginEntry(EntryType type, string name, string properName, string sortName, IFormLinkGetter<IGlobalGetter> toggle, IFormLinkGetter<IMessageGetter> carcassMessageBox, IFormLinkGetter<IItemGetter> meat, int carcassSize, int carcassWeight, int carcassValue, int[] peltCount, int[] furPlateCount, List<Dictionary<IFormLinkGetter<IItemGetter>, int>> materials, List<IFormLinkGetter<IItemGetter>> discard, IFormLinkGetter<IFormListGetter> sharedDeathItems, IFormLinkGetter<IItemGetter> bloodType, IFormLinkGetter<IItemGetter> venom, IFormLinkGetter<IVoiceTypeGetter> voice)
        {
            Type = type;
            Name = name;
            ProperName = properName;
            SortName = sortName;
            Toggle = toggle;
            CarcassMessageBox = carcassMessageBox;
            Meat = meat;
            CarcassSize = carcassSize;
            CarcassWeight = carcassWeight;
            CarcassValue = carcassValue;
            PeltCount = peltCount;
            FurPlateCount = furPlateCount;
            Materials = materials;
            Discard = discard;
            SharedDeathItems = sharedDeathItems;
            BloodType = bloodType;
            Venom = venom;
            Voice = voice;
        }*/
    }

    /// <summary>
    /// Used to describe plugins that get loaded from JSon files.
    /// They each have a name and can have required mods.
    /// 
    /// @TODO Add required mods to the JSon files. This will reduce unresolved record issues.
    /// For now it's fine if it's empty and unused.
    /// 
    /// </summary>
    sealed public class AddonPluginEntry : PluginEntry
    {
        //public ModKey[] RequiredMods { get; set; } = Array.Empty<ModKey>();

        public AddonPluginEntry() { }

        public AddonPluginEntry(EntryType type, string name) : base(type, name) { }
    }


    /// <summary>
    /// Used to describe the hard-coded plugins from Hunterborn.esp.
    /// They each have a KnownDeathItem used as a prototype.
    /// </summary>
    sealed public class InternalPluginEntry : PluginEntry
    {
        public FormKey KnownDeathItem { get; set; } = new();

        public InternalPluginEntry() { }

        public InternalPluginEntry(EntryType type, string name, FormKey deathItem) : base(type, name) { 
            KnownDeathItem = deathItem;
        }

    }
}
