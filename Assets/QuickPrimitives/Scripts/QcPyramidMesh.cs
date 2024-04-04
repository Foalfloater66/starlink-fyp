﻿using UnityEngine;

namespace QuickPrimitives.Scripts
{
    [ExecuteInEditMode]
    public class QcPyramidMesh : QcPrimitivesBase
    {
        [System.Serializable]
        public class QcPyramidProperties : QcBaseProperties
        {
            public float width = 1;
            public float depth = 1;
            public float height = 1;

            public int sides = 5;

            public float triangleIncline = 0.5f;
            public void CopyFrom(QcPyramidProperties source)
            {
                base.CopyFrom(source);

                this.width = source.width;
                this.depth = source.depth;
                this.height = source.height;
                this.sides = source.sides;
                this.triangleIncline = source.triangleIncline;
            }

            public bool Modified(QcPyramidProperties source)
            {
                if ((this.width == source.width) && (this.depth == source.depth) && (this.height == source.height) &&
                    (this.sides == source.sides) &&
                    (this.triangleIncline == source.triangleIncline) &&
                    (this.genTextureCoords == source.genTextureCoords) &&
                    (this.addCollider == source.addCollider) &&
                    (this.offset[0] == source.offset[0]) && (this.offset[1] == source.offset[1]) && (this.offset[2] == source.offset[2]))
                {
                        return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public QcPyramidProperties properties = new QcPyramidProperties();

        private float[] xc;
        private float[] yc;

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
            if ((properties.width <= 0) || (properties.depth <= 0) || (properties.height <= 0)) return;

            xc = new float[properties.sides + 1];
            yc = new float[properties.sides + 1];

            if (properties.sides == 3)
            {
                xc[0] = -1;
                yc[0] = -1;
                xc[1] = 1;
                yc[1] = properties.triangleIncline * 2 - 1;
                xc[2] = -1;
                yc[2] = 1;
                xc[3] = -1;
                yc[3] = -1;
            }
            else if (properties.sides == 4)
            {
                xc[0] = -1;
                yc[0] = -1;
                xc[1] = 1;
                yc[1] = -1;
                xc[2] = 1;
                yc[2] = 1;
                xc[3] = -1;
                yc[3] = 1;
                xc[4] = -1;
                yc[4] = -1;
            }
            else
            {
                float partAngle = twoPi / properties.sides;
                for (int i = 0; i <= properties.sides; ++i)
                {
                    float angle = i * partAngle;
                    if (properties.sides % 2 == 1) angle += Mathf.PI * 0.5f;
                    xc[i] = Mathf.Cos(angle);
                    yc[i] = Mathf.Sin(angle);
                }
            }

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            ClearVertices();

            GenerateVertices(properties.height, properties.sides, xc, yc);
            GenerateTriangles(properties.height, properties.sides);

            if (properties.offset != Vector3.zero)
            {
                AddOffset(properties.offset);
            }

            int[] triangles = new int[faces.Count * 3];
            int index = 0;
            foreach (var tri in faces)
            {
                triangles[index] = tri.v1;
                triangles[index + 1] = tri.v2;
                triangles[index + 2] = tri.v3;

                index += 3;
            }

            Mesh mesh = new Mesh();

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
        #endregion

        #region RebuildGeometry
        public override void RebuildGeometry()
        {
            xc = new float[properties.sides + 1];
            yc = new float[properties.sides + 1];

            if (properties.sides == 3)
            {
                xc[0] = -1;
                yc[0] = -1;
                xc[1] = 1;
                yc[1] = properties.triangleIncline * 2 - 1;
                xc[2] = -1;
                yc[2] = 1;
                xc[3] = -1;
                yc[3] = -1;
            }
            else if (properties.sides == 4)
            {
                xc[0] = -1;
                yc[0] = -1;
                xc[1] = 1;
                yc[1] = -1;
                xc[2] = 1;
                yc[2] = 1;
                xc[3] = -1;
                yc[3] = 1;
                xc[4] = -1;
                yc[4] = -1;
            }
            else
            {
                float partAngle = twoPi / properties.sides;
                for (int i = 0; i <= properties.sides; ++i)
                {
                    float angle = i * partAngle;
                    if (properties.sides % 2 == 1) angle += Mathf.PI * 0.5f;
                    xc[i] = Mathf.Cos(angle);
                    yc[i] = Mathf.Sin(angle);
                }
            }


            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

            ClearVertices();

            GenerateVertices(properties.height, properties.sides, xc, yc);
            GenerateTriangles(properties.height, properties.sides);

            if (properties.offset != Vector3.zero)
            {
                AddOffset(properties.offset);
            }

            int[] triangles = new int[faces.Count * 3];
            int index = 0;
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
                BoxCollider collider = gameObject.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = gameObject.AddComponent<BoxCollider>();
                }

                collider.enabled = true;
                collider.center = properties.offset;
                collider.size = new Vector3(properties.width, properties.height, properties.depth);
            }
            else
            {
                BoxCollider collider = gameObject.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        #region GenerateVertices
        private void GenerateVertices(float height, int numCircleSegments, float[] xc, float[] yc)
        {
            float[] x = new float[numCircleSegments + 1];
            float[] y = new float[numCircleSegments + 1];

            float halfWidth = properties.width * 0.5f;
            float halfDepth = properties.depth * 0.5f;
            for (int i = 0; i <= numCircleSegments; ++i)
            {
                x[i] = xc[i] * halfWidth;
                y[i] = yc[i] * halfDepth;
            }

            float[] xn = new float[numCircleSegments + 1];
            float[] yn = new float[numCircleSegments + 1];
            ComputeNormals(numCircleSegments, halfWidth, halfDepth, out xn, out yn);

            Vector3 bottomNormal = Vector3.down;

            AddVertex(new Vector3(0f, -0.5f * height, 0f));       // bottom center 

            AddNormal(bottomNormal);

            AddUV(new Vector2(0.5f, 0.5f));

            for (int i = 0; i <= numCircleSegments; i++)
            {
                // for bottom face
                AddVertex(new Vector3(x[i], -0.5f * height, y[i]));

                // for front faces
                AddVertex(new Vector3(x[i], -0.5f * height, y[i]));
                AddVertex(new Vector3(0, 0.5f * height, 0));

                AddNormal(bottomNormal);

                AddUV(new Vector2((xc[i] + 1.0f) * 0.5f, (yc[i] + 1.0f) * 0.5f));

                AddUV(new Vector2(2 * i / (float)numCircleSegments, 0));
                AddUV(new Vector2((2 * i + 1) / (float)numCircleSegments, 1));

                if ((i > 0) && (i < numCircleSegments))
                {
                    AddVertex(new Vector3(x[i], -0.5f * height, y[i]));
                    AddVertex(new Vector3(0, 0.5f * height, 0));

                    AddUV(new Vector2(2 * i / (float)numCircleSegments, 0));
                    AddUV(new Vector2((2 * i + 1) / (float)numCircleSegments, 1));
                }

                if (i == 0)
                {
                    Vector3 normal1 = new Vector3(xn[i], properties.height, yn[i]);
                    normal1.Normalize();

                    AddNormal(normal1);
                    AddNormal(normal1);

                }
                else if (i == numCircleSegments)
                {
                    Vector3 normal1 = new Vector3(xn[i - 1], properties.height, yn[i - 1]);
                    normal1.Normalize();

                    AddNormal(normal1);
                    AddNormal(normal1);
                }
                else
                {
                    Vector3 normal1 = new Vector3(xn[i - 1], properties.height, yn[i - 1]);
                    normal1.Normalize();

                    AddNormal(normal1);
                    AddNormal(normal1);

                    Vector3 normal2 = new Vector3(xn[i], properties.height, yn[i]);
                    normal2.Normalize();

                    AddNormal(normal2);
                    AddNormal(normal2);
                }
            }
        }
        #endregion

        private void ComputeNormals(int numCircleSegments, float halfWidth, float halfDepth, out float[] xn, out float[] yn)
        {
            xn = new float[numCircleSegments + 1];
            yn = new float[numCircleSegments + 1];

            if (properties.sides == 3)
            {
                xn[0] = properties.triangleIncline;
                yn[0] = -halfWidth / halfDepth;
                xn[1] = 1 - properties.triangleIncline;
                yn[1] = halfWidth / halfDepth;
                xn[2] = -1;
                yn[2] = 0;
                xn[3] = properties.triangleIncline * 2 - 1;
                yn[3] = halfWidth / halfDepth;
            }
            else if (properties.sides == 4)
            {
                xn[0] = 0;
                yn[0] = -1;
                xn[1] = 1;
                yn[1] = 0;
                xn[2] = 0;
                yn[2] = 1;
                xn[3] = -1;
                yn[3] = 0;
                xn[4] = 0;
                yn[4] = -1;
            }
            else
            {
                float partAngle = (2f * Mathf.PI) / numCircleSegments;
                for (int i = 0; i < numCircleSegments; ++i)
                {
                    float angle = i * partAngle + partAngle * 0.5f + Mathf.PI * 0.5f;
                    xn[i] = Mathf.Cos(angle) * halfWidth;
                    yn[i] = Mathf.Sin(angle) * halfDepth;
                }
            }
        }

        #region GenerateTriangles
        private void GenerateTriangles(float height, int numCircleSegments)
        {
            for (int i = 0; i < numCircleSegments; ++i)
            {
                if (i == 0)
                {
                    int base1 = 1;
                    int base2 = 4;

                    // bottom triangles
                    faces.Add(new TriangleIndices(base2, 0, base1));

                    // side triangles
                    faces.Add(new TriangleIndices(base2 + 1, base1 + 1, base1 + 2));
                }
                else if (i == numCircleSegments - 1)
                {
                    int base1 = 4 + 5 * (i - 1);
                    int base2 = 4 + 5 * i;

                    // bottom triangles
                    faces.Add(new TriangleIndices(base2, 0, base1));

                    // side triangles
                    faces.Add(new TriangleIndices(base2 + 1, base1 + 3, base1 + 4));
                }
                else
                {
                    int base1 = 4 + 5 * (i - 1);
                    int base2 = 4 + 5 * i;

                    // bottom triangles
                    faces.Add(new TriangleIndices(base2, 0, base1));

                    // side triangles
                    faces.Add(new TriangleIndices(base2 + 1, base1 + 3, base1 + 4));
                }
            }
        }
        #endregion
    }
}
