using System;
using Afareet.GarageRuntime;
using NUnit.Framework;

namespace Afareet.Tests.Garage
{
    public sealed class GaragePreviewResolverTests
    {
        [Test]
        public void DefaultCatalogPreviewPathsAreExplicitProductionResources()
        {
            var catalog = GarageCatalog.CreateDefault();
            foreach (var definition in catalog.Vehicles)
            {
                Assert.DoesNotThrow(() =>
                    GaragePreviewResourcePolicy.ValidateProductionResourcePathOrThrow(
                        definition.PreviewResourcePath));
            }
        }

        [TestCase("Art/Vehicles/HeroCar/Generated/PF_Hero")]
        [TestCase("Art/Vehicles/HeroCar/Refinement/PF_Hero")]
        [TestCase("Art/Vehicles/Rivals/Review/PF_Rival")]
        [TestCase("Art/Vehicles/Rivals/Blockout/PF_Rival")]
        [TestCase("Art/Vehicles/Rivals/Prototype/PF_Rival")]
        public void NonProductionPreviewPathsAreRejected(string path)
        {
            Assert.Throws<InvalidOperationException>(() =>
                GaragePreviewResourcePolicy.ValidateProductionResourcePathOrThrow(path));
        }

        [Test]
        public void ProductionSegmentIsMandatory()
        {
            Assert.Throws<InvalidOperationException>(() =>
                GaragePreviewResourcePolicy.ValidateProductionResourcePathOrThrow(
                    "Art/Vehicles/Rivals/PF_Rival_01"));
        }

        [Test]
        public void NonVehicleResourceRootsAreRejected()
        {
            Assert.Throws<InvalidOperationException>(() =>
                GaragePreviewResourcePolicy.ValidateProductionResourcePathOrThrow(
                    "Art/Environment/Production/PF_Cairo"));
        }
    }
}
