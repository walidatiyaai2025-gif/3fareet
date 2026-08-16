using System;
using Afareet.Vehicle;

internal static class ArcadeDriveModifierContractRunner
{
    private static int Main()
    {
        try
        {
            NeutralContract();
            BoundsContract();
            DefaultFailsClosedContract();
            RepresentativeProjectionContract();
            Console.WriteLine("Arcade drive modifier behavior contract: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Arcade drive modifier behavior contract: FAIL: {ex.Message}");
            return 1;
        }
    }

    private static void NeutralContract()
    {
        var modifier = ArcadeDriveModifier.Neutral();
        Require(modifier.IsValid, "neutral modifier must be initialized");
        Require(modifier.IsNeutral, "neutral modifier must report neutral");
        Require(modifier.AccelerationMultiplier == 1d, "neutral acceleration must be 1");
        Require(modifier.MaxSpeedMultiplier == 1d, "neutral max speed must be 1");
        Require(modifier.SteeringAuthorityMultiplier == 1d, "neutral steering must be 1");
        Require(modifier.GripMultiplier == 1d, "neutral grip must be 1");
    }

    private static void BoundsContract()
    {
        var bounds = new ArcadeDriveModifier(
            ArcadeDriveModifier.MinimumMultiplier,
            ArcadeDriveModifier.MaximumMultiplier,
            ArcadeDriveModifier.MinimumMultiplier,
            ArcadeDriveModifier.MaximumMultiplier);
        Require(bounds.IsValid, "inclusive safety bounds must be accepted");

        RequireThrows<ArgumentOutOfRangeException>(() =>
            new ArcadeDriveModifier(.249d, 1d, 1d, 1d),
            "below-minimum multiplier must fail closed");
        RequireThrows<ArgumentOutOfRangeException>(() =>
            new ArcadeDriveModifier(2.001d, 1d, 1d, 1d),
            "above-maximum multiplier must fail closed");
        RequireThrows<ArgumentOutOfRangeException>(() =>
            new ArcadeDriveModifier(double.NaN, 1d, 1d, 1d),
            "NaN multiplier must fail closed");
        RequireThrows<ArgumentOutOfRangeException>(() =>
            new ArcadeDriveModifier(double.PositiveInfinity, 1d, 1d, 1d),
            "infinite multiplier must fail closed");
    }

    private static void DefaultFailsClosedContract()
    {
        var modifier = default(ArcadeDriveModifier);
        Require(!modifier.IsValid, "default struct must remain invalid");
        RequireThrows<ArgumentException>(() =>
            ArcadeDriveModifier.ValidateInitialized(modifier, nameof(modifier)),
            "default struct must be rejected by the application seam");
    }

    private static void RepresentativeProjectionContract()
    {
        var modifier = new ArcadeDriveModifier(.72d, 1.35d, .65d, .58d);
        Require(modifier.IsValid, "representative power-up projection must be accepted");
        Require(!modifier.IsNeutral, "representative projection must not report neutral");
        Require(Math.Abs(modifier.AccelerationMultiplier - .72d) < .0000001d, "acceleration must round-trip exactly");
        Require(Math.Abs(modifier.MaxSpeedMultiplier - 1.35d) < .0000001d, "max speed must round-trip exactly");
        Require(Math.Abs(modifier.SteeringAuthorityMultiplier - .65d) < .0000001d, "steering must round-trip exactly");
        Require(Math.Abs(modifier.GripMultiplier - .58d) < .0000001d, "grip must round-trip exactly");
    }

    private static void RequireThrows<TException>(Action action, string message)
        where TException : Exception
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
