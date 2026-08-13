using NUnit.Framework;

namespace Afareet.Tests.Support
{
    public sealed class SupportNextPolicyTests
    {
        [Test] public void TouchIntentHonorsBrakeAndDeadZone()
        {
            var intent = Afareet.Support.TouchGesturePolicy.Compose(.04f, 1f, true, true);
            Assert.That(intent.Steering, Is.EqualTo(0f));
            Assert.That(intent.Brake, Is.True);
            Assert.That(intent.Nitro, Is.False);
        }

        [Test] public void EngineLayersShiftWithSpeed()
        {
            Assert.That(Afareet.Support.EngineLayerPolicy.Idle(0f), Is.GreaterThan(Afareet.Support.EngineLayerPolicy.Idle(1f)));
            Assert.That(Afareet.Support.EngineLayerPolicy.High(1f), Is.GreaterThan(.9f));
        }

        [Test] public void SfxImpactHasHighestPriority()
        {
            Assert.That(Afareet.Support.GameplaySfxMixPolicy.Priority(Afareet.Support.GameplaySfxKind.Impact), Is.GreaterThan(Afareet.Support.GameplaySfxMixPolicy.Priority(Afareet.Support.GameplaySfxKind.Drift)));
        }

        [Test] public void ReuseBudgetNeverCapsBelowWarmCount()
        {
            var budget = Afareet.Support.ReuseBudgetPolicy.For(Afareet.Support.ReuseTier.Low, 10);
            Assert.That(budget.MaxCount, Is.GreaterThanOrEqualTo(budget.WarmCount));
        }

        [Test] public void RivalVariantIsDeterministic()
        {
            Assert.That(Afareet.Support.RivalVisualVariantPolicy.VariantIndex("rival-a", 3), Is.EqualTo(Afareet.Support.RivalVisualVariantPolicy.VariantIndex("rival-a", 3)));
        }

        [Test] public void ModulePlacementAlternatesSides()
        {
            Assert.That(Afareet.Support.ModulePlacementPolicy.SideOffset(0, 5f, 2f), Is.LessThan(0f));
            Assert.That(Afareet.Support.ModulePlacementPolicy.SideOffset(1, 5f, 2f), Is.GreaterThan(0f));
        }

        [Test] public void VisibilityDropsDetailWithDistance()
        {
            Assert.That(Afareet.Support.VisibilityPolicy.Detail(20f), Is.EqualTo(0));
            Assert.That(Afareet.Support.VisibilityPolicy.Detail(200f), Is.EqualTo(2));
        }

        [Test] public void SceneBudgetScalesByTier()
        {
            Assert.That(Afareet.Support.SceneBudgetPolicy.ItemCount(2), Is.GreaterThan(Afareet.Support.SceneBudgetPolicy.ItemCount(0)));
        }

        [Test] public void LayoutSeedProducesStableSlot()
        {
            Assert.That(Afareet.Support.LayoutSeedPolicy.Slot(42, 3, 8), Is.EqualTo(Afareet.Support.LayoutSeedPolicy.Slot(42, 3, 8)));
        }

        [Test] public void LodSelectionUsesScreenFraction()
        {
            Assert.That(Afareet.Support.LodSelectionPolicy.Select(.2f), Is.EqualTo(0));
            Assert.That(Afareet.Support.LodSelectionPolicy.Select(.03f), Is.EqualTo(2));
        }
    }
}
