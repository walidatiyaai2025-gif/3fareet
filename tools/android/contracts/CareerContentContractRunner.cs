using System;
using Afareet.Progression;

internal static class CareerContentContractRunner
{
    private static int Main()
    {
        try
        {
            var definitions = ChapterOneCareerEventContent.CreateDefinitions();
            var expectedNodeIds = new[] { "c01_r01", "c01_r02", "c01_r03", "c01_r04", "c01_boss" };
            var expectedCoins = new[] { 250, 350, 450, 550, 650 };
            var expectedSpirit = new[] { 5, 6, 7, 8, 9 };

            Require(definitions.Count == 5, "Chapter 1 content must contain exactly five definitions");
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                Require(definition.Node.Id == expectedNodeIds[index], "node ordering/id parity drifted");
                Require(definition.Objectives[0].Id == $"finish_{expectedNodeIds[index]}", "finish objective id parity drifted");
                Require(definition.Objectives[0].Description == "Finish the event", "finish objective copy parity drifted");
                Require(definition.Objectives[0].Target == 1d, "finish objective target parity drifted");
                Require(definition.Rewards[0].Coins == expectedCoins[index], "coin reward parity drifted");
                Require(definition.Rewards[0].Spirit == expectedSpirit[index], "spirit reward parity drifted");

                if (index == 0)
                {
                    Require(definition.Objectives.Count == 1, "first node must not have clean-run objective");
                }
                else
                {
                    Require(definition.Objectives.Count == 2, "later nodes must have two objectives");
                    Require(definition.Objectives[1].Id == $"clean_{expectedNodeIds[index]}", "clean objective id parity drifted");
                    Require(definition.Objectives[1].Description == "Finish without restart", "clean objective copy parity drifted");
                    Require(definition.Objectives[1].Target == 1d, "clean objective target parity drifted");
                }
            }

            var boss = definitions[4];
            Require(boss.Node.Mode == CareerRaceMode.Boss, "final Chapter 1 definition must remain boss mode");
            Require(boss.Rewards.Count == 2, "boss must contain primary reward and unlock payload");
            Require(boss.Rewards[1].Coins == 0 && boss.Rewards[1].Spirit == 0, "boss unlock payload must not invent currency");
            Require(boss.Rewards[1].UnlockVehicleId == "djinn_spirit", "boss unlock vehicle parity drifted");

            Expect<ArgumentException>(() => new CareerObjective(" ", "Finish", 1d), "blank objective id must fail closed");
            Expect<ArgumentOutOfRangeException>(() => new CareerObjective("finish", "Finish", 0d), "non-positive target must fail closed");
            Expect<ArgumentException>(() => new CareerReward(), "empty reward must fail closed");

            var node = ChapterOneCareerContent.CreateFoundation().Nodes[0];
            Expect<ArgumentException>(() => new CareerNodeDefinition(
                node,
                new[] { new CareerObjective("same", "A", 1d), new CareerObjective("same", "B", 1d) },
                new[] { new CareerReward(coins: 1) }), "duplicate objective ids must fail closed");

            Console.WriteLine("Career content behavior contract: PASS");
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
