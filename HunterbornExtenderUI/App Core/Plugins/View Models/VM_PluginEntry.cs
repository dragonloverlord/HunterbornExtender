﻿using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using Noggog.WPF;
using System.Windows.Input;
using ReactiveUI;
using static System.Windows.Forms.AxHost;
using System;

namespace HunterbornExtenderUI;

public class VM_PluginEntry : ViewModel
{
    private StateProvider _state;
    public VM_PluginEntry(StateProvider state)
    {
        _state = state;
        LinkCache = state.LinkCache;

        AddMat = ReactiveCommand.Create(
            () => Mats.Add(new(_state.LinkCache, this)));
    }

    [Reactive]
    public EntryType Type { get; set; } = EntryType.Monster;
    [Reactive]
    public string Name { get; set; } = "";
    [Reactive]
    public string ProperName { get; set; } = "";
    [Reactive]
    public string SortName { get; set; } = "";
    [Reactive]
    public FormKey AnimalSwitch { get; set; }
    [Reactive]
    public FormKey CarcassMessageBox { get; set; }
    [Reactive]
    public int CarcassSize { get; set; } = 1;
    [Reactive]
    public int CarcassWeight { get; set; } = 1;
    [Reactive]
    public int CarcassValue { get; set; } = 1;
    [Reactive]
    public string[] PeltCount { get; set; } = new string[4] { "1", "1", "1", "1" };
    [Reactive]
    public string[] FurPlateCount { get; set; } = new string[4] { "1", "1", "1", "1" };
    [Reactive]
    public FormKey Meat { get; set; }
    [Reactive]
    public ObservableCollection<VM_MatEntry> Mats { get; set; } = new();
    [Reactive]
    public ObservableCollection<FormKey> NegativeTreasure { get; set; } = new();
    [Reactive]
    public FormKey SharedDeathItems { get; set; }
    [Reactive]
    public FormKey BloodType { get; set; }
    [Reactive]
    public FormKey Venom { get; set; }
    [Reactive]
    public FormKey Voice { get; set; }
    [Reactive]
    public string FilePath { get; set; } = "";
    public ILinkCache LinkCache { get; set; }
    public IEnumerable<Type> AnimalSwitchType { get; } = typeof(IGlobalGetter).AsEnumerable();
    public IEnumerable<Type> CarcassMessageBoxType { get; } = typeof(IMessageGetter).AsEnumerable();
    public IEnumerable<Type> MeatType { get; } = typeof(IIngestibleGetter).AsEnumerable();
    public IEnumerable<Type> NegativeTreasureType { get; } = typeof(IIngestibleGetter).AsEnumerable();
    public IEnumerable<Type> SharedDeathItemsType { get; } = typeof(ILeveledItem).AsEnumerable();
    public IEnumerable<Type> BloodTypeType { get; } = typeof(IIngestibleGetter).AsEnumerable();
    public IEnumerable<Type> VenomType { get; } = typeof(IIngestibleGetter).AsEnumerable();
    public IEnumerable<Type> VoiceType { get; } = typeof(IVoiceTypeGetter).AsEnumerable();

    public ICommand AddMat { get; }

    public void LoadFromModel(PluginEntry model)
    {
        Type = model.Type;
        Name = model.Name;
        ProperName = model.ProperName;
        SortName = model.SortName;
        AnimalSwitch = model.AnimalSwitch;
        CarcassMessageBox = model.CarcassMessageBox;
        CarcassSize = model.CarcassSize;
        CarcassValue = model.CarcassValue;
        PeltCount = model.PeltCount;
        FurPlateCount = model.FurPlateCount;
        Meat = model.Meat;
        foreach (var dict in model.Mats)
        {
            var matEntry = new VM_MatEntry(LinkCache, this);
            matEntry.GetFromModel(dict);
            Mats.Add(matEntry);
        }
        NegativeTreasure = new ObservableCollection<FormKey>(model.NegativeTreasure);
        SharedDeathItems = model.SharedDeathItems;
        BloodType = model.BloodType;
        Venom = model.Venom;
        Voice = model.Voice;
    }

    public PluginEntry DumpToModel()
    {
        var model = new PluginEntry();
        model.Type = Type;
        model.Name = Name;
        model.ProperName = ProperName;
        model.SortName = SortName;
        model.AnimalSwitch = AnimalSwitch;
        model.CarcassMessageBox = CarcassMessageBox;
        model.CarcassSize = CarcassSize;
        model.CarcassValue = CarcassValue;
        model.PeltCount = PeltCount;
        model.FurPlateCount = FurPlateCount;
        model.Meat = Meat;
        foreach (var entry in Mats)
        {
            model.Mats.Add(entry.DumpToModel());
        }
        model.NegativeTreasure = NegativeTreasure.ToList();
        model.SharedDeathItems = SharedDeathItems;
        model.BloodType = BloodType;
        model.Venom = Venom;
        model.Voice = Voice;;
        return model;
    }
}

public class VM_MatEntry : ViewModel
{
    [Reactive]
    public ObservableCollection<VM_Mat> Items { get; set; } = new();
    public ILinkCache LinkCache { get; set; }
    public ICommand AddItem { get; }
    public ICommand DeleteMe { get; }
    public VM_PluginEntry Parent { get; set; }

    public VM_MatEntry(ILinkCache linkCache, VM_PluginEntry parent)
    {
        LinkCache = linkCache;
        Parent = parent;
        AddItem = ReactiveCommand.Create(
            () => Items.Add(new(linkCache, this)));
        DeleteMe = ReactiveCommand.Create(
            () => Parent.Mats.Remove(this));
    }

    public void GetFromModel(Dictionary<FormKey, int> model)
    {
        foreach (var key in model.Keys)
        {
            VM_Mat mat = new(LinkCache, this);
            mat.Key = key;
            mat.Value = model[key];
            Items.Add(mat);
        }
    }

    public Dictionary<FormKey, int> DumpToModel()
    {
        Dictionary<FormKey, int> model = new();
        foreach (var item in Items)
        {
            if (!model.ContainsKey(item.Key))
            {
                model.Add(item.Key, item.Value);
            }
        }
        return model;
    }
}

public class VM_Mat : ViewModel
{
    public ILinkCache LinkCache { get; set; }
    public IEnumerable<Type> MatType { get; } = typeof(IMiscItemGetter).AsEnumerable().And(typeof(IIngredientGetter));
    public VM_MatEntry Parent { get; set; }
    public ICommand DeleteMe { get; }

    public VM_Mat(ILinkCache linkCache, VM_MatEntry parent)
    {
        LinkCache = linkCache;
        Parent = parent;

        DeleteMe = ReactiveCommand.Create(
            () => Parent.Items.Remove(this));
    }

    //[Reactive]
    public FormKey Key { get; set; } = new();
    [Reactive]
    public int Value { get; set; } = 1;

}
