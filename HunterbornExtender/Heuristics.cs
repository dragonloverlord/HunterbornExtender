﻿namespace HunterbornExtender;
using HunterbornExtender.Settings;
using Microsoft.CodeAnalysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System;
using System.Collections.Generic;
using System.Linq;
using DeathItemGetter = Mutagen.Bethesda.Skyrim.ILeveledItemGetter;

sealed public class Heuristics
{
    /// <summary>
    /// Scans a list of NPCs and tries to assign a PluginEntry to each DeathItem that belongs to a creature.
    /// </summary>
    /// 
    /// <param name="plugins">The list of plugins to match with DeathItems.</param>
    /// <param name="npcs">The npcs to process.</param>
    /// <param name="previousSelections">The previous selections, so that user choices can persist from run to run.</param>
    /// <returns></returns>
    /// <exception cref="HeuristicsError">Indicates that something went wrong during recreation. Using the InnerException field to retrieve the cause.</exception>
    /// 
    static public DeathItemSelection[] MakeHeuristicSelections(IEnumerable<INpcGetter> npcs, List<PluginEntry> plugins, DeathItemSelection[] previousSelections, 
        ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, bool debuggingMode = false)
    {
        try
        {
            // 
            // Import allowed and forbidden values from plugins.
            //
            foreach (var plugin in plugins)
            {
                if (!plugin.Voice.IsNull) SpecialCases.Lists.AllowedVoices.Add(plugin.Voice);
            }

            // For each DeathItem, there will be a weighted set of plausible Plugins.
            // HeuristicMatcher assigns the weights.
            Dictionary<DeathItemSelection, Dictionary<PluginEntry, int>> selectionWeights = new();
            Dictionary<DeathItemGetter, DeathItemSelection> indexer = new();

            // Tokenize the names of the plugins.
            foreach (var plugin in plugins) plugin.Tokens = Tokenizer.Tokenize(plugin.Name, plugin.SortName, plugin.ProperName);
            if (debuggingMode)
            {
                Write.Title(1, "Tokenizing plugin names.");
                plugins.ForEach(p => Write.Action(2, $"Plugin: {p.Name} -> {p.Tokens.Pretty()}"));
                Write.Title(1, "Analyzing NPCs.");
            }

            // Scan the list of npcs.
            foreach (var npc in npcs.Where(n => IsCreature(n, debuggingMode)))
            {
                //if (settings.DebuggingMode) Write.Action(2, $"Heuristics examining {npc}");
                if (npc.DeathItem?.IsNull ?? true) continue;

                var deathItem = npc.DeathItem.Resolve(linkCache);
                //if (KnownDeathItems.ContainsKey(deathItem)) continue;

                // If there is no DeathItemSelection record for the NPC's DeathItem, create it.
                // Try as hard as possible to give the DeathItemSelection a internalName. Fallbacks on fallbacks.
                if (!indexer.ContainsKey(deathItem))
                {
                    indexer[deathItem] = new DeathItemSelection() { DeathItem = deathItem.FormKey, CreatureEntryName = DeathItemNamer(deathItem) };
                    selectionWeights[indexer[deathItem]] = new();
                }

                // Add the NPC to the assigned NPCs of the DeathItemSelection.
                var deathItemSelection = indexer[deathItem];
                deathItemSelection.AssignedNPCs.Add(npc);

                // Run the heuristic matcher.
                var npcWeights = HeuristicNpcMatcher(npc, plugins, linkCache, debuggingMode);
                var itemWeights = selectionWeights[deathItemSelection];

                foreach (PluginEntry plugin in npcWeights.Keys)
                    itemWeights[plugin] = itemWeights.GetValueOrDefault(plugin, 0) + npcWeights[plugin];
            }

            DeathItemSelection[] selections = selectionWeights.Keys.ToArray();
            Dictionary<FormKey, PluginEntry> savedSelections = previousSelections.ToDictionary(v => v.DeathItem, v => v.Selection ?? PluginEntry.SKIP);

            foreach (var selection in selections)
            {
                if (savedSelections.ContainsKey(selection.DeathItem))
                {
                    selection.Selection = savedSelections[selection.DeathItem];
                    if (debuggingMode) Write.Action(3, $"Previously selected {selection.Selection?.ProperName}.");
                }
                else
                {
                    var itemWeights = selectionWeights[selection];
                    List<PluginEntry> options = new(itemWeights.Keys);
                    if (options.Count == 0) continue;

                    options.Sort((a, b) => itemWeights[b].CompareTo(itemWeights[a]));
                    selection.Selection = options.First();
                    if (debuggingMode && !selection.DeathItem.IsNull)
                    {
                        selection.DeathItem.ToLink<DeathItemGetter>().TryResolve(linkCache, out var deathItem);
                        Write.Action(2, $"{deathItem?.EditorID ?? deathItem?.ToString() ?? "NO DEATH ITEM"}: heuristic selected {selection.Selection?.SortName}.");
                        Write.Action(3, $"From: {itemWeights.Pretty()}");

                        var npcNames = selection.AssignedNPCs.Take(6).Select(n => NpcNamer(n)).ToArray().Pretty();
                        Write.Action(3, $"Archetypes: {npcNames}");
                    }
                }
            }

            return selections;
        }
        catch (Exception ex)
        {
            throw new HeuristicsError(ex);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    static private Dictionary<PluginEntry, int> HeuristicNpcMatcher(INpcGetter npc, List<PluginEntry> plugins, 
        ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache, bool debuggingMode = false)
    {
        Dictionary<PluginEntry, int> candidates = new();
        string name = NpcNamer(npc);

        var clicker = DictionaryIncrementer(candidates);

        // Try to match the voice.
        if (!npc.Voice.IsNull)
        {
            plugins
                .Where(plugin => !plugin.Voice.IsNull)
                .Where(plugin => plugin.Voice.Equals(npc.Voice))
                .ForEach(clicker(10));
        }

        // Match the creature's editorId, internalName, and race internalName to the names of plugins.
        var nameMatches = new HashSet<PluginEntry>();
        var race = npc.Race.Resolve(linkCache);
        npc.DeathItem.TryResolve(linkCache, out var deathItem);

        if (npc.EditorID is string npcEditorId) plugins.Where(PluginNameMatch(npcEditorId)).ForEach(clicker(1));
        if (npc.Name?.ToString() is string npcName) plugins.Where(PluginNameMatch(npcName)).ForEach(clicker(1));
        if (race.EditorID is string raceEditorId) plugins.Where(PluginNameMatch(raceEditorId)).ForEach(clicker(1));
        if (race.Name?.ToString() is string raceName) plugins.Where(PluginNameMatch(raceName)).ForEach(clicker(1));

        // Try this tokenizing matcher to break ties.
        var npcTokens = Tokenizer.Tokenize(new List<string?>() { npc.Name?.ToString(), npc.EditorID, race.Name?.ToString(), race.EditorID, deathItem?.EditorID });
        if (debuggingMode) Write.Action(2, $"Tokens for {name} ({npc.EditorID}): {npcTokens.Pretty()}");

        foreach (var plugin in plugins)
        {
            int intersection = plugin.Tokens.Intersect(npcTokens).Count();
            if (intersection > 0) clicker(intersection)(plugin);
        }

        // @TODO Add matching for distinctive keywords?
        // @TODO Add exclusion terms?

        if (debuggingMode)
        {
            Write.Success(2, $"Candidates for {name}:");
            Write.Success(3, candidates.Pretty());
        }

        return candidates;
    }


    static public bool IsCreature(INpcGetter npc, bool debuggingMode = false)
    {
        var deathItem = npc.DeathItem;
        var edid = npc.EditorID;

        if (edid is not null && HasForbiddenEditorId(edid))
        {
            //if (debuggingMode) Write.Fail(3, $"Skipping {npc.EditorID} -- forbidden editorId {edid}");
            return false;
        }
        else if (deathItem == null)
        {
            //if (debuggingMode) Write.Fail(3, $"Skipping {npc.EditorID} -- no DeathItem");
            return false;
        }
        else if (HasForbiddenDeathItem(deathItem))
        {
            //if (debuggingMode) Write.Fail(3, $"Skipping {npc.EditorID} -- forbidden DeathItem {deathItem}");
            return false;
        }
        else if (HasForbiddenKeyword(npc))
        {
            //if (debuggingMode) Write.Fail(3, $"Skipping {npc.EditorID} -- forbidden DeathItem {GetForbiddenKeyword(npc)}");
            return false;
        }
        else if (HasForbiddenFaction(npc))
        {
            //if (debuggingMode) Write.Fail(3, $"Skipping {npc.EditorID} -- forbidden DeathItem {GetForbiddenFaction(npc)}");
            return false;
        }
        else if (!HasAllowedVoice(npc))
        {
            //if (debuggingMode) Write.Fail(3, $"Skipping {npc.EditorID} -- voice not allowed ({npc.Voice})");
            return false;
        }
        else if (HasForbiddenFlag(npc))
        {
            if (debuggingMode) Write.Fail(3, $"Skipping {npc.EditorID} -- forbidden flag {GetForbiddenFlag(npc)}");
            return false;
        }
        else if (npc.ActorEffect?.Contains(Skyrim.Spell.GhostAbility) ?? false)
        {
            if (debuggingMode) Write.Fail(3, $"Skipping {npc.EditorID} -- forbidden NO GHOSTS");
            return false;
        }
        else return true;
    }

    static private bool HasForbiddenEditorId(string editorId) => SpecialCases.Lists.ForbiddenNpcEditorIds.Any(edid => edid.EqualsIgnoreCase(editorId));

    static private bool HasForbiddenFaction(INpcGetter npc) =>
        npc.Factions.Any(placement => SpecialCases.Lists.ForbiddenFactions.Contains(placement.Faction));

    static private bool HasForbiddenKeyword(INpcGetter npc) =>
        npc.Keywords?.Any(keyword => SpecialCases.Lists.ForbiddenKeywords.Contains(keyword)) ?? false;

    static private bool HasAllowedVoice(INpcGetter npc) => SpecialCases.Lists.AllowedVoices.Contains(npc.Voice);

    static private bool HasForbiddenDeathItem(IFormLinkGetter<ILeveledItemGetter> deathItem) => SpecialCases.Lists.ForbiddenDeathItems.Contains(deathItem);

    static private bool HasForbiddenFlag(INpcGetter npc) => (SpecialCases.Lists.ForbiddenFlags & npc.Configuration.Flags) != 0;

    static private string GetForbiddenEditorId(string editorId) 
        => SpecialCases.Lists.ForbiddenNpcEditorIds.Where(edid => edid.EqualsIgnoreCase(editorId)).FirstOrDefault("");

    static private IRankPlacementGetter? GetForbiddenFaction(INpcGetter npc) =>
        npc.Factions.Where(placement => SpecialCases.Lists.ForbiddenFactions.Contains(placement.Faction)).First();

    static private IFormLinkGetter<IKeywordGetter> GetForbiddenKeyword(INpcGetter npc) =>
        npc.Keywords?.Where(keyword => SpecialCases.Lists.ForbiddenKeywords.Contains(keyword)).First() ?? new FormLink<IKeywordGetter>();

    static private NpcConfiguration.Flag GetForbiddenFlag(INpcGetter npc) => (SpecialCases.Lists.ForbiddenFlags & npc.Configuration.Flags);

    /// <summary>
    /// This thing is ridiculous but convenient. Can you say "Currying"?
    /// 
    /// All this does is create delegate for a dictionary, that creates a delegate for a number, which 
    /// increases the value associated with a key by the amount of the number.
    /// </summary>
    /// 
    static private Func<int, Action<T>> DictionaryIncrementer<T>(Dictionary<T, int> dict) where T : notnull
        => val => plugin => { if (val > 0) dict[plugin] = dict.GetValueOrDefault(plugin, 0) + val; };

    /// <summary>
    /// Matcher for plugin names. 
    /// A match occurs if the plugin internalName is contained in the target string.
    /// Case-insensitive.
    /// 
    /// </summary>
    /// <param internalName="str">The string against which to match the plugin names.</param>
    /// <returns>The matcher.</returns>
    /// 
    static private Func<PluginEntry, bool> PluginNameMatch(string str) => plugin => str.ContainsInsensitive(plugin.Name);

    static private string DeathItemNamer(DeathItemGetter deathItem)
        => deathItem.EditorID ?? /*deathItem.ToStandardizedIdentifier().ToString() ??*/ deathItem.FormKey.ToString();

    static private string NpcNamer(INpcGetter npc)
        => npc.Name?.ToString() ?? npc.EditorID ?? /*npc.ToStandardizedIdentifier().ToString() ??*/ npc.FormKey.ToString();

}
