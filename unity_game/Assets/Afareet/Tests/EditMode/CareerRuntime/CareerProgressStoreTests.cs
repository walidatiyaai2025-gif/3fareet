using Afareet.CareerRuntime;
using Afareet.Progression;
using NUnit.Framework;

namespace Afareet.Tests.CareerRuntime
{
    public sealed class CareerProgressStoreTests
    {
        private sealed class MemoryStorage : ICareerProgressStorage
        {
            public string Payload;
            public bool TryRead(out string payload)
            {
                payload = Payload;
                return Payload != null;
            }
            public void Write(string payload) => Payload = payload;
            public void Clear() => Payload = null;
        }

        [Test]
        public void MissingPayload_LoadsEmptyProgress()
        {
            var storage = new MemoryStorage();
            var result = new CareerProgressStore(storage).Load();

            Assert.That(result.HasStoredPayload, Is.False);
            Assert.That(result.RecoveredFromInvalidPayload, Is.False);
            Assert.That(result.Progress.Stars, Is.Zero);
            Assert.That(result.Progress.CompletedNodeIds, Is.Empty);
        }

        [Test]
        public void SaveThenLoad_RoundTripsAuthoritativeCodec()
        {
            var storage = new MemoryStorage();
            var store = new CareerProgressStore(storage);
            var source = new CareerProgress(
                CareerProgress.CurrentVersion,
                7,
                new[] { "c01_r01", "c01_r02" },
                new[] { "career:c01_r01:reward:00" });

            store.Save(source);
            var result = store.Load();

            Assert.That(result.HasStoredPayload, Is.True);
            Assert.That(result.RecoveredFromInvalidPayload, Is.False);
            Assert.That(result.Progress.Stars, Is.EqualTo(7));
            Assert.That(result.Progress.IsNodeCompleted("c01_r02"), Is.True);
            Assert.That(result.Progress.IsRewardClaimed("career:c01_r01:reward:00"), Is.True);
        }

        [Test]
        public void InvalidPayload_FailsSafeWithoutDeletingEvidence()
        {
            var storage = new MemoryStorage { Payload = "{not valid json" };
            var store = new CareerProgressStore(storage);

            var result = store.Load();

            Assert.That(result.HasStoredPayload, Is.True);
            Assert.That(result.RecoveredFromInvalidPayload, Is.True);
            Assert.That(result.Progress.Stars, Is.Zero);
            Assert.That(result.Error, Is.Not.Empty);
            Assert.That(storage.Payload, Is.EqualTo("{not valid json"));
        }

        [Test]
        public void Clear_RemovesStoredPayload()
        {
            var storage = new MemoryStorage();
            var store = new CareerProgressStore(storage);
            store.Save(CareerProgress.Empty());
            Assert.That(storage.Payload, Is.Not.Null);

            store.Clear();

            Assert.That(storage.Payload, Is.Null);
            Assert.That(store.Load().HasStoredPayload, Is.False);
        }
    }
}
