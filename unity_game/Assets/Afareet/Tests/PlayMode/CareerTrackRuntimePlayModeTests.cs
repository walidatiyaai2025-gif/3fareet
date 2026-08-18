using System;
using System.Collections;
using System.Collections.Generic;
using Afareet.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Afareet.Tests.PlayMode
{
    public sealed class CareerTrackRuntimePlayModeTests
    {
        private GameObject host;
        private readonly List<GameObject> builtRoots = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new GameObject("CAREER TRACK TEST HOST");
            builtRoots.Clear();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var index = 0; index < builtRoots.Count; index++)
                if (builtRoots[index] != null) Object.Destroy(builtRoots[index]);
            if (host != null) Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllRetainedTrackIds_BuildDeterministicallyFromAuthoredCairoRoute()
        {
            Assert.That(CairoCareerTrackCatalog.Specs.Count, Is.EqualTo(5));
            var signatures = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < CairoCareerTrackCatalog.Specs.Count; index++)
            {
                var spec = CairoCareerTrackCatalog.Specs[index];
                var build = CairoCareerTrackBuilder.Build(host.transform, spec);
                builtRoots.Add(build.Root);

                Assert.That(build.Spec.Id, Is.EqualTo(spec.Id));
                Assert.That(build.Root.name, Is.EqualTo($"CAREER TRACK // {spec.Id}"));
                Assert.That(build.Track.Waypoints.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(build.Root.transform.localScale, Is.EqualTo(Vector3.one * spec.UniformScale));
                Assert.That(signatures.Add(spec.DeterministicSignature), Is.True, $"duplicate signature: {spec.Id}");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CornicheTrack_PreservesP1IdentityTransform()
        {
            var spec = CairoCareerTrackCatalog.Resolve(CairoCareerTrackCatalog.CornicheNightId);
            var build = CairoCareerTrackBuilder.Build(host.transform, spec);
            builtRoots.Add(build.Root);

            Assert.That(spec.UniformScale, Is.EqualTo(1f));
            Assert.That(spec.YawDegrees, Is.EqualTo(0f));
            Assert.That(build.Root.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(build.Root.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(build.Root.transform.localScale, Is.EqualTo(Vector3.one));
            yield return null;
        }

        [Test]
        public void UnknownTrackId_FailsClosedBeforeCreatingRuntimeRoot()
        {
            var before = host.transform.childCount;
            Assert.Throws<ArgumentException>(() => CairoCareerTrackBuilder.Build(host.transform, "missing_track"));
            Assert.That(host.transform.childCount, Is.EqualTo(before));
        }
    }
}
