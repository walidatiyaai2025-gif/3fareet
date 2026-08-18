using Afareet.GarageRuntime;
using NUnit.Framework;

namespace Afareet.Tests.Garage
{
    public sealed class GaragePersistenceTests
    {
        [Test]
        public void V2CodecRoundTripsEquippedVehicleAndCustomization()
        {
            var catalog = GarageCatalog.CreateDefault();
            var service = new GarageService(catalog, new[] { "wedge_coupe" });
            var selection = new GarageCosmeticSelection("obsidian", "shadow-rim", "purple-wisp", "afareet");
            service.Customize("wedge_coupe", selection);
            service.Equip("wedge_coupe");

            var codec = new GarageStateCodec();
            var decoded = codec.Decode(codec.Encode(service.State));

            Assert.That(decoded.MigratedLegacyV1, Is.False);
            Assert.That(decoded.State.EquippedVehicleId, Is.EqualTo("wedge_coupe"));
            Assert.That(decoded.State.TryGetSelection("wedge_coupe", out var restored), Is.True);
            Assert.That(restored, Is.EqualTo(selection));
        }

        [Test]
        public void LegacyV1MigratesWithoutInventingCosmetics()
        {
            var codec = new GarageStateCodec();
            var payload = codec.EncodeLegacyV1ForMigrationFixture("djinn_spirit");
            var decoded = codec.Decode(payload);

            Assert.That(decoded.MigratedLegacyV1, Is.True);
            Assert.That(decoded.State.EquippedVehicleId, Is.EqualTo("djinn_spirit"));
            Assert.That(decoded.State.Selections.Count, Is.Zero);
        }

        [Test]
        public void StoreMigratesAndRewritesLegacyPayloadToCanonicalV2()
        {
            var catalog = GarageCatalog.CreateDefault();
            var codec = new GarageStateCodec();
            var storage = new MemoryStorage(codec.EncodeLegacyV1ForMigrationFixture("djinn_spirit"));
            var store = new GarageStateStore(storage, catalog);

            var loaded = store.Load(new[] { "djinn_spirit" });

            Assert.That(loaded.HasStoredPayload, Is.True);
            Assert.That(loaded.MigratedLegacyV1, Is.True);
            Assert.That(loaded.RecoveredFromInvalidPayload, Is.False);
            Assert.That(loaded.RewrittenCanonicalPayload, Is.True);
            Assert.That(loaded.State.EquippedVehicleId, Is.EqualTo("djinn_spirit"));
            Assert.That(storage.Payload, Does.StartWith(GarageStateCodec.CurrentHeader));
            Assert.That(storage.Payload, Does.Not.StartWith(GarageStateCodec.LegacyHeaderV1));
        }

        [Test]
        public void InvalidPayloadRecoversAndRewritesCanonicalStarterState()
        {
            var catalog = GarageCatalog.CreateDefault();
            var storage = new MemoryStorage("corrupt payload");
            var store = new GarageStateStore(storage, catalog);

            var loaded = store.Load();

            Assert.That(loaded.HasStoredPayload, Is.True);
            Assert.That(loaded.RecoveredFromInvalidPayload, Is.True);
            Assert.That(loaded.RewrittenCanonicalPayload, Is.True);
            Assert.That(loaded.State.EquippedVehicleId, Is.EqualTo(GarageCatalog.StarterVehicleId));
            Assert.That(loaded.Error, Is.Not.Empty);
            Assert.That(storage.Payload, Does.StartWith(GarageStateCodec.CurrentHeader));
        }

        [Test]
        public void V2PayloadWithUnavailableEquippedVehicleIsNormalizedAndRewritten()
        {
            var catalog = GarageCatalog.CreateDefault();
            var codec = new GarageStateCodec();
            var staleState = new GarageState("djinn_spirit");
            var storage = new MemoryStorage(codec.Encode(staleState));
            var store = new GarageStateStore(storage, catalog);

            var loaded = store.Load();

            Assert.That(loaded.MigratedLegacyV1, Is.False);
            Assert.That(loaded.RecoveredFromInvalidPayload, Is.False);
            Assert.That(loaded.RewrittenCanonicalPayload, Is.True);
            Assert.That(loaded.State.EquippedVehicleId, Is.EqualTo(GarageCatalog.StarterVehicleId));
            Assert.That(codec.Decode(storage.Payload).State.EquippedVehicleId, Is.EqualTo(GarageCatalog.StarterVehicleId));
        }

        [Test]
        public void CanonicalV2PayloadDoesNotRewriteOnLoad()
        {
            var catalog = GarageCatalog.CreateDefault();
            var codec = new GarageStateCodec();
            var state = new GarageService(catalog).State;
            var storage = new MemoryStorage(codec.Encode(state));
            var writesBefore = storage.WriteCount;
            var store = new GarageStateStore(storage, catalog);

            var loaded = store.Load();

            Assert.That(loaded.RewrittenCanonicalPayload, Is.False);
            Assert.That(storage.WriteCount, Is.EqualTo(writesBefore));
        }

        [Test]
        public void MissingPayloadCreatesStarterStateWithoutInventingStoredSave()
        {
            var catalog = GarageCatalog.CreateDefault();
            var storage = new MemoryStorage();
            var store = new GarageStateStore(storage, catalog);

            var loaded = store.Load();

            Assert.That(loaded.HasStoredPayload, Is.False);
            Assert.That(loaded.RecoveredFromInvalidPayload, Is.False);
            Assert.That(loaded.RewrittenCanonicalPayload, Is.False);
            Assert.That(loaded.State.EquippedVehicleId, Is.EqualTo(GarageCatalog.StarterVehicleId));
            Assert.That(storage.Payload, Is.Null);
        }

        [Test]
        public void SaveWritesCanonicalV2Payload()
        {
            var catalog = GarageCatalog.CreateDefault();
            var storage = new MemoryStorage();
            var store = new GarageStateStore(storage, catalog);
            var state = new GarageService(catalog).State;

            store.Save(state);

            Assert.That(storage.Payload, Does.StartWith(GarageStateCodec.CurrentHeader));
        }

        private sealed class MemoryStorage : IGarageStateStorage
        {
            public string Payload { get; private set; }
            public int WriteCount { get; private set; }

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
                WriteCount++;
            }
        }
    }
}
