using System;
using Afareet.Progression;

namespace Afareet.CareerRuntime
{
    public interface ICareerProgressStorage
    {
        bool TryRead(out string payload);
        void Write(string payload);
        void Clear();
    }

    public sealed class CareerProgressLoadResult
    {
        public CareerProgress Progress { get; }
        public bool HasStoredPayload { get; }
        public bool RecoveredFromInvalidPayload { get; }
        public string Error { get; }

        public CareerProgressLoadResult(
            CareerProgress progress,
            bool hasStoredPayload,
            bool recoveredFromInvalidPayload,
            string error = null)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            HasStoredPayload = hasStoredPayload;
            RecoveredFromInvalidPayload = recoveredFromInvalidPayload;
            Error = error;
        }
    }

    public sealed class CareerProgressStore
    {
        private readonly ICareerProgressStorage storage;
        private readonly CareerSaveCodec codec;

        public CareerProgressStore(
            ICareerProgressStorage storage,
            CareerSaveCodec codec = null)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.codec = codec ?? new CareerSaveCodec();
        }

        public CareerProgressLoadResult Load()
        {
            string payload;
            if (!storage.TryRead(out payload) || string.IsNullOrWhiteSpace(payload))
            {
                return new CareerProgressLoadResult(
                    CareerProgress.Empty(),
                    hasStoredPayload: false,
                    recoveredFromInvalidPayload: false);
            }

            try
            {
                return new CareerProgressLoadResult(
                    codec.Decode(payload),
                    hasStoredPayload: true,
                    recoveredFromInvalidPayload: false);
            }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is ArgumentException ||
                exception is OverflowException)
            {
                // Fail safe for gameplay, but preserve the original stored bytes for diagnosis/recovery.
                return new CareerProgressLoadResult(
                    CareerProgress.Empty(),
                    hasStoredPayload: true,
                    recoveredFromInvalidPayload: true,
                    error: exception.Message);
            }
        }

        public void Save(CareerProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            storage.Write(codec.Encode(progress));
        }

        public void Clear()
        {
            storage.Clear();
        }
    }
}
