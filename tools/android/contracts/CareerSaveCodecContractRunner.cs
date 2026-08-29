using System;
using Afareet.Progression;

internal static class CareerSaveCodecContractRunner
{
    private static int Main()
    {
        try
        {
            var codec = new CareerSaveCodec();
            var progress = new CareerProgress(
                CareerProgress.CurrentVersion,
                7,
                new[] { "node_b", "node_\"a\\line\n" },
                new[] { "reward_z", "reward_a" });

            var encoded = codec.Encode(progress);
            var decoded = codec.Decode(encoded);
            Require(encoded == codec.Encode(decoded), "canonical encode/decode must be deterministic");
            Require(decoded.Stars == 7, "v1 round-trip must preserve stars");
            Require(decoded.CompletedNodeIds.Count == 2, "v1 round-trip must preserve completed ids");
            Require(decoded.ClaimedRewardIds.Count == 2, "v1 round-trip must preserve claimed reward ids");
            Require(decoded.CompletedNodeIds[0] == "node_\"a\\line\n", "escaped id must round-trip exactly");

            var migrated = codec.Decode("{\"totalStars\":12000,\"completed\":[\"legacy_race\",\"\",7,null,\"legacy_race\"]}");
            Require(migrated.Version == CareerProgress.CurrentVersion, "legacy save must migrate to current version");
            Require(migrated.Stars == CareerSaveCodec.MaxStoredStars, "legacy stars must clamp to persisted bound");
            Require(migrated.CompletedNodeIds.Count == 1 && migrated.CompletedNodeIds[0] == "legacy_race", "legacy ids must filter invalid members and deduplicate");
            Require(migrated.ClaimedRewardIds.Count == 0, "legacy migration must initialize claimed rewards empty");

            var explicitV0 = codec.Decode("{\"version\":0,\"totalStars\":4,\"completed\":[\"legacy_race\"]}");
            Require(explicitV0.Stars == 4, "explicit v0 migration must retain valid stars");

            var negative = codec.Decode("{\"version\":1,\"stars\":-4,\"completedNodeIds\":[],\"claimedRewardIds\":[]}");
            Require(negative.Stars == 0, "negative persisted stars must clamp to zero");

            var tooLarge = new CareerProgress(
                CareerProgress.CurrentVersion,
                CareerSaveCodec.MaxStoredStars + 1,
                Array.Empty<string>(),
                Array.Empty<string>());
            Expect<ArgumentOutOfRangeException>(() => codec.Encode(tooLarge), "encode above persisted star bound must fail closed");

            Expect<FormatException>(() => codec.Decode("[]"), "non-object root must fail closed");
            Expect<FormatException>(() => codec.Decode("{\"version\":\"1\"}"), "wrong version type must fail closed");
            Expect<FormatException>(() => codec.Decode("{\"version\":1,\"stars\":{}}"), "wrong stars type must fail closed");
            Expect<FormatException>(() => codec.Decode("{\"version\":2}"), "unsupported version must fail closed");
            Expect<FormatException>(() => codec.Decode("{\"version\":1,\"version\":1}"), "duplicate JSON keys must fail closed");
            Expect<FormatException>(() => codec.Decode("{\"version\":1} trailing"), "trailing content must fail closed");

            Console.WriteLine("Career save codec behavior contract passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Expect<TException>(Action action, string message) where TException : Exception
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
}
