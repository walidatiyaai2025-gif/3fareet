using System.Collections.Generic;
using Afareet.Race;
using Afareet.World;
using NUnit.Framework;
using UnityEngine;

namespace Afareet.Tests.Race
{
    public sealed class TrackBoundaryTests
    {
        private readonly List<GameObject> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
        }

        [Test]
        public void Sample_ClassifiesCenterLineAsOnRoad()
        {
            var track = CreateSquareTrack();
            var sample = TrackBoundaryPolicy.Sample(track.Waypoints, new Vector3(0f, 0f, 5f), 3f);

            Assert.That(sample.SegmentIndex, Is.EqualTo(0));
            Assert.That(sample.SegmentProgress01, Is.EqualTo(.5f).Within(.001f));
            Assert.That(sample.LateralDistance, Is.EqualTo(0f).Within(.001f));
            Assert.That(sample.IsOnRoad, Is.True);
        }

        [Test]
        public void Sample_ClassifiesPositionOutsideRoadWidthAsOffRoad()
        {
            var track = CreateSquareTrack();
            var sample = TrackBoundaryPolicy.Sample(track.Waypoints, new Vector3(5f, 0f, 5f), 3f);

            Assert.That(sample.SegmentIndex, Is.EqualTo(0));
            Assert.That(sample.LateralDistance, Is.EqualTo(5f).Within(.001f));
            Assert.That(sample.IsOnRoad, Is.False);
        }

        [Test]
        public void Sample_PreservesSignedLateralOrientationOnHorizontalSegment()
        {
            var track = new TrackRuntime();
            AddWaypoint(track, "W0", new Vector3(0f, 0f, 0f));
            AddWaypoint(track, "W1", new Vector3(10f, 0f, 0f));
            AddWaypoint(track, "W2", new Vector3(10f, 0f, 20f));

            var rightOfForward = TrackBoundaryPolicy.Sample(
                track.Waypoints,
                new Vector3(5f, 0f, -2f),
                3f);
            var leftOfForward = TrackBoundaryPolicy.Sample(
                track.Waypoints,
                new Vector3(5f, 0f, 2f),
                3f);

            Assert.That(rightOfForward.SegmentIndex, Is.EqualTo(0));
            Assert.That(rightOfForward.SignedLateralDistance, Is.EqualTo(2f).Within(.001f));
            Assert.That(leftOfForward.SegmentIndex, Is.EqualTo(0));
            Assert.That(leftOfForward.SignedLateralDistance, Is.EqualTo(-2f).Within(.001f));
        }

        [Test]
        public void BuildEdges_CreatesTwoSolidCollidersPerSegment()
        {
            var track = CreateSquareTrack();
            var root = NewObject("Track Edge Root");

            var edges = TrackBoundaryRuntimeBuilder.BuildEdges(track, root.transform, 3f, 1.2f, .4f);

            Assert.That(edges, Has.Count.EqualTo(8));
            foreach (var edge in edges)
            {
                Assert.That(edge, Is.Not.Null);
                Assert.That(edge.isTrigger, Is.False);
                Assert.That(edge.size.x, Is.EqualTo(.4f).Within(.001f));
                Assert.That(edge.size.y, Is.EqualTo(1.2f).Within(.001f));
            }
        }

        [Test]
        public void Monitor_EmitsWhenRacerLeavesAndReentersRoadCorridor()
        {
            var track = CreateSquareTrack();
            var racer = NewObject("Racer");
            racer.transform.position = new Vector3(0f, 0f, 5f);
            var monitor = TrackBoundaryRuntimeBuilder.EnsureMonitor(racer, track, 3f);
            var states = new List<bool>();
            monitor.OffRoadStateChanged += states.Add;

            Assert.That(monitor.IsOffRoad, Is.False);
            racer.transform.position = new Vector3(5f, 0f, 5f);
            monitor.Refresh();
            racer.transform.position = new Vector3(0f, 0f, 5f);
            monitor.Refresh();

            Assert.That(states, Is.EqualTo(new[] { true, false }));
            Assert.That(monitor.IsOffRoad, Is.False);
        }

        [Test]
        public void Sample_RejectsTrackWithoutUsableSegments()
        {
            var track = new TrackRuntime();
            track.Waypoints.Add(NewObject("Only Waypoint").transform);

            Assert.That(
                () => TrackBoundaryPolicy.Sample(track.Waypoints, Vector3.zero, 3f),
                Throws.ArgumentException);
        }

        private TrackRuntime CreateSquareTrack()
        {
            var track = new TrackRuntime();
            AddWaypoint(track, "W0", new Vector3(0f, 0f, 0f));
            AddWaypoint(track, "W1", new Vector3(0f, 0f, 10f));
            AddWaypoint(track, "W2", new Vector3(10f, 0f, 10f));
            AddWaypoint(track, "W3", new Vector3(10f, 0f, 0f));
            return track;
        }

        private void AddWaypoint(TrackRuntime track, string name, Vector3 position)
        {
            var obj = NewObject(name);
            obj.transform.position = position;
            track.Waypoints.Add(obj.transform);
        }

        private GameObject NewObject(string name)
        {
            var obj = new GameObject(name);
            created.Add(obj);
            return obj;
        }
    }
}
