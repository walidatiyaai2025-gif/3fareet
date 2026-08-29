using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.Editor
{
    /// <summary>
    /// UART-003 deterministic hard-surface authoring pass for the Afareet King Hero.
    /// This creates the packaged editor-time production mesh used by the Android player;
    /// it is intentionally separate from the runtime primitive fallback in CarFactory.
    /// </summary>
    public static class HeroCarProductionMeshAuthoring
    {
        public enum SurfaceGroup
        {
            Body = 0,
            Glass = 1,
            Wheel = 2,
            GoldTrim = 3,
            Black = 4,
            Spirit = 5
        }

        public sealed class MeshData
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int>[] Triangles =
            {
                new List<int>(), new List<int>(), new List<int>(),
                new List<int>(), new List<int>(), new List<int>()
            };

            public int TriangleCount
            {
                get
                {
                    var count = 0;
                    for (var i = 0; i < Triangles.Length; i++) count += Triangles[i].Count / 3;
                    return count;
                }
            }
        }

        private readonly struct LoftStation
        {
            public readonly float Z;
            public readonly float HalfWidth;
            public readonly float CenterY;
            public readonly float HalfHeight;
            public readonly float Power;

            public LoftStation(float z, float halfWidth, float centerY, float halfHeight, float power)
            {
                Z = z;
                HalfWidth = halfWidth;
                CenterY = centerY;
                HalfHeight = halfHeight;
                Power = power;
            }
        }

        public static MeshData Build(int lod)
        {
            if (lod < 0 || lod > 2) throw new ArgumentOutOfRangeException(nameof(lod));

            var data = new MeshData();
            var bodySegments = lod == 0 ? 28 : lod == 1 ? 20 : 14;
            var wheelMajor = lod == 0 ? 28 : lod == 1 ? 18 : 12;
            var wheelMinor = lod == 0 ? 10 : lod == 1 ? 7 : 5;
            var headlightSegments = lod == 0 ? 14 : lod == 1 ? 10 : 8;

            AddLoft(data, SurfaceGroup.Body, BodyStations(lod), bodySegments);
            AddLoft(data, SurfaceGroup.Glass, CabinStations(lod), Mathf.Max(10, bodySegments / 2));

            AddBox(data, SurfaceGroup.Body, new Vector3(0f, .38f, 2.14f), new Vector3(2.08f, .22f, .28f));
            AddBox(data, SurfaceGroup.Body, new Vector3(0f, .40f, -2.14f), new Vector3(2.04f, .20f, .26f));
            AddBox(data, SurfaceGroup.Black, new Vector3(0f, .20f, 2.22f), new Vector3(2.22f, .09f, .55f));
            AddBox(data, SurfaceGroup.Black, new Vector3(0f, .20f, -2.20f), new Vector3(2.12f, .10f, .50f));
            AddBox(data, SurfaceGroup.GoldTrim, new Vector3(-1.02f, .25f, -.08f), new Vector3(.12f, .15f, 3.45f));
            AddBox(data, SurfaceGroup.GoldTrim, new Vector3(1.02f, .25f, -.08f), new Vector3(.12f, .15f, 3.45f));
            AddBox(data, SurfaceGroup.GoldTrim, new Vector3(0f, .89f, .25f), new Vector3(.24f, .035f, 3.30f));
            AddBox(data, SurfaceGroup.Black, new Vector3(0f, 1.06f, .92f), new Vector3(.86f, .28f, .70f));
            AddBox(data, SurfaceGroup.Black, new Vector3(0f, .48f, 2.28f), new Vector3(1.20f, .28f, .10f));

            // Afareet King identity: oversized spirit wing, gold supports and hood runes.
            AddBox(data, SurfaceGroup.Spirit, new Vector3(0f, 1.52f, -1.90f), new Vector3(2.62f, .13f, .55f));
            AddBox(data, SurfaceGroup.GoldTrim, new Vector3(-.83f, 1.20f, -1.73f), new Vector3(.12f, .57f, .16f));
            AddBox(data, SurfaceGroup.GoldTrim, new Vector3(.83f, 1.20f, -1.73f), new Vector3(.12f, .57f, .16f));
            AddBox(data, SurfaceGroup.Spirit, new Vector3(-.53f, .92f, .88f), new Vector3(.10f, .025f, 1.55f), -.16f);
            AddBox(data, SurfaceGroup.Spirit, new Vector3(.53f, .92f, .88f), new Vector3(.10f, .025f, 1.55f), .16f);
            AddBox(data, SurfaceGroup.GoldTrim, new Vector3(-.38f, .46f, 2.24f), new Vector3(.12f, .34f, .12f));
            AddBox(data, SurfaceGroup.GoldTrim, new Vector3(.38f, .46f, 2.24f), new Vector3(.12f, .34f, .12f));
            AddBox(data, SurfaceGroup.GoldTrim, new Vector3(0f, 1.50f, -.10f), new Vector3(.08f, .025f, 1.55f));

            foreach (var x in new[] { -.58f, .58f })
            {
                AddEllipsoid(data, SurfaceGroup.Spirit, new Vector3(x, .69f, 2.17f), new Vector3(.31f, .12f, .08f), headlightSegments, Mathf.Max(4, headlightSegments / 2));
                AddEllipsoid(data, SurfaceGroup.GoldTrim, new Vector3(x, .69f, 2.23f), new Vector3(.16f, .08f, .055f), Mathf.Max(8, headlightSegments - 2), 4);
                AddBox(data, SurfaceGroup.Spirit, new Vector3(x, .67f, -2.18f), new Vector3(.50f, .14f, .07f));
            }

            foreach (var x in new[] { -1.00f, 1.00f })
            foreach (var z in new[] { -1.36f, 1.36f })
            {
                var center = new Vector3(x, .39f, z);
                AddTorusX(data, SurfaceGroup.Wheel, center, .30f, .12f, wheelMajor, wheelMinor);
                AddCylinderX(data, SurfaceGroup.GoldTrim, center + Vector3.right * (x > 0f ? .035f : -.035f), .23f, .075f, wheelMajor);
                AddCylinderX(data, SurfaceGroup.Spirit, center + Vector3.right * (x > 0f ? .045f : -.045f), .085f, .09f, Mathf.Max(8, wheelMajor / 2));
            }

            AddCylinderZ(data, SurfaceGroup.Black, new Vector3(-.62f, .29f, -2.26f), .10f, .32f, Mathf.Max(8, wheelMajor / 2));
            AddCylinderZ(data, SurfaceGroup.Black, new Vector3(.62f, .29f, -2.26f), .10f, .32f, Mathf.Max(8, wheelMajor / 2));

            return data;
        }

        private static LoftStation[] BodyStations(int lod)
        {
            if (lod == 0)
                return new[]
                {
                    S(-2.22f,.72f,.52f,.27f,2.8f), S(-1.95f,.91f,.55f,.32f,3f), S(-1.55f,1f,.58f,.35f,3.4f),
                    S(-1.05f,1.03f,.60f,.38f,3.7f), S(-.45f,1.04f,.61f,.39f,3.8f), S(.20f,1.04f,.61f,.39f,3.8f),
                    S(.75f,1.02f,.59f,.37f,3.6f), S(1.25f,.99f,.57f,.34f,3.3f), S(1.70f,.91f,.54f,.31f,3f),
                    S(2.05f,.80f,.51f,.27f,2.8f), S(2.25f,.62f,.49f,.22f,2.6f)
                };
            if (lod == 1)
                return new[]
                {
                    S(-2.22f,.72f,.52f,.27f,2.8f), S(-1.8f,.95f,.56f,.33f,3f), S(-1.1f,1.03f,.60f,.38f,3.5f),
                    S(-.25f,1.04f,.61f,.39f,3.7f), S(.65f,1.02f,.59f,.37f,3.5f), S(1.45f,.95f,.55f,.32f,3.1f),
                    S(2.02f,.79f,.51f,.27f,2.8f), S(2.25f,.62f,.49f,.22f,2.6f)
                };
            return new[]
            {
                S(-2.18f,.70f,.52f,.27f,2.7f), S(-1.55f,.99f,.58f,.35f,3f), S(-.55f,1.03f,.60f,.38f,3.2f),
                S(.55f,1.01f,.59f,.36f,3.2f), S(1.55f,.91f,.54f,.31f,2.9f), S(2.18f,.64f,.49f,.23f,2.6f)
            };
        }

        private static LoftStation[] CabinStations(int lod)
        {
            if (lod == 0)
                return new[] { S(-1.05f,.69f,1.03f,.34f,2.6f), S(-.68f,.74f,1.09f,.42f,2.8f), S(-.15f,.76f,1.12f,.46f,3f), S(.38f,.73f,1.09f,.42f,2.8f), S(.82f,.62f,1.02f,.32f,2.6f) };
            if (lod == 1)
                return new[] { S(-1.03f,.68f,1.03f,.34f,2.6f), S(-.45f,.75f,1.10f,.44f,2.8f), S(.20f,.75f,1.11f,.45f,2.9f), S(.78f,.62f,1.02f,.32f,2.6f) };
            return new[] { S(-.98f,.66f,1.03f,.32f,2.5f), S(-.25f,.73f,1.09f,.41f,2.7f), S(.70f,.60f,1.01f,.30f,2.5f) };
        }

        private static LoftStation S(float z, float w, float y, float h, float p) => new LoftStation(z, w, y, h, p);

        private static void AddLoft(MeshData data, SurfaceGroup group, LoftStation[] stations, int segments)
        {
            var start = data.Vertices.Count;
            foreach (var station in stations)
            for (var i = 0; i < segments; i++)
            {
                var a = Mathf.PI * 2f * i / segments;
                var ca = Mathf.Cos(a);
                var sa = Mathf.Sin(a);
                var x = station.HalfWidth * Mathf.Sign(ca) * Mathf.Pow(Mathf.Abs(ca), 2f / station.Power);
                var y = station.CenterY + station.HalfHeight * Mathf.Sign(sa) * Mathf.Pow(Mathf.Abs(sa), 2f / station.Power);
                data.Vertices.Add(new Vector3(x, y, station.Z));
            }

            for (var s = 0; s < stations.Length - 1; s++)
            for (var i = 0; i < segments; i++)
            {
                var j = (i + 1) % segments;
                AddQuad(data, group, start + s * segments + i, start + s * segments + j, start + (s + 1) * segments + j, start + (s + 1) * segments + i);
            }

            var firstCenter = data.Vertices.Count;
            data.Vertices.Add(new Vector3(0f, stations[0].CenterY, stations[0].Z));
            var lastCenter = data.Vertices.Count;
            data.Vertices.Add(new Vector3(0f, stations[stations.Length - 1].CenterY, stations[stations.Length - 1].Z));
            for (var i = 0; i < segments; i++)
            {
                var j = (i + 1) % segments;
                AddTri(data, group, firstCenter, start + j, start + i);
                var a = start + (stations.Length - 1) * segments + i;
                var b = start + (stations.Length - 1) * segments + j;
                AddTri(data, group, lastCenter, a, b);
            }
        }

        private static void AddBox(MeshData data, SurfaceGroup group, Vector3 center, Vector3 size, float yaw = 0f)
        {
            var half = size * .5f;
            var rotation = Quaternion.Euler(0f, yaw * Mathf.Rad2Deg, 0f);
            var start = data.Vertices.Count;
            var corners = new[]
            {
                new Vector3(-half.x,-half.y,-half.z), new Vector3(half.x,-half.y,-half.z),
                new Vector3(half.x,half.y,-half.z), new Vector3(-half.x,half.y,-half.z),
                new Vector3(-half.x,-half.y,half.z), new Vector3(half.x,-half.y,half.z),
                new Vector3(half.x,half.y,half.z), new Vector3(-half.x,half.y,half.z)
            };
            foreach (var corner in corners) data.Vertices.Add(center + rotation * corner);
            AddQuad(data, group, start+0,start+1,start+2,start+3);
            AddQuad(data, group, start+5,start+4,start+7,start+6);
            AddQuad(data, group, start+4,start+0,start+3,start+7);
            AddQuad(data, group, start+1,start+5,start+6,start+2);
            AddQuad(data, group, start+3,start+2,start+6,start+7);
            AddQuad(data, group, start+4,start+5,start+1,start+0);
        }

        private static void AddTorusX(MeshData data, SurfaceGroup group, Vector3 center, float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            var start = data.Vertices.Count;
            for (var i = 0; i < majorSegments; i++)
            {
                var a = Mathf.PI * 2f * i / majorSegments;
                var ca = Mathf.Cos(a);
                var sa = Mathf.Sin(a);
                for (var j = 0; j < minorSegments; j++)
                {
                    var b = Mathf.PI * 2f * j / minorSegments;
                    var radial = majorRadius + minorRadius * Mathf.Cos(b);
                    data.Vertices.Add(center + new Vector3(minorRadius * Mathf.Sin(b), radial * ca, radial * sa));
                }
            }
            for (var i = 0; i < majorSegments; i++)
            for (var j = 0; j < minorSegments; j++)
            {
                var ni = (i + 1) % majorSegments;
                var nj = (j + 1) % minorSegments;
                AddQuad(data, group,
                    start + i * minorSegments + j,
                    start + ni * minorSegments + j,
                    start + ni * minorSegments + nj,
                    start + i * minorSegments + nj);
            }
        }

        private static void AddCylinderX(MeshData data, SurfaceGroup group, Vector3 center, float radius, float depth, int sections)
        {
            var start = data.Vertices.Count;
            for (var side = -1; side <= 1; side += 2)
            for (var i = 0; i < sections; i++)
            {
                var a = Mathf.PI * 2f * i / sections;
                data.Vertices.Add(center + new Vector3(side * depth * .5f, Mathf.Cos(a) * radius, Mathf.Sin(a) * radius));
            }
            var leftCenter = data.Vertices.Count; data.Vertices.Add(center - Vector3.right * depth * .5f);
            var rightCenter = data.Vertices.Count; data.Vertices.Add(center + Vector3.right * depth * .5f);
            for (var i = 0; i < sections; i++)
            {
                var j = (i + 1) % sections;
                AddQuad(data, group, start+i, start+j, start+sections+j, start+sections+i);
                AddTri(data, group, leftCenter, start+j, start+i);
                AddTri(data, group, rightCenter, start+sections+i, start+sections+j);
            }
        }

        private static void AddCylinderZ(MeshData data, SurfaceGroup group, Vector3 center, float radius, float depth, int sections)
        {
            var start = data.Vertices.Count;
            for (var side = -1; side <= 1; side += 2)
            for (var i = 0; i < sections; i++)
            {
                var a = Mathf.PI * 2f * i / sections;
                data.Vertices.Add(center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, side * depth * .5f));
            }
            var backCenter = data.Vertices.Count; data.Vertices.Add(center - Vector3.forward * depth * .5f);
            var frontCenter = data.Vertices.Count; data.Vertices.Add(center + Vector3.forward * depth * .5f);
            for (var i = 0; i < sections; i++)
            {
                var j = (i + 1) % sections;
                AddQuad(data, group, start+i, start+j, start+sections+j, start+sections+i);
                AddTri(data, group, backCenter, start+j, start+i);
                AddTri(data, group, frontCenter, start+sections+i, start+sections+j);
            }
        }

        private static void AddEllipsoid(MeshData data, SurfaceGroup group, Vector3 center, Vector3 radii, int longitude, int latitude)
        {
            var start = data.Vertices.Count;
            for (var lat = 0; lat <= latitude; lat++)
            {
                var v = lat / (float)latitude;
                var phi = Mathf.PI * v;
                var sy = Mathf.Cos(phi);
                var ring = Mathf.Sin(phi);
                for (var lon = 0; lon < longitude; lon++)
                {
                    var theta = Mathf.PI * 2f * lon / longitude;
                    data.Vertices.Add(center + new Vector3(radii.x * ring * Mathf.Cos(theta), radii.y * sy, radii.z * ring * Mathf.Sin(theta)));
                }
            }
            for (var lat = 0; lat < latitude; lat++)
            for (var lon = 0; lon < longitude; lon++)
            {
                var next = (lon + 1) % longitude;
                AddQuad(data, group,
                    start + lat * longitude + lon,
                    start + lat * longitude + next,
                    start + (lat + 1) * longitude + next,
                    start + (lat + 1) * longitude + lon);
            }
        }

        private static void AddQuad(MeshData data, SurfaceGroup group, int a, int b, int c, int d)
        {
            AddTri(data, group, a, b, c);
            AddTri(data, group, a, c, d);
        }

        private static void AddTri(MeshData data, SurfaceGroup group, int a, int b, int c)
        {
            var target = data.Triangles[(int)group];
            target.Add(a); target.Add(b); target.Add(c);
        }
    }
}
