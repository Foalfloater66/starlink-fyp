using UnityEngine;

namespace QuickPrimitives.Scripts
{
    [ExecuteInEditMode]
    public class QcTorusMesh : QcPrimitivesBase
    {
        [System.Serializable]
        public class QcTorusProperties : QcBaseProperties
        {
            public float radius = 1;
            public float ringRadius = 0.2f;

            public bool sliceOn = false;
            public float sliceFrom = 0.0f;
            public float sliceTo = 0.0f;

            public int torusSegments = 24;
            public int ringSegments = 16;

            public void CopyFrom(QcTorusProperties source)
            {
                base.CopyFrom(source);

                radius = source.radius;
                ringRadius = source.ringRadius;

                sliceOn = source.sliceOn;
                sliceFrom = source.sliceFrom;
                sliceTo = source.sliceTo;

                torusSegments = source.torusSegments;
                ringSegments = source.ringSegments;
            }

            public bool Modified(QcTorusProperties source)
            {
                var offsetChanged = offset[0] != source.offset[0] || offset[1] != source.offset[1] ||
                                    offset[2] != source.offset[2];

                return radius != source.radius ||
                       ringRadius != source.ringRadius ||
                       torusSegments != source.torusSegments ||
                       sliceOn != source.sliceOn ||
                       (sliceOn && (sliceFrom != source.sliceFrom || sliceTo != source.sliceTo)) ||
                       ringSegments != source.ringSegments ||
                       genTextureCoords != source.genTextureCoords ||
                       addCollider != source.addCollider ||
                       offsetChanged;
            }
        }

        public QcTorusProperties properties = new QcTorusProperties();

        public void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            BuildGeometry();
        }

        #region BuildGeometry

        protected override void BuildGeometry()
        {
            if (properties.radius <= 0)
                return;

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            ClearVertices();

            GenerateVertices();

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

            meshFilter.sharedMesh.Clear();

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

        #endregion

        #region RebuildGeometry

        public override void RebuildGeometry()
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            ClearVertices();

            GenerateVertices();

            if (properties.offset != Vector3.zero) AddOffset(properties.offset);

            var triangles = new int[faces.Count * 3];
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
                var collider = gameObject.GetComponent<BoxCollider>();
                if (collider == null) collider = gameObject.AddComponent<BoxCollider>();

                collider.enabled = true;
                collider.center = properties.offset;
                collider.size = new Vector3((properties.ringRadius + properties.radius) * 2,
                    properties.ringRadius * 2,
                    (properties.ringRadius + properties.radius) * 2);
            }
            else
            {
                var collider = gameObject.GetComponent<BoxCollider>();
                if (collider != null) collider.enabled = false;
            }
        }

        #region GenerateVertices

        //
        // adapted from http://wiki.unity3d.com/index.php/ProceduralPrimitives
        //
        private void GenerateVertices()
        {
            #region Vertices

            var sideCap = properties.sliceOn &&
                          properties.sliceFrom != properties.sliceTo &&
                          Mathf.Abs(properties.sliceFrom - properties.sliceTo) < 360;

            float partAngle;
            float startAngle = 0;
            float endAngle = 0;
            if (!sideCap)
            {
                partAngle = 2f * Mathf.PI / properties.torusSegments;
            }
            else
            {
                var sliceTo = properties.sliceTo;
                var sliceFrom = properties.sliceFrom;
                if (sliceFrom > sliceTo) sliceTo += 360;
                startAngle = sliceFrom * Mathf.Deg2Rad;
                endAngle = sliceTo * Mathf.Deg2Rad;
                partAngle = (endAngle - startAngle) / properties.torusSegments;
            }

            for (var seg = 0; seg <= properties.torusSegments; ++seg)
            {
                //int currSeg = (seg == properties.torusSegments) ? 0 : seg;

                //float t1 = (float)currSeg / properties.torusSegments * twoPi;
                var t1 = seg * partAngle + startAngle;
                var r1 = new Vector3(Mathf.Cos(t1) * properties.radius, 0f, Mathf.Sin(t1) * properties.radius);

                for (var side = 0; side <= properties.ringSegments; ++side)
                {
                    var currSide = side == properties.ringSegments ? 0 : side;

                    var t2 = (float)currSide / properties.ringSegments * twoPi;
                    var r2 = Quaternion.AngleAxis(-t1 * Mathf.Rad2Deg, Vector3.up) *
                             new Vector3(Mathf.Sin(t2) * properties.ringRadius, Mathf.Cos(t2) * properties.ringRadius);

                    AddVertex(r1 + r2);
                }
            }

            #endregion

            #region Normals

            for (var seg = 0; seg <= properties.torusSegments; ++seg)
            {
                var t1 = seg * partAngle + startAngle;
                var r1 = new Vector3(Mathf.Cos(t1) * properties.radius, 0f, Mathf.Sin(t1) * properties.radius);

                for (var side = 0; side <= properties.ringSegments; ++side)
                    AddNormal((vertices[side + seg * (properties.ringSegments + 1)] - r1).normalized);
            }

            #endregion

            #region UVs

            if (properties.genTextureCoords)
                for (var seg = 0; seg <= properties.torusSegments; ++seg)
                for (var side = 0; side <= properties.ringSegments; ++side)
                    AddUV(new Vector2((float)seg / properties.torusSegments,
                        1 - (float)side / properties.ringSegments));

            if (sideCap)
            {
                // Add caps on both ends
                var t1 = startAngle;
                var r1 = new Vector3(Mathf.Cos(t1) * properties.radius, 0f, Mathf.Sin(t1) * properties.radius);

                AddVertex(r1);

                var circleNormal = Vector3.right;

                for (var side = 0; side <= properties.ringSegments; ++side)
                {
                    var currSide = side == properties.ringSegments ? 0 : side;

                    var t2 = (float)currSide / properties.ringSegments * twoPi;
                    var r2 = Quaternion.AngleAxis(-t1 * Mathf.Rad2Deg, Vector3.up) *
                             new Vector3(Mathf.Sin(t2) * properties.ringRadius, Mathf.Cos(t2) * properties.ringRadius);

                    AddVertex(r1 + r2);
                    if (side == 0)
                    {
                        circleNormal = ComputeNormal(Vector3.zero, r2, r1);
                        AddNormal(circleNormal); // for the vertex r1
                        AddUV(Vector2.zero);
                    }

                    AddNormal(circleNormal);
                    AddUV(Vector2.zero);
                }

                t1 = endAngle;
                r1 = new Vector3(Mathf.Cos(t1) * properties.radius, 0f, Mathf.Sin(t1) * properties.radius);

                AddVertex(r1);

                for (var side = 0; side <= properties.ringSegments; ++side)
                {
                    var currSide = side == properties.ringSegments ? 0 : side;

                    var t2 = (float)currSide / properties.ringSegments * twoPi;
                    var r2 = Quaternion.AngleAxis(-t1 * Mathf.Rad2Deg, Vector3.up) *
                             new Vector3(Mathf.Sin(t2) * properties.ringRadius, Mathf.Cos(t2) * properties.ringRadius);

                    AddVertex(r1 + r2);
                    if (side == 0)
                    {
                        circleNormal = ComputeNormal(Vector3.zero, r1, r2);
                        AddNormal(circleNormal); // for the vertex r1
                        AddUV(Vector2.zero);
                    }

                    AddNormal(circleNormal);
                    AddUV(Vector2.zero);
                }
            }

            #endregion

            #region Triangles

            faces.Clear();

            if (!sideCap)
            {
                for (var seg = 0; seg <= properties.torusSegments; seg++)
                for (var side = 0; side <= properties.ringSegments - 1; side++)
                {
                    var current = side + seg * (properties.ringSegments + 1);
                    var next = side + (seg < properties.torusSegments ? (seg + 1) * (properties.ringSegments + 1) : 0);

                    faces.Add(new TriangleIndices(current, next, next + 1));
                    faces.Add(new TriangleIndices(current, next + 1, current + 1));
                }
            }
            else
            {
                for (var seg = 0; seg < properties.torusSegments; seg++)
                for (var side = 0; side <= properties.ringSegments - 1; side++)
                {
                    var current = side + seg * (properties.ringSegments + 1);
                    var next = side + (seg + 1) * (properties.ringSegments + 1);

                    faces.Add(new TriangleIndices(current, next, next + 1));
                    faces.Add(new TriangleIndices(current, next + 1, current + 1));
                }

                var baseIndex = (properties.torusSegments + 1) * (properties.ringSegments + 1);
                // Add caps on both ends
                for (var side = 0; side <= properties.ringSegments; ++side)
                    faces.Add(new TriangleIndices(baseIndex, baseIndex + side + 0, baseIndex + side + 1));

                baseIndex += properties.ringSegments + 2;
                for (var side = 0; side <= properties.ringSegments; ++side)
                    faces.Add(new TriangleIndices(baseIndex, baseIndex + side + 1, baseIndex + side));
            }

            #endregion
        }

        #endregion
    }
}