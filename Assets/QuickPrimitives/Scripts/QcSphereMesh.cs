﻿using System.Collections.Generic;
using UnityEngine;

namespace QuickPrimitives.Scripts
{
    [ExecuteInEditMode]
    public class QcSphereMesh : QcPrimitivesBase
    {
        [System.Serializable]
        public class QcSphereProperties : QcBaseProperties
        {
            [System.Serializable]
            public class Icosphere
            {
                public int subdivisions = 2;
            }

            [System.Serializable]
            public class UvSphere
            {
                public int segments = 16;
                public float hemisphere = 0;

                public bool sliceOn = false;
                public float sliceFrom = 0.0f;
                public float sliceTo = 0.0f;
            }

            public enum MeshGenMethod
            {
                Icosphere,
                UVSphere
            }

            public float radius = 0.5f;

            public MeshGenMethod meshGenMethod = MeshGenMethod.Icosphere;
            public Icosphere icosphere = new Icosphere();
            public UvSphere uvSphere = new UvSphere();

            public void CopyFrom(QcSphereProperties source)
            {
                base.CopyFrom(source);

                radius = source.radius;

                icosphere.subdivisions = source.icosphere.subdivisions;
                uvSphere.segments = source.uvSphere.segments;
                uvSphere.hemisphere = source.uvSphere.hemisphere;
                uvSphere.sliceOn = source.uvSphere.sliceOn;
                uvSphere.sliceFrom = source.uvSphere.sliceFrom;
                uvSphere.sliceTo = source.uvSphere.sliceTo;


                meshGenMethod = source.meshGenMethod;
            }

            public bool Modified(QcSphereProperties source)
            {
                return radius != source.radius ||
                       uvSphere.sliceOn != source.uvSphere.sliceOn ||
                       (uvSphere.sliceOn && (uvSphere.sliceFrom != source.uvSphere.sliceFrom ||
                                             uvSphere.sliceTo != source.uvSphere.sliceTo)) ||
                       icosphere.subdivisions != source.icosphere.subdivisions ||
                       offset[0] != source.offset[0] || offset[1] != source.offset[1] ||
                       offset[2] != source.offset[2] ||
                       uvSphere.segments != source.uvSphere.segments ||
                       uvSphere.hemisphere != source.uvSphere.hemisphere ||
                       genTextureCoords != source.genTextureCoords ||
                       addCollider != source.addCollider ||
                       meshGenMethod != source.meshGenMethod;
            }
        }

        public QcSphereProperties properties = new QcSphereProperties();

        private Dictionary<long, int> middlePointIndexCache;

        protected override void ClearVertices()
        {
            base.ClearVertices();

            if (middlePointIndexCache != null)
                middlePointIndexCache.Clear();
            else
                middlePointIndexCache = new Dictionary<long, int>();
        }

        public void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            middlePointIndexCache = new Dictionary<long, int>();

            BuildGeometry();
        }

        protected override void BuildGeometry()
        {
            if (properties.radius <= 0) return;

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            ClearVertices();

            switch (properties.meshGenMethod)
            {
                case QcSphereProperties.MeshGenMethod.UVSphere:
                    GenerateMeshUvSphere();
                    break;

                case QcSphereProperties.MeshGenMethod.Icosphere:
                default:
                    GenerateMeshIcosahedron();
                    break;
            }

            if (properties.offset != Vector3.zero) AddOffset(properties.offset);

            var triangles = new int[faces.Count * 3];
            var ti = 0;
            foreach (var tri in faces)
            {
                triangles[ti] = tri.v1;
                triangles[ti + 1] = tri.v2;
                triangles[ti + 2] = tri.v3;

                ti += 3;
            }

            var mesh = new Mesh();

            meshFilter.sharedMesh = mesh;

            // Assign verts, norms, uvs and tris to mesh and calc normals
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            if (properties.genTextureCoords)
                mesh.uv = uvs.ToArray();
            else
                mesh.uv = null;

            mesh.triangles = triangles;

            SetCollider();

            mesh.RecalculateBounds();
        }

        #region RebuildGeometry

        public override void RebuildGeometry()
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            ClearVertices();

            switch (properties.meshGenMethod)
            {
                case QcSphereProperties.MeshGenMethod.UVSphere:
                    GenerateMeshUvSphere();
                    break;

                case QcSphereProperties.MeshGenMethod.Icosphere:
                default:
                    GenerateMeshIcosahedron();
                    break;
            }

            if (properties.offset != Vector3.zero) AddOffset(properties.offset);

            int[] triangles;

            triangles = new int[faces.Count * 3];
            var index = 0;
            foreach (var tri in faces)
            {
                triangles[index] = tri.v1;
                triangles[index + 1] = tri.v2;
                triangles[index + 2] = tri.v3;

                index += 3;
            }

            // Assign verts, norms, uvs and tris to mesh and calc normals
            if (meshFilter.sharedMesh != null)
            {
                meshFilter.sharedMesh.Clear();

                meshFilter.sharedMesh.vertices = vertices.ToArray();
                meshFilter.sharedMesh.normals = normals.ToArray();
                if (properties.genTextureCoords)
                    meshFilter.sharedMesh.uv = uvs.ToArray();
                else
                    meshFilter.sharedMesh.uv = null;

                meshFilter.sharedMesh.triangles = triangles;

                SetCollider();

                meshFilter.sharedMesh.RecalculateBounds();
            }
        }

        #endregion

        private void SetCollider()
        {
            if (properties.addCollider)
            {
                // set collider bound
                var collider = gameObject.GetComponent<SphereCollider>();
                if (collider == null) collider = gameObject.AddComponent<SphereCollider>();

                collider.enabled = true;
                collider.center = properties.offset;
                collider.radius = properties.radius;
            }
            else
            {
                var collider = gameObject.GetComponent<SphereCollider>();
                if (collider != null) collider.enabled = false;
            }
        }

        #region GenerateMeshIcosahedron

        //
        // code from http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html
        //
        private void GenerateMeshIcosahedron()
        {
            var t = (1.0f + Mathf.Sqrt(5.0f)) * 0.5f;

            var point = new Vector3(-1, t, 0).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(1, t, 0).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(-1, -t, 0).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(1, -t, 0).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));

            point = new Vector3(0, -1, t).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(0, 1, t).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(0, -1, -t).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(0, 1, -t).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));

            point = new Vector3(t, 0, -1).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(t, 0, 1).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(-t, 0, -1).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));
            point = new Vector3(-t, 0, 1).normalized;
            AddVertex(point * properties.radius, point,
                new Vector2(Mathf.Atan2(point.z, point.x) * oneOver2PI, point.y * 0.5f + 0.5f));

            // create 20 triangles of the icosahedron
            faces.Clear();

            // 5 faces around point 0
            faces.Add(new TriangleIndices(0, 11, 5));
            faces.Add(new TriangleIndices(0, 5, 1));
            faces.Add(new TriangleIndices(0, 1, 7));
            faces.Add(new TriangleIndices(0, 7, 10));
            faces.Add(new TriangleIndices(0, 10, 11));

            // 5 adjacent faces 
            faces.Add(new TriangleIndices(1, 5, 9));
            faces.Add(new TriangleIndices(5, 11, 4));
            faces.Add(new TriangleIndices(11, 10, 2));
            faces.Add(new TriangleIndices(10, 7, 6));
            faces.Add(new TriangleIndices(7, 1, 8));

            // 5 faces around point 3
            faces.Add(new TriangleIndices(3, 9, 4));
            faces.Add(new TriangleIndices(3, 4, 2));
            faces.Add(new TriangleIndices(3, 2, 6));
            faces.Add(new TriangleIndices(3, 6, 8));
            faces.Add(new TriangleIndices(3, 8, 9));

            // 5 adjacent faces 
            faces.Add(new TriangleIndices(4, 9, 5));
            faces.Add(new TriangleIndices(2, 4, 11));
            faces.Add(new TriangleIndices(6, 2, 10));
            faces.Add(new TriangleIndices(8, 6, 7));
            faces.Add(new TriangleIndices(9, 8, 1));

            for (var i = 0; i < properties.icosphere.subdivisions; i++)
            {
                var faces2 = new List<TriangleIndices>();
                foreach (var tri in faces)
                {
                    // replace triangle by 4 triangles
                    var a = GetMiddlePoint(tri.v1, tri.v2);
                    var b = GetMiddlePoint(tri.v2, tri.v3);
                    var c = GetMiddlePoint(tri.v3, tri.v1);
                    ;

                    faces2.Add(new TriangleIndices(tri.v1, a, c));
                    faces2.Add(new TriangleIndices(tri.v2, b, a));
                    faces2.Add(new TriangleIndices(tri.v3, c, b));
                    faces2.Add(new TriangleIndices(a, b, c));
                }

                faces = faces2;
            }
        }

        #endregion

        #region GenerateMeshUvSphere

        private void GenerateMeshUvSphere()
        {
            if (properties.uvSphere.hemisphere > 0)
                GenerateMeshUvHemiSphere();
            else
                GenerateMeshUvFullSphere();
        }

        #endregion

        #region GenerateMeshUvFullSphere

        //
        // code adapted from http://wiki.unity3d.com/index.php/ProceduralPrimitives
        //
        private void GenerateMeshUvFullSphere()
        {
            //AddVertex(Vector3.up * properties.radius);      // top vertex (north pole)

            var segments = properties.uvSphere.segments;

            var sideCap = properties.uvSphere.sliceOn &&
                          properties.uvSphere.sliceFrom != properties.uvSphere.sliceTo &&
                          Mathf.Abs(properties.uvSphere.sliceFrom - properties.uvSphere.sliceTo) < 360;

            float partAngle;
            float startAngle = 0;
            float endAngle = 0;
            if (!sideCap)
            {
                partAngle = 2f * Mathf.PI / segments;
            }
            else
            {
                var sliceTo = properties.uvSphere.sliceTo;
                var sliceFrom = properties.uvSphere.sliceFrom;
                if (sliceFrom > sliceTo) sliceTo += 360;
                startAngle = sliceFrom * Mathf.Deg2Rad;
                endAngle = sliceTo * Mathf.Deg2Rad;
                partAngle = (endAngle - startAngle) / segments;
            }

            for (var lat = 0; lat <= segments; lat++)
            {
                var a1 = Mathf.PI * (float)lat / segments;
                var sin1 = Mathf.Sin(a1);
                var cos1 = Mathf.Cos(a1);

                for (var lon = 0; lon <= segments; lon++)
                {
                    var a2 = lon * partAngle + startAngle;
                    var sin2 = Mathf.Sin(a2);
                    var cos2 = Mathf.Cos(a2);

                    AddVertex(new Vector3(sin1 * cos2, cos1, sin1 * sin2) * properties.radius);
                }
            }

            for (var n = 0; n < vertices.Count; n++) AddNormal(vertices[n].normalized);

            if (properties.genTextureCoords)
            {
                //AddUV(Vector2.up);
                for (var lon = 0; lon <= segments; lon++) AddUV(new Vector2(((float)lon + 0.5f) / segments, 1f));

                for (var lat = 1; lat < segments; lat++)
                for (var lon = 0; lon <= segments; lon++)
                    AddUV(new Vector2((float)lon / segments, 1f - (float)lat / segments));
                //AddUV(Vector2.zero);
                for (var lon = 0; lon <= segments; lon++) AddUV(new Vector2(((float)lon + 0.5f) / segments, 0f));
            }

            if (sideCap)
            {
                var sin2s = Mathf.Sin(startAngle);
                var cos2s = Mathf.Cos(startAngle);

                var sin2e = Mathf.Sin(endAngle);
                var cos2e = Mathf.Cos(endAngle);

                var a1n = Mathf.PI * 0.5f;
                var sin1n = Mathf.Sin(a1n);
                var cos1n = Mathf.Cos(a1n);

                var capNormal1 = ComputeNormal(new Vector3(sin1n * cos2s, cos1n, sin1n * sin2s),
                    new Vector3(0, -properties.radius, 0),
                    new Vector3(0, properties.radius, 0));
                var capNormal2 = ComputeNormal(new Vector3(sin1n * cos2e, cos1n, sin1n * sin2e),
                    new Vector3(0, properties.radius, 0),
                    new Vector3(0, -properties.radius, 0));

                AddVertex(Vector3.up * properties.radius);
                AddNormal(capNormal1);
                AddUV(Vector2.zero);

                AddVertex(Vector3.up * properties.radius);
                AddNormal(capNormal2);
                AddUV(Vector2.zero);

                for (var lat = 0; lat < segments; lat++)
                {
                    var a1 = Mathf.PI * (float)(lat + 1) / (segments + 1);
                    var sin1 = Mathf.Sin(a1);
                    var cos1 = Mathf.Cos(a1);

                    AddVertex(new Vector3(sin1 * cos2s, cos1, sin1 * sin2s) * properties.radius);
                    AddNormal(capNormal1);
                    AddUV(Vector2.zero);

                    AddVertex(new Vector3(0, cos1, 0) * properties.radius);
                    AddNormal(capNormal1);
                    AddUV(Vector2.zero);

                    AddVertex(new Vector3(sin1 * cos2e, cos1, sin1 * sin2e) * properties.radius);
                    AddNormal(capNormal2);
                    AddUV(Vector2.zero);

                    AddVertex(new Vector3(0, cos1, 0) * properties.radius);
                    AddNormal(capNormal2);
                    AddUV(Vector2.zero);
                }


                AddVertex(Vector3.down * properties.radius);
                AddNormal(capNormal1);
                AddUV(Vector2.zero);

                AddVertex(Vector3.down * properties.radius);
                AddNormal(capNormal2);
                AddUV(Vector2.zero);
            }

            #region Triangles

            faces.Clear();

            //Middle

            //Top Cap
            for (var lon = 0; lon < segments; lon++)
            {
                var current = lon;
                var next = current + segments + 1;

                faces.Add(new TriangleIndices(next + 1, next, current));
            }

            for (var lat = 1; lat < segments - 1; lat++)
            for (var lon = 0; lon < segments; lon++)
            {
                var current = lon + lat * (segments + 1);
                var next = current + segments + 1;

                faces.Add(new TriangleIndices(current, current + 1, next + 1));
                faces.Add(new TriangleIndices(current, next + 1, next));
            }

            //Bottom Cap
            for (var lon = 0; lon < segments; lon++)
            {
                var current = lon + (segments - 1) * (segments + 1);
                var next = current + segments + 1;

                faces.Add(new TriangleIndices(current, current + 1, next));
            }

            if (sideCap)
            {
                // side faces
                var baseIndex = (segments + 1) * (segments + 1);
                faces.Add(new TriangleIndices(baseIndex + 0, baseIndex + 1, baseIndex + 2));
                faces.Add(new TriangleIndices(baseIndex + 3, baseIndex + 2, baseIndex + 1));

                baseIndex += 2;
                for (var lat = 0; lat < segments - 1; lat++)
                {
                    faces.Add(new TriangleIndices(baseIndex + 1, baseIndex + 0, baseIndex + 5));
                    faces.Add(new TriangleIndices(baseIndex + 5, baseIndex + 0, baseIndex + 4));

                    faces.Add(new TriangleIndices(baseIndex + 3, baseIndex + 7, baseIndex + 2));
                    faces.Add(new TriangleIndices(baseIndex + 2, baseIndex + 7, baseIndex + 6));
                    baseIndex += 4;
                }

                faces.Add(new TriangleIndices(baseIndex + 1, baseIndex + 0, baseIndex + 4));
                faces.Add(new TriangleIndices(baseIndex + 2, baseIndex + 3, baseIndex + 5));
            }

            #endregion
        }

        #endregion

        #region GenerateMeshUvHemiSphere

        private void GenerateMeshUvHemiSphere()
        {
            var segments = properties.uvSphere.segments;

            var sideCap = properties.uvSphere.sliceOn &&
                          properties.uvSphere.sliceFrom != properties.uvSphere.sliceTo &&
                          Mathf.Abs(properties.uvSphere.sliceFrom - properties.uvSphere.sliceTo) < 360;

            float partAngle;
            float startAngle = 0;
            float endAngle = 0;
            if (!sideCap)
            {
                partAngle = 2f * Mathf.PI / segments;
            }
            else
            {
                var sliceTo = properties.uvSphere.sliceTo;
                var sliceFrom = properties.uvSphere.sliceFrom;
                if (sliceFrom > sliceTo) sliceTo += 360;
                startAngle = sliceFrom * Mathf.Deg2Rad;
                endAngle = sliceTo * Mathf.Deg2Rad;
                partAngle = (endAngle - startAngle) / segments;
            }

            var baseAngle = Mathf.PI * properties.uvSphere.hemisphere;
            var vSegments = (int)(segments * (1 - properties.uvSphere.hemisphere));

            var a0 = (Mathf.PI - baseAngle) * 1.0f / (vSegments + 1) + baseAngle;
            var sin0 = Mathf.Sin(a0);
            var cos0 = Mathf.Cos(a0);

            for (var lon = 0; lon <= segments; lon++) // for top center
                AddVertex(new Vector3(0, cos0 * properties.radius, 0));

            for (var lon = 0; lon <= segments; lon++) // for top perimeter
            {
                //float a2 = twoPi * (float)(lon % segments) / segments;
                var a2 = partAngle * lon + startAngle;
                var sin2 = Mathf.Sin(a2);
                var cos2 = Mathf.Cos(a2);

                AddVertex(new Vector3(sin0 * cos2, cos0, sin0 * sin2) * properties.radius);
            }

            for (var lat = 0; lat < vSegments; lat++) // for 
            {
                var a1 = (Mathf.PI - baseAngle) * (float)(lat + 1) / (vSegments + 1) + baseAngle;
                var sin1 = Mathf.Sin(a1);
                var cos1 = Mathf.Cos(a1);

                for (var lon = 0; lon <= segments; lon++)
                {
                    var a2 = partAngle * lon + startAngle;
                    var sin2 = Mathf.Sin(a2);
                    var cos2 = Mathf.Cos(a2);

                    AddVertex(new Vector3(sin1 * cos2, cos1, sin1 * sin2) * properties.radius);
                }
            }

            for (var lon = 0; lon <= segments; lon++) // south pole
                AddVertex(Vector3.down * properties.radius);

            for (var n = 0; n <= segments * 2; n++) AddNormal(Vector3.up);

            for (var n = segments * 2 + 1; n < vertices.Count; n++) AddNormal(vertices[n].normalized);

            var h = properties.uvSphere.hemisphere > 0.5
                ? properties.uvSphere.hemisphere - 0.5f
                : 0.5f - properties.uvSphere.hemisphere;
            var hRadius = Mathf.Sqrt(0.25f - h * h);

            for (var lon = 0; lon <= segments; lon++)
                AddUV(new Vector2(((float)lon + 0.5f) / segments, 1 - hRadius * 0.5f));

            for (var lon = 0; lon <= segments; lon++) AddUV(new Vector2((float)lon / segments, 1.0f));

            for (var lat = 0; lat < vSegments; lat++)
            for (var lon = 0; lon <= segments; lon++)
                AddUV(new Vector2((float)lon / segments, 1f - (float)lat / vSegments));

            for (var lon = 0; lon <= segments; lon++) AddUV(new Vector2(((float)lon + 0.5f) / segments, 0f));

            if (sideCap)
            {
                var sin2s = Mathf.Sin(startAngle);
                var cos2s = Mathf.Cos(startAngle);

                var sin2e = Mathf.Sin(endAngle);
                var cos2e = Mathf.Cos(endAngle);

                var a1n = Mathf.PI * 0.5f;
                var sin1n = Mathf.Sin(a1n);
                var cos1n = Mathf.Cos(a1n);

                var capNormal1 = ComputeNormal(new Vector3(sin1n * cos2s, cos1n, sin1n * sin2s),
                    new Vector3(0, -properties.radius, 0),
                    new Vector3(0, properties.radius, 0));
                var capNormal2 = ComputeNormal(new Vector3(sin1n * cos2e, cos1n, sin1n * sin2e),
                    new Vector3(0, properties.radius, 0),
                    new Vector3(0, -properties.radius, 0));

                AddVertex(new Vector3(0, cos0 * properties.radius, 0));
                AddNormal(capNormal1);
                AddUV(Vector2.zero);

                AddVertex(new Vector3(0, cos0 * properties.radius, 0));
                AddNormal(capNormal2);
                AddUV(Vector2.zero);

                for (var lat = 0; lat < vSegments; lat++)
                {
                    var a1 = (Mathf.PI - baseAngle) * (float)(lat + 1) / (vSegments + 1) + baseAngle;
                    var sin1 = Mathf.Sin(a1);
                    var cos1 = Mathf.Cos(a1);

                    AddVertex(new Vector3(sin1 * cos2s, cos1, sin1 * sin2s) * properties.radius);
                    AddNormal(capNormal1);
                    AddUV(Vector2.zero);

                    AddVertex(new Vector3(0, cos1, 0) * properties.radius);
                    AddNormal(capNormal1);
                    AddUV(Vector2.zero);

                    AddVertex(new Vector3(sin1 * cos2e, cos1, sin1 * sin2e) * properties.radius);
                    AddNormal(capNormal2);
                    AddUV(Vector2.zero);

                    AddVertex(new Vector3(0, cos1, 0) * properties.radius);
                    AddNormal(capNormal2);
                    AddUV(Vector2.zero);
                }


                AddVertex(-Vector3.up * properties.radius);
                AddNormal(capNormal1);
                AddUV(Vector2.zero);

                AddVertex(-Vector3.up * properties.radius);
                AddNormal(capNormal2);
                AddUV(Vector2.zero);
            }

            #region Triangles

            faces.Clear();

            //Top Cap
            for (var lon = 0; lon < segments; lon++)
            {
                var current = lon;
                var next = current + segments + 1;

                faces.Add(new TriangleIndices(next + 1, next, current));
            }

            //Middle
            var baseIndex = (segments + 1) * 2;
            for (var lat = 0; lat < vSegments - 1; lat++)
            {
                for (var lon = 0; lon < segments; lon++)
                {
                    var current = lon + baseIndex;
                    var next = current + segments + 1;

                    faces.Add(new TriangleIndices(current, current + 1, next + 1));
                    faces.Add(new TriangleIndices(current, next + 1, next));
                }

                baseIndex += segments + 1;
            }

            //Bottom Cap
            for (var lon = 0; lon < segments; lon++)
            {
                var current = baseIndex + lon;
                var next = current + segments + 1;

                faces.Add(new TriangleIndices(current, current + 1, next));
            }

            if (sideCap)
            {
                baseIndex += (segments + 1) * 2 + 2;

                faces.Add(new TriangleIndices(baseIndex + 0, baseIndex + 1, baseIndex - 2));
                faces.Add(new TriangleIndices(baseIndex + 3, baseIndex + 2, baseIndex - 1));

                for (var lat = 0; lat < vSegments - 1; lat++)
                {
                    faces.Add(new TriangleIndices(baseIndex + 1, baseIndex + 0, baseIndex + 5));
                    faces.Add(new TriangleIndices(baseIndex + 5, baseIndex + 0, baseIndex + 4));

                    faces.Add(new TriangleIndices(baseIndex + 3, baseIndex + 7, baseIndex + 2));
                    faces.Add(new TriangleIndices(baseIndex + 2, baseIndex + 7, baseIndex + 6));
                    baseIndex += 4;
                }

                faces.Add(new TriangleIndices(baseIndex + 1, baseIndex + 0, baseIndex + 4));
                faces.Add(new TriangleIndices(baseIndex + 2, baseIndex + 3, baseIndex + 5));
            }

            #endregion
        }

        #endregion

        // return index of point in the middle of p1 and p2
        private int GetMiddlePoint(int p1, int p2)
        {
            // first check if we have it already
            var firstIsSmaller = p1 < p2;
            long smallerIndex = firstIsSmaller ? p1 : p2;
            long greaterIndex = firstIsSmaller ? p2 : p1;
            var key = (smallerIndex << 32) + greaterIndex;

            int ret;
            if (middlePointIndexCache.TryGetValue(key, out ret)) return ret;

            // not in cache, calculate it
            var point1 = GetVertex(p1);
            var point2 = GetVertex(p2);
            var middle = new Vector3((point1.x + point2.x) * 0.5f, (point1.y + point2.y) * 0.5f,
                (point1.z + point2.z) * 0.5f);

            var middlePt = middle.normalized * properties.radius;

            // add vertex makes sure point is on unit sphere
            var index = AddVertex(middlePt);
            AddNormal(middlePt.normalized);
            AddUV(new Vector2(Mathf.Atan2(middlePt.z, middlePt.x) * oneOver2PI, middle.normalized.y * 0.5f + 0.5f));

            // store it, return index
            middlePointIndexCache.Add(key, index);
            return index;
        }


        private void OnValidate()
        {
            if (properties.radius <= 0) properties.radius = 1;

            RebuildGeometry();
        }
    }
}