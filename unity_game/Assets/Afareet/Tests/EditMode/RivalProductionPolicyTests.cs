using Afareet.Vehicle;
using NUnit.Framework;

namespace Afareet.Tests
{
    public sealed class RivalProductionPolicyTests
    {
        [Test]
        public void RivalProductionContractIsInternallyValid()
        {
            Assert.DoesNotThrow(RivalProductionPolicy.ValidateContract);
        }

        [Test]
        public void ThreeProductionRivalPathsAreStableAndDistinct()
        {
            Assert.That(RivalProductionPolicy.VariantCount, Is.EqualTo(3));
            Assert.That(RivalProductionPolicy.ResourcePath(0), Is.EqualTo("Art/Vehicles/Rivals/Production/PF_Rival_01_Production"));
            Assert.That(RivalProductionPolicy.ResourcePath(1), Is.EqualTo("Art/Vehicles/Rivals/Production/PF_Rival_02_Production"));
            Assert.That(RivalProductionPolicy.ResourcePath(2), Is.EqualTo("Art/Vehicles/Rivals/Production/PF_Rival_03_Production"));
            Assert.That(RivalProductionPolicy.ResourcePath(0), Is.Not.EqualTo(RivalProductionPolicy.ResourcePath(1)));
            Assert.That(RivalProductionPolicy.ResourcePath(1), Is.Not.EqualTo(RivalProductionPolicy.ResourcePath(2)));
        }

        [Test]
        public void AuthoredSourceMustBeAUnityProjectModelAssetPath()
        {
            Assert.That(RivalProductionPolicy.IsSupportedAuthoredModelSource("Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01.fbx"), Is.True);
            Assert.That(RivalProductionPolicy.IsSupportedAuthoredModelSource("Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02.OBJ"), Is.True);
            Assert.That(RivalProductionPolicy.IsSupportedAuthoredModelSource("Rival_01.fbx"), Is.False);
            Assert.That(RivalProductionPolicy.IsSupportedAuthoredModelSource("../Rival_01.fbx"), Is.False);
            Assert.That(RivalProductionPolicy.IsSupportedAuthoredModelSource("docs/assets/Rival_01.fbx"), Is.False);
            Assert.That(RivalProductionPolicy.IsSupportedAuthoredModelSource("Assets/Afareet/Rival_01.prefab"), Is.False);
        }

        [Test]
        public void SurfaceAuthoringIsMandatoryEvenAtValidTriangleCount()
        {
            Assert.That(RivalProductionPolicy.MeetsProductionFloor(0, 4000, false, true, true), Is.False);
            Assert.That(RivalProductionPolicy.MeetsProductionFloor(0, 4000, true, false, true), Is.False);
            Assert.That(RivalProductionPolicy.MeetsProductionFloor(0, 4000, true, true, false), Is.False);
            Assert.That(RivalProductionPolicy.MeetsProductionFloor(0, 4000, true, true, true), Is.True);
        }

        [Test]
        public void PrimitiveBlockoutScaleGeometryCannotQualifyAsProduction()
        {
            Assert.That(RivalProductionPolicy.MeetsProductionFloor(0, 24, true, true, true), Is.False);
            Assert.That(RivalProductionPolicy.MeetsProductionFloor(1, 24, true, true, true), Is.False);
            Assert.That(RivalProductionPolicy.MeetsProductionFloor(2, 24, true, true, true), Is.False);
        }
    }
}
