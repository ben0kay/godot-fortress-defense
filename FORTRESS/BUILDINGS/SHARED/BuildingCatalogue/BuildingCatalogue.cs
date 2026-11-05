// Small reusable authored catalogue, validated once when a level session configures placement.
using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

[GlobalClass]
public partial class BuildingCatalogue : Resource
{
    [Export] public Godot.Collections.Array<BuildingCatalogueEntry> Entries { get; set; } = new();

    // Catch duplicate IDs and scene/definition mismatches at setup, not on every preview frame.
    public IReadOnlyDictionary<string, BuildingCatalogueEntry> CreateLookup()
    {
        var lookup = new Dictionary<string, BuildingCatalogueEntry>(StringComparer.Ordinal);
        if (Entries == null) throw new InvalidOperationException("Catalogue entries are required.");
        foreach (var entry in Entries)
        {
            if (entry?.Definition == null || entry.Scene == null) throw new InvalidOperationException("Incomplete catalogue entry.");
            entry.Definition.Validate();
            string id = entry.Definition.BuildingId;
            if (!entry.Definition.Purchasable)
                throw new InvalidOperationException($"Catalogue building '{id}' is not purchasable. Enable Purchasable on '{entry.Definition.ResourcePath}' or remove its catalogue entry.");
            if (!lookup.TryAdd(id, entry))
                throw new InvalidOperationException($"Catalogue contains duplicate BuildingId '{id}' ({entry.Definition.ResourcePath}).");
            Node prototype = entry.Scene.Instantiate();
            try
            {
                if (prototype is not Building building || building.Definition != entry.Definition)
                    throw new InvalidOperationException("Catalogue scene must use its paired BuildingDefinition.");
            }
            finally { prototype.Free(); }
        }
        return new ReadOnlyDictionary<string, BuildingCatalogueEntry>(lookup);
    }
}
