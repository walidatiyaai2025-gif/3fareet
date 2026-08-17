using System;
using Afareet.GarageRuntime;

internal static class GarageRuntimeContractRunner
{
    private static int Main()
    {
        try
        {
            CatalogContract();
            UnlockEquipContract();
            CustomizationContract();
            PersistenceContract();
            MigrationRecoveryContract();
            Console.WriteLine("Garage runtime behavior contract: PASS");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Garage runtime behavior contract: FAIL — {exception}");
            return 1;
        }
    }

    private static void CatalogContract()
    {
        var catalog = GarageCatalog.CreateDefault();
        Require(catalog.SchemaVersion == GarageCatalog.CurrentSchemaVersion, "schema version");
        Require(catalog.Vehicles.Count == 4, "four vehicle definitions");
        Require(catalog.Vehicles[0].Id == "afareet_king", "starter id");
        Require(catalog.Vehicles[1].Id == "wedge_coupe", "wedge id");
        Require(catalog.Vehicles[2].Id == "fastback_muscle", "fastback id");
        Require(catalog.Vehicles[3].Id == "djinn_spirit", "djinn id");

        foreach (var definition in catalog.Vehicles)
        {
            Require(definition.Cosmetics.Allows(definition.Cosmetics.CreateDefaultSelection()),
                $"default cosmetics allowed for {definition.Id}");
            var normalized = catalog.NormalizeStats(definition.Id);
            Require(InUnitRange(normalized.TopSpeed), $"top speed normalized for {definition.Id}");
            Require(InUnitRange(normalized.Acceleration), $"acceleration normalized for {definition.Id}");
            Require(InUnitRange(normalized.Handling), $"handling normalized for {definition.Id}");
            Require(InUnitRange(normalized.Drift), $"drift normalized for {definition.Id}");
            Require(InUnitRange(normalized.Spirit), $"spirit normalized for {definition.Id}");
        }
    }

    private static void UnlockEquipContract()
    {
        var catalog = GarageCatalog.CreateDefault();
        var service = new GarageService(catalog);
        Require(service.State.EquippedVehicleId == GarageCatalog.StarterVehicleId, "starter auto-equipped");
        Require(service.IsUnlocked(GarageCatalog.StarterVehicleId), "starter always unlocked");
        Require(!service.IsUnlocked("wedge_coupe"), "wedge initially locked");
        RequireThrows<InvalidOperationException>(() => service.Equip("wedge_coupe"), "locked equip rejected");

        service.ReplaceUnlockedVehicleIds(new[] { "wedge_coupe" });
        service.Equip("wedge_coupe");
        Require(service.State.EquippedVehicleId == "wedge_coupe", "unlocked equip succeeds");

        service.ReplaceUnlockedVehicleIds(Array.Empty<string>());
        Require(service.State.EquippedVehicleId == GarageCatalog.StarterVehicleId,
            "removed unlock falls back to starter");
    }

    private static void CustomizationContract()
    {
        var catalog = GarageCatalog.CreateDefault();
        var service = new GarageService(catalog, new[] { "wedge_coupe" });
        var selection = new GarageCosmeticSelection("obsidian", "shadow-rim", "purple-wisp", "afareet");
        service.Customize("wedge_coupe", selection);
        Require(service.GetDetail("wedge_coupe").Selection.Equals(selection), "customization persisted in service state");

        RequireThrows<InvalidOperationException>(() => service.Customize(
            "wedge_coupe",
            new GarageCosmeticSelection("unknown-paint", "shadow-rim", "purple-wisp", "afareet")),
            "unknown cosmetic rejected");
    }

    private static void PersistenceContract()
    {
        var catalog = GarageCatalog.CreateDefault();
        var service = new GarageService(catalog, new[] { "djinn_spirit" });
        service.Equip("djinn_spirit");
        service.Customize(
            "djinn_spirit",
            new GarageCosmeticSelection("obsidian", "shadow-rim", "purple-wisp", "afareet"));

        var codec = new GarageStateCodec();
        var payload = codec.Encode(service.State);
        Require(payload.StartsWith(GarageStateCodec.CurrentHeader, StringComparison.Ordinal), "V2 header");
        var decoded = codec.Decode(payload);
        Require(!decoded.MigratedLegacyV1, "V2 not marked migrated");
        var restored = new GarageService(catalog, new[] { "djinn_spirit" }, decoded.State);
        Require(restored.State.EquippedVehicleId == "djinn_spirit", "equipped vehicle roundtrip");
        Require(restored.State.TryGetSelection("djinn_spirit", out var selection), "selection roundtrip exists");
        Require(selection.PaintId == "obsidian", "selection roundtrip value");
    }

    private static void MigrationRecoveryContract()
    {
        var catalog = GarageCatalog.CreateDefault();
        var codec = new GarageStateCodec();
        var storage = new MemoryStorage(codec.EncodeLegacyV1ForMigrationFixture("djinn_spirit"));
        var store = new GarageStateStore(storage, catalog);
        var migrated = store.Load(new[] { "djinn_spirit" });
        Require(migrated.MigratedLegacyV1, "V1 migration flag");
        Require(!migrated.RecoveredFromInvalidPayload, "valid V1 not recovery");
        Require(migrated.RewrittenCanonicalPayload, "legacy payload rewritten");
        Require(migrated.State.EquippedVehicleId == "djinn_spirit", "legacy equipped preserved");
        Require(storage.Payload.StartsWith(GarageStateCodec.CurrentHeader, StringComparison.Ordinal),
            "legacy storage rewritten to V2");

        storage.Payload = "not a garage save";
        var recovered = store.Load();
        Require(recovered.RecoveredFromInvalidPayload, "invalid payload recovery flag");
        Require(recovered.RewrittenCanonicalPayload, "invalid payload rewritten");
        Require(recovered.State.EquippedVehicleId == GarageCatalog.StarterVehicleId,
            "invalid payload recovers starter");
        Require(!string.IsNullOrWhiteSpace(recovered.Error), "invalid payload recovery diagnostic");
        Require(storage.Payload.StartsWith(GarageStateCodec.CurrentHeader, StringComparison.Ordinal),
            "invalid storage rewritten to V2");
    }

    private static bool InUnitRange(float value) => value >= 0f && value <= 1f;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void RequireThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }

    private sealed class MemoryStorage : IGarageStateStorage
    {
        public string Payload { get; set; }

        public MemoryStorage(string payload = null)
        {
            Payload = payload;
        }

        public bool TryRead(out string payload)
        {
            payload = Payload;
            return Payload != null;
        }

        public void Write(string payload)
        {
            Payload = payload;
        }
    }
}
