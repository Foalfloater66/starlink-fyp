using System.Collections.Generic;
using UnityEngine;

namespace QuickPrimitives.Scripts
{
    [ExecuteInEditMode]
    public class QcStairMesh : QcPrimitivesBase
    {
        [System.Serializable]
        public class QcStairProperties : QcBaseProperties
        {
            public enum Types
            {
                Box,
                Closed,
                Open
            }

            public float width = 1;
            public float depth = 1;
            public float height = 1;

            public int steps = 5;

            public float treadDepth = 1;
            public float treadThickness = 1;

            public Types type = new Types();

            public bool spiral = false;

            public float innerRadius = 0;

            //public float outerRadius = 0;
            public bool conical = false;
            public float radius = 0.0f;
            public float rotations = 1;

            public enum WindingDirection
            {
                Clockwise,
                Counterclockwise
            };

            public WindingDirection windingDirection = WindingDirection.Counterclockwise;

            //public bool textureWrapped;

            public void CopyFrom(QcStairProperties source)
            {
                base.CopyFrom(source);

                width = source.width;
                height = source.height;
                depth = source.depth;

                steps = source.steps;
                treadDepth = source.treadDepth;
                treadThickness = source.treadThickness;

                spiral = source.spiral;
                innerRadius = source.innerRadius;
                conical = source.conical;
                radius = source.radius;
                //this.outerRadius = source.outerRadius;
                rotations = source.rotations;
                windingDirection = source.windingDirection;

                type = source.type;

                //this.textureWrapped = source.textureWrapped;
            }

            public bool Modified(QcStairProperties source)
            {
                return width != source.width ||
                       height != source.height ||
                       depth != source.depth ||
                       steps != source.steps ||
                       treadDepth != source.treadDepth ||
                       treadThickness != source.treadThickness ||
                       spiral != source.spiral ||
                       (spiral &&
                        (innerRadius != source.innerRadius ||
                         conical != source.conical ||
                         radius != source.radius ||
                         rotations != source.rotations ||
                         //(this.outerRadius != source.outerRadius) ||
                         windingDirection != source.windingDirection)) ||
                       offset[0] != source.offset[0] ||
                       offset[1] != source.offset[1] ||
                       offset[2] != source.offset[2] ||
                       genTextureCoords != source.genTextureCoords ||
                       addCollider != source.addCollider ||
                       type != source.type;
            }
        }

        public QcStairProperties properties = new QcStairProperties();

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
            if (properties.width <= 0 || properties.height <= 0 || properties.depth <= 0) return;

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            ClearVertices();

            GenerateGeometry();

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

        #endregion

        #region RebuildGeometry

        public override void RebuildGeometry()
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();

            ClearVertices();

            GenerateGeometry();

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
                if (!properties.spiral)
                {
                    // set collider bound
                    var collider = gameObject.GetComponent<BoxCollider>();
                    if (collider == null) collider = gameObject.AddComponent<BoxCollider>();

                    collider.enabled = true;
                    collider.center = properties.offset + new Vector3(0, properties.height * 0.5f, 0);
                    collider.size = new Vector3(properties.width, properties.height, properties.depth);

                    var oldCollider = gameObject.GetComponent<CapsuleCollider>();
                    if (oldCollider != null) oldCollider.enabled = false;
                }
                else
                {
                    var collider = gameObject.GetComponent<CapsuleCollider>();
                    if (collider == null) collider = gameObject.AddComponent<CapsuleCollider>();

                    collider.enabled = true;
                    collider.center = properties.offset + new Vector3(0, properties.height * 0.5f, 0);
                    collider.radius = properties.innerRadius + properties.width;
                    collider.height = properties.height;

                    var oldCollider = gameObject.GetComponent<BoxCollider>();
                    if (oldCollider != null) oldCollider.enabled = false;
                }
            }
            else
            {
                var collider = gameObject.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }
        }

        private void GenerateGeometry()
        {
            if (!properties.spiral)
                switch (properties.type)
                {
                    case QcStairProperties.Types.Box:
                    default:
                        GenerateVerticesBox();
                        GenerateTrianglesBox();
                        break;

                    case QcStairProperties.Types.Closed:
                        GenerateVerticesClosed();
                        GenerateTrianglesClosed();
                        break;

                    case QcStairProperties.Types.Open:
                        GenerateVerticesOpen();
                        GenerateTrianglesOpen();
                        break;
                }
            else
                switch (properties.type)
                {
                    case QcStairProperties.Types.Box:
                        GenerateVerticesSpiralBox();
                        break;

                    case QcStairProperties.Types.Closed:
                        GenerateVerticesSpiralClosed();
                        break;

                    case QcStairProperties.Types.Open:
                        GenerateVerticesSpiralOpen();
                        GenerateTrianglesOpen();
                        break;
                }
        }

        #region GenerateVerticesBox

        private void GenerateVerticesBox()
        {
            var width = properties.width;
            var height = properties.height;
            var depth = properties.depth;

            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;

            AddVertex(new Vector3(-halfWidth, 0, halfDepth)); // bottom face
            AddVertex(new Vector3(halfWidth, 0, halfDepth));
            AddVertex(new Vector3(halfWidth, 0, -halfDepth));
            AddVertex(new Vector3(-halfWidth, 0, -halfDepth));

            AddVertex(new Vector3(-halfWidth, height, halfDepth)); // back face
            AddVertex(new Vector3(halfWidth, height, halfDepth));
            AddVertex(new Vector3(halfWidth, 0, halfDepth));
            AddVertex(new Vector3(-halfWidth, 0, halfDepth));

            AddVertex(new Vector3(-halfWidth, 0, -halfDepth)); // left face
            AddVertex(new Vector3(-halfWidth, height, halfDepth));
            AddVertex(new Vector3(-halfWidth, 0, halfDepth));

            AddVertex(new Vector3(halfWidth, 0, -halfDepth)); // right face
            AddVertex(new Vector3(halfWidth, 0, halfDepth));
            AddVertex(new Vector3(halfWidth, height, halfDepth));

            AddNormal(Vector3.down);
            AddNormal(Vector3.down);
            AddNormal(Vector3.down);
            AddNormal(Vector3.down);

            AddNormal(Vector3.forward);
            AddNormal(Vector3.forward);
            AddNormal(Vector3.forward);
            AddNormal(Vector3.forward);

            AddNormal(Vector3.left);
            AddNormal(Vector3.left);
            AddNormal(Vector3.left);

            AddNormal(Vector3.right);
            AddNormal(Vector3.right);
            AddNormal(Vector3.right);

            AddUV(new Vector2(0, 0));
            AddUV(new Vector2(1, 0));
            AddUV(new Vector2(1, 1));
            AddUV(new Vector2(0, 1));

            AddUV(new Vector2(0, 1));
            AddUV(new Vector2(1, 1));
            AddUV(new Vector2(1, 0));
            AddUV(new Vector2(0, 0));

            AddUV(new Vector2(1, 0));
            AddUV(new Vector2(0, 1));
            AddUV(new Vector2(0, 0));

            AddUV(new Vector2(0, 0));
            AddUV(new Vector2(1, 0));
            AddUV(new Vector2(1, 1));

            var stepHeight = height / properties.steps;
            var stepDepth = depth / properties.steps;

            for (var i = 0; i < properties.steps; ++i)
            {
                var stepBaseHeight = i * stepHeight;
                var stepBaseDepth = i * stepDepth - halfDepth;

                AddVertex(new Vector3(-halfWidth, stepBaseHeight, stepBaseDepth)); // step front
                AddVertex(new Vector3(halfWidth, stepBaseHeight, stepBaseDepth));
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));
                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));

                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth)); // step top
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + stepDepth));
                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + stepDepth));

                AddVertex(new Vector3(-halfWidth, stepBaseHeight, stepBaseDepth)); // left cap
                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));
                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + stepDepth));

                AddVertex(new Vector3(halfWidth, stepBaseHeight, stepBaseDepth)); // roght cap
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + stepDepth));

                AddNormal(Vector3.back);
                AddNormal(Vector3.back);
                AddNormal(Vector3.back);
                AddNormal(Vector3.back);

                AddNormal(Vector3.up);
                AddNormal(Vector3.up);
                AddNormal(Vector3.up);
                AddNormal(Vector3.up);

                AddNormal(Vector3.left);
                AddNormal(Vector3.left);
                AddNormal(Vector3.left);

                AddNormal(Vector3.right);
                AddNormal(Vector3.right);
                AddNormal(Vector3.right);

                AddUV(new Vector2(0, (float)i / properties.steps));
                AddUV(new Vector2(1, (float)i / properties.steps));
                AddUV(new Vector2(1, (i + 0.5f) / properties.steps));
                AddUV(new Vector2(0, (i + 0.5f) / properties.steps));

                AddUV(new Vector2(0, (i + 0.5f) / properties.steps));
                AddUV(new Vector2(1, (i + 0.5f) / properties.steps));
                AddUV(new Vector2(1, (i + 1.0f) / properties.steps));
                AddUV(new Vector2(0, (i + 1.0f) / properties.steps));

                var currentDepth = i * stepDepth;
                AddUV(new Vector2((depth - currentDepth) / depth, stepBaseHeight / height));
                AddUV(new Vector2((depth - currentDepth) / depth, (stepBaseHeight + stepHeight) / height));
                AddUV(new Vector2((depth - currentDepth - stepDepth) / depth, (stepBaseHeight + stepHeight) / height));

                AddUV(new Vector2(currentDepth / depth, stepBaseHeight / height));
                AddUV(new Vector2(currentDepth / depth, (stepBaseHeight + stepHeight) / height));
                AddUV(new Vector2((currentDepth + stepDepth) / depth, (stepBaseHeight + stepHeight) / height));
            }
        }

        #endregion

        #region GenerateVerticesClosed

        private void GenerateVerticesClosed()
        {
            var width = properties.width;
            var height = properties.height;
            var depth = properties.depth;

            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;

            var stepHeight = height / properties.steps;
            var stepDepth = depth / properties.steps;

            AddVertex(new Vector3(-halfWidth, 0, -halfDepth + stepDepth)); // bottom face
            AddVertex(new Vector3(halfWidth, 0, -halfDepth + stepDepth));
            AddVertex(new Vector3(halfWidth, 0, -halfDepth));
            AddVertex(new Vector3(-halfWidth, 0, -halfDepth));

            AddVertex(new Vector3(-halfWidth, 0, -halfDepth + stepDepth)); // slanted bottom face
            AddVertex(new Vector3(halfWidth, 0, -halfDepth + stepDepth));
            AddVertex(new Vector3(halfWidth, height - stepHeight, halfDepth));
            AddVertex(new Vector3(-halfWidth, height - stepHeight, halfDepth));

            AddVertex(new Vector3(-halfWidth, height, halfDepth)); // back face
            AddVertex(new Vector3(halfWidth, height, halfDepth));
            AddVertex(new Vector3(halfWidth, height - stepHeight, halfDepth));
            AddVertex(new Vector3(-halfWidth, height - stepHeight, halfDepth));

            AddVertex(new Vector3(-halfWidth, 0, -halfDepth + stepDepth)); // left face
            AddVertex(new Vector3(-halfWidth, 0, -halfDepth));
            AddVertex(new Vector3(-halfWidth, height, halfDepth));
            AddVertex(new Vector3(-halfWidth, height - stepHeight, halfDepth));

            AddVertex(new Vector3(halfWidth, 0, -halfDepth + stepDepth)); // right face
            AddVertex(new Vector3(halfWidth, 0, -halfDepth));
            AddVertex(new Vector3(halfWidth, height, halfDepth));
            AddVertex(new Vector3(halfWidth, height - stepHeight, halfDepth));

            AddNormal(Vector3.down);
            AddNormal(Vector3.down);
            AddNormal(Vector3.down);
            AddNormal(Vector3.down);

            var slantedVector = new Vector3(0, -depth, height);
            slantedVector.Normalize();

            AddNormal(slantedVector);
            AddNormal(slantedVector);
            AddNormal(slantedVector);
            AddNormal(slantedVector);

            AddNormal(Vector3.forward);
            AddNormal(Vector3.forward);
            AddNormal(Vector3.forward);
            AddNormal(Vector3.forward);

            AddNormal(Vector3.left);
            AddNormal(Vector3.left);
            AddNormal(Vector3.left);
            AddNormal(Vector3.left);

            AddNormal(Vector3.right);
            AddNormal(Vector3.right);
            AddNormal(Vector3.right);
            AddNormal(Vector3.right);

            var bottomLength = Mathf.Sqrt((depth - stepDepth) * (depth - stepDepth) +
                                          (height - stepHeight) * (height - stepHeight))
                               + stepDepth + stepHeight;
            AddUV(new Vector2(1, stepDepth / bottomLength));
            AddUV(new Vector2(0, stepDepth / bottomLength));
            AddUV(new Vector2(0, 0));
            AddUV(new Vector2(1, 0));

            AddUV(new Vector2(1, stepDepth / bottomLength));
            AddUV(new Vector2(0, stepDepth / bottomLength));
            AddUV(new Vector2(0, (bottomLength - stepHeight) / bottomLength));
            AddUV(new Vector2(1, (bottomLength - stepHeight) / bottomLength));

            AddUV(new Vector2(1, 1));
            AddUV(new Vector2(0, 1));
            AddUV(new Vector2(0, (bottomLength - stepHeight) / bottomLength));
            AddUV(new Vector2(1, (bottomLength - stepHeight) / bottomLength));

            AddUV(new Vector2((depth - stepDepth) / depth, 0));
            AddUV(new Vector2(1, 0));
            AddUV(new Vector2(0, 1));
            AddUV(new Vector2(0, (height - stepHeight) / height));

            AddUV(new Vector2(stepDepth / depth, 0));
            AddUV(new Vector2(0, 0));
            AddUV(new Vector2(1, 1));
            AddUV(new Vector2(1, (height - stepHeight) / height));

            for (var i = 0; i < properties.steps; ++i)
            {
                var stepBaseHeight = i * stepHeight;
                var stepBaseDepth = i * stepDepth - halfDepth;

                AddVertex(new Vector3(-halfWidth, stepBaseHeight, stepBaseDepth)); // step front
                AddVertex(new Vector3(halfWidth, stepBaseHeight, stepBaseDepth));
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));
                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));

                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth)); // step top
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + stepDepth));
                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + stepDepth));

                AddVertex(new Vector3(-halfWidth, stepBaseHeight, stepBaseDepth)); // left cap
                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));
                AddVertex(new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + stepDepth));

                AddVertex(new Vector3(halfWidth, stepBaseHeight, stepBaseDepth)); // roght cap
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth));
                AddVertex(new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + stepDepth));

                AddNormal(Vector3.back);
                AddNormal(Vector3.back);
                AddNormal(Vector3.back);
                AddNormal(Vector3.back);

                AddNormal(Vector3.up);
                AddNormal(Vector3.up);
                AddNormal(Vector3.up);
                AddNormal(Vector3.up);

                AddNormal(Vector3.left);
                AddNormal(Vector3.left);
                AddNormal(Vector3.left);

                AddNormal(Vector3.right);
                AddNormal(Vector3.right);
                AddNormal(Vector3.right);

                AddUV(new Vector2(0, (float)i / properties.steps));
                AddUV(new Vector2(1, (float)i / properties.steps));
                AddUV(new Vector2(1, (i + 0.5f) / properties.steps));
                AddUV(new Vector2(0, (i + 0.5f) / properties.steps));

                AddUV(new Vector2(0, (i + 0.5f) / properties.steps));
                AddUV(new Vector2(1, (i + 0.5f) / properties.steps));
                AddUV(new Vector2(1, (i + 1.0f) / properties.steps));
                AddUV(new Vector2(0, (i + 1.0f) / properties.steps));

                var currentDepth = i * stepDepth;
                AddUV(new Vector2((depth - currentDepth) / depth, stepBaseHeight / height));
                AddUV(new Vector2((depth - currentDepth) / depth, (stepBaseHeight + stepHeight) / height));
                AddUV(new Vector2((depth - currentDepth - stepDepth) / depth, (stepBaseHeight + stepHeight) / height));

                AddUV(new Vector2(currentDepth / depth, stepBaseHeight / height));
                AddUV(new Vector2(currentDepth / depth, (stepBaseHeight + stepHeight) / height));
                AddUV(new Vector2((currentDepth + stepDepth) / depth, (stepBaseHeight + stepHeight) / height));
            }
        }

        #endregion

        #region GenerateVerticesOpen

        private void GenerateVerticesOpen()
        {
            var halfWidth = properties.width * 0.5f;
            var halfDepth = properties.depth * 0.5f;

            var stepHeight = properties.height / properties.steps;
            var stepDepth = properties.depth / properties.steps;
            var treadThickness = properties.treadThickness;
            var treadDepth = properties.treadDepth;

            for (var i = 0; i < properties.steps; ++i)
            {
                var stepBaseHeight = i * stepHeight;
                var stepBaseDepth = i * stepDepth - halfDepth;

                var pts = new Vector3[8];
                pts[0] = new Vector3(-halfWidth, stepBaseHeight + stepHeight - treadThickness, stepBaseDepth);
                pts[1] = new Vector3(halfWidth, stepBaseHeight + stepHeight - treadThickness, stepBaseDepth);
                pts[2] = new Vector3(halfWidth, stepBaseHeight + stepHeight - treadThickness,
                    stepBaseDepth + treadDepth);
                pts[3] = new Vector3(-halfWidth, stepBaseHeight + stepHeight - treadThickness,
                    stepBaseDepth + treadDepth);

                pts[4] = new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth);
                pts[5] = new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth);
                pts[6] = new Vector3(halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + treadDepth);
                pts[7] = new Vector3(-halfWidth, stepBaseHeight + stepHeight, stepBaseDepth + treadDepth);

                AddVertex(pts[3]); // step bottom
                AddVertex(pts[2]);
                AddVertex(pts[1]);
                AddVertex(pts[0]);

                AddVertex(pts[0]); // step front
                AddVertex(pts[1]);
                AddVertex(pts[5]);
                AddVertex(pts[4]);

                AddVertex(pts[4]); // step top
                AddVertex(pts[5]);
                AddVertex(pts[6]);
                AddVertex(pts[7]);

                AddVertex(pts[7]); // step back
                AddVertex(pts[6]);
                AddVertex(pts[2]);
                AddVertex(pts[3]);

                AddVertex(pts[3]); // step left
                AddVertex(pts[0]);
                AddVertex(pts[4]);
                AddVertex(pts[7]);

                AddVertex(pts[1]); // step right
                AddVertex(pts[2]);
                AddVertex(pts[6]);
                AddVertex(pts[5]);

                AddNormal(Vector3.down);
                AddNormal(Vector3.down);
                AddNormal(Vector3.down);
                AddNormal(Vector3.down);

                AddNormal(Vector3.back);
                AddNormal(Vector3.back);
                AddNormal(Vector3.back);
                AddNormal(Vector3.back);

                AddNormal(Vector3.up);
                AddNormal(Vector3.up);
                AddNormal(Vector3.up);
                AddNormal(Vector3.up);

                AddNormal(Vector3.forward);
                AddNormal(Vector3.forward);
                AddNormal(Vector3.forward);
                AddNormal(Vector3.forward);

                AddNormal(Vector3.left);
                AddNormal(Vector3.left);
                AddNormal(Vector3.left);
                AddNormal(Vector3.left);

                AddNormal(Vector3.right);
                AddNormal(Vector3.right);
                AddNormal(Vector3.right);
                AddNormal(Vector3.right);

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));

                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));
                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));
            }
        }

        #endregion

        #region GenerateVerticesSpiralBox

        private void GenerateVerticesSpiralBox()
        {
            var stepHeight = properties.height / properties.steps;

            var innerRadius0 = properties.innerRadius;
            var outerRadius0 = innerRadius0 + properties.width;
            var innerRadius1 = innerRadius0;
            var outerRadius1 = outerRadius0;

            var rotations = properties.rotations;

            var pts = new List<Vector3>();
            for (var i = 0; i < properties.steps; ++i)
            {
                var stepBaseHeight = i * stepHeight;

                var angle0 = properties.windingDirection == QcStairProperties.WindingDirection.Counterclockwise
                    ? twoPi * rotations / properties.steps * i
                    : -twoPi * rotations / properties.steps * i;
                var angle1 = properties.windingDirection == QcStairProperties.WindingDirection.Counterclockwise
                    ? twoPi * rotations / properties.steps * (i + 1)
                    : -twoPi * rotations / properties.steps * (i + 1);

                if (properties.conical)
                {
                    innerRadius0 = properties.innerRadius +
                                   (float)i * (properties.radius - properties.innerRadius) / (properties.steps - 1);
                    outerRadius0 = innerRadius0 + properties.width;
                    innerRadius1 = properties.innerRadius + (float)(i + 1) *
                        (properties.radius - properties.innerRadius) / (properties.steps - 1);
                    outerRadius1 = innerRadius1 + properties.width;
                }

                pts.Clear();

                pts.Add(new Vector3(innerRadius0 * Mathf.Cos(angle0), stepBaseHeight,
                    innerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(innerRadius1 * Mathf.Cos(angle1), stepBaseHeight,
                    innerRadius1 * Mathf.Sin(angle1)));
                pts.Add(new Vector3(innerRadius0 * Mathf.Cos(angle0), stepBaseHeight + stepHeight,
                    innerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(innerRadius1 * Mathf.Cos(angle1), stepBaseHeight + stepHeight,
                    innerRadius1 * Mathf.Sin(angle1)));

                pts.Add(new Vector3(outerRadius0 * Mathf.Cos(angle0), stepBaseHeight,
                    outerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(outerRadius1 * Mathf.Cos(angle1), stepBaseHeight,
                    outerRadius1 * Mathf.Sin(angle1)));
                pts.Add(new Vector3(outerRadius0 * Mathf.Cos(angle0), stepBaseHeight + stepHeight,
                    outerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(outerRadius1 * Mathf.Cos(angle1), stepBaseHeight + stepHeight,
                    outerRadius1 * Mathf.Sin(angle1)));

                pts.Add(new Vector3(innerRadius0 * Mathf.Cos(angle0), 0, innerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(innerRadius1 * Mathf.Cos(angle1), 0, innerRadius1 * Mathf.Sin(angle1)));

                pts.Add(new Vector3(outerRadius0 * Mathf.Cos(angle0), 0, outerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(outerRadius1 * Mathf.Cos(angle1), 0, outerRadius1 * Mathf.Sin(angle1)));

                if (properties.windingDirection == QcStairProperties.WindingDirection.Clockwise)
                {
                    AddVertex(pts[8]); // back(inner)
                    AddVertex(pts[9]);
                    AddVertex(pts[3]);
                    AddVertex(pts[2]);

                    AddVertex(pts[11]); // front(outer)
                    AddVertex(pts[10]);
                    AddVertex(pts[6]);
                    AddVertex(pts[7]);

                    AddVertex(pts[2]); // top
                    AddVertex(pts[3]);
                    AddVertex(pts[7]);
                    AddVertex(pts[6]);

                    AddVertex(pts[8]); // bottom
                    AddVertex(pts[10]);
                    AddVertex(pts[11]);
                    AddVertex(pts[9]);

                    AddVertex(pts[4]); // right
                    AddVertex(pts[0]);
                    AddVertex(pts[2]);
                    AddVertex(pts[6]);


                    var normal0Out = new Vector3(pts[4].x, 0f, pts[4].z);
                    normal0Out.Normalize();
                    var normal0In = new Vector3(-pts[4].x, 0f, -pts[4].z);
                    normal0In.Normalize();

                    var normal1Out = new Vector3(pts[5].x, 0f, pts[5].z);
                    normal0Out.Normalize();
                    var normal1In = new Vector3(-pts[5].x, 0f, -pts[5].z);
                    normal0In.Normalize();

                    AddNormal(normal0In);
                    AddNormal(normal1In);
                    AddNormal(normal1In);
                    AddNormal(normal0In);

                    AddNormal(normal1Out);
                    AddNormal(normal0Out);
                    AddNormal(normal0Out);
                    AddNormal(normal1Out);

                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);

                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);

                    var capNormal1 = ComputeNormal(pts[2], pts[0], pts[4]);

                    AddNormal(capNormal1);
                    AddNormal(capNormal1);
                    AddNormal(capNormal1);
                    AddNormal(capNormal1);

                    var nSteps = properties.steps;
                    AddUV(new Vector2((float)i / nSteps, 0));
                    AddUV(new Vector2((i + 1f) / nSteps, 0));
                    AddUV(new Vector2((i + 1f) / nSteps, (i + 1f) / nSteps));
                    AddUV(new Vector2((float)i / nSteps, (i + 1f) / nSteps));

                    AddUV(new Vector2(1 - (i + 1f) / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, (i + 1f) / nSteps));
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, (i + 1f) / nSteps));

                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1.0f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1.0f) / nSteps, 0));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 0));

                    AddUV(new Vector2(1 - (float)i / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, 0));

                    AddUV(new Vector2(1 - (float)i / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 0));

                    if (i == properties.steps - 1)
                    {
                        AddVertex(pts[9]); // left
                        AddVertex(pts[11]);
                        AddVertex(pts[7]);
                        AddVertex(pts[3]);

                        var capNormal2 = ComputeNormal(pts[7], pts[5], pts[1]);

                        AddNormal(capNormal2);
                        AddNormal(capNormal2);
                        AddNormal(capNormal2);
                        AddNormal(capNormal2);

                        AddUV(new Vector2(0, 0));
                        AddUV(new Vector2(1, 0));
                        AddUV(new Vector2(1, 1));
                        AddUV(new Vector2(0, 1));
                    }
                }
                else // counterclockwise
                {
                    AddVertex(pts[9]); // back(inner)
                    AddVertex(pts[8]);
                    AddVertex(pts[2]);
                    AddVertex(pts[3]);

                    AddVertex(pts[10]); // front(outer)
                    AddVertex(pts[11]);
                    AddVertex(pts[7]);
                    AddVertex(pts[6]);

                    AddVertex(pts[6]); // top
                    AddVertex(pts[7]);
                    AddVertex(pts[3]);
                    AddVertex(pts[2]);

                    AddVertex(pts[10]); // bottom
                    AddVertex(pts[8]);
                    AddVertex(pts[9]);
                    AddVertex(pts[11]);

                    AddVertex(pts[0]); // left
                    AddVertex(pts[4]);
                    AddVertex(pts[6]);
                    AddVertex(pts[2]);


                    var normal0Out = new Vector3(pts[4].x, 0f, pts[4].z);
                    normal0Out.Normalize();
                    var normal0In = new Vector3(-pts[4].x, 0f, -pts[4].z);
                    normal0In.Normalize();

                    var normal1Out = new Vector3(pts[5].x, 0f, pts[5].z);
                    normal0Out.Normalize();
                    var normal1In = new Vector3(-pts[5].x, 0f, -pts[5].z);
                    normal0In.Normalize();

                    AddNormal(normal1In);
                    AddNormal(normal0In);
                    AddNormal(normal0In);
                    AddNormal(normal1In);

                    AddNormal(normal0Out);
                    AddNormal(normal1Out);
                    AddNormal(normal1Out);
                    AddNormal(normal0Out);

                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);

                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);

                    var capNormal1 = ComputeNormal(pts[2], pts[0], pts[4]);

                    AddNormal(capNormal1);
                    AddNormal(capNormal1);
                    AddNormal(capNormal1);
                    AddNormal(capNormal1);

                    var nSteps = properties.steps;
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, (i + 1f) / nSteps));
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, (i + 1f) / nSteps));

                    AddUV(new Vector2((float)i / nSteps, 0));
                    AddUV(new Vector2((i + 1f) / nSteps, 0));
                    AddUV(new Vector2((i + 1f) / nSteps, (i + 1f) / nSteps));
                    AddUV(new Vector2((float)i / nSteps, (i + 1f) / nSteps));

                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1.0f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1.0f) / nSteps, 0));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 0));


                    AddUV(new Vector2(1 - (float)i / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, 0));

                    AddUV(new Vector2(1 - (float)i / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 0));

                    if (i == properties.steps - 1)
                    {
                        AddVertex(pts[11]); // right
                        AddVertex(pts[9]);
                        AddVertex(pts[3]);
                        AddVertex(pts[7]);

                        var capNormal2 = ComputeNormal(pts[7], pts[5], pts[1]);

                        AddNormal(capNormal2);
                        AddNormal(capNormal2);
                        AddNormal(capNormal2);
                        AddNormal(capNormal2);

                        AddUV(new Vector2(0, 0));
                        AddUV(new Vector2(1, 0));
                        AddUV(new Vector2(1, 1));
                        AddUV(new Vector2(0, 1));
                    }
                }


                for (var j = 0; j < 6; ++j)
                {
                    var bi = i * 20 + j * 4;
                    faces.Add(new TriangleIndices(bi + 1, bi + 0, bi + 2));
                    faces.Add(new TriangleIndices(bi + 2, bi + 0, bi + 3));
                }

                var ci = (i + 1) * 20;
                faces.Add(new TriangleIndices(ci + 1, ci + 0, ci + 2));
                faces.Add(new TriangleIndices(ci + 2, ci + 0, ci + 3));
            }
        }

        #endregion

        #region GenerateVerticesSpiralClosed

        private void GenerateVerticesSpiralClosed()
        {
            var stepHeight = properties.height / properties.steps;

            var innerRadius0 = properties.innerRadius;
            var outerRadius0 = innerRadius0 + properties.width;
            var innerRadius1 = innerRadius0;
            var outerRadius1 = outerRadius0;

            var rotations = properties.rotations;

            var pts = new List<Vector3>();
            for (var i = 0; i < properties.steps; ++i)
            {
                var stepBaseHeight = i * stepHeight;

                var angle0 = properties.windingDirection == QcStairProperties.WindingDirection.Counterclockwise
                    ? twoPi * rotations / properties.steps * i
                    : -twoPi * rotations / properties.steps * i;
                var angle1 = properties.windingDirection == QcStairProperties.WindingDirection.Counterclockwise
                    ? twoPi * rotations / properties.steps * (i + 1)
                    : -twoPi * rotations / properties.steps * (i + 1);

                if (properties.conical)
                {
                    innerRadius0 = properties.innerRadius +
                                   (float)i * (properties.radius - properties.innerRadius) / (properties.steps - 1);
                    outerRadius0 = innerRadius0 + properties.width;
                    innerRadius1 = properties.innerRadius + (float)(i + 1) *
                        (properties.radius - properties.innerRadius) / (properties.steps - 1);
                    outerRadius1 = innerRadius1 + properties.width;
                }

                pts.Clear();

                pts.Add(new Vector3(innerRadius0 * Mathf.Cos(angle0), stepBaseHeight,
                    innerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(innerRadius1 * Mathf.Cos(angle1), stepBaseHeight,
                    innerRadius1 * Mathf.Sin(angle1)));
                pts.Add(new Vector3(innerRadius0 * Mathf.Cos(angle0), stepBaseHeight + stepHeight,
                    innerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(innerRadius1 * Mathf.Cos(angle1), stepBaseHeight + stepHeight,
                    innerRadius1 * Mathf.Sin(angle1)));

                pts.Add(new Vector3(outerRadius0 * Mathf.Cos(angle0), stepBaseHeight,
                    outerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(outerRadius1 * Mathf.Cos(angle1), stepBaseHeight,
                    outerRadius1 * Mathf.Sin(angle1)));
                pts.Add(new Vector3(outerRadius0 * Mathf.Cos(angle0), stepBaseHeight + stepHeight,
                    outerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(outerRadius1 * Mathf.Cos(angle1), stepBaseHeight + stepHeight,
                    outerRadius1 * Mathf.Sin(angle1)));

                pts.Add(new Vector3(innerRadius0 * Mathf.Cos(angle0), stepBaseHeight - stepHeight,
                    innerRadius0 * Mathf.Sin(angle0)));
                pts.Add(new Vector3(outerRadius0 * Mathf.Cos(angle0), stepBaseHeight - stepHeight,
                    outerRadius0 * Mathf.Sin(angle0)));

                if (properties.windingDirection == QcStairProperties.WindingDirection.Clockwise)
                {
                    if (i == 0)
                        AddVertex(pts[0]); // back(inner)
                    else
                        AddVertex(pts[8]); // back(inner)
                    AddVertex(pts[1]);
                    AddVertex(pts[3]);
                    AddVertex(pts[2]);

                    AddVertex(pts[5]); // front(outer)
                    if (i == 0)
                        AddVertex(pts[4]);
                    else
                        AddVertex(pts[9]);
                    AddVertex(pts[6]);
                    AddVertex(pts[7]);

                    AddVertex(pts[2]); // top
                    AddVertex(pts[3]);
                    AddVertex(pts[7]);
                    AddVertex(pts[6]);

                    if (i == 0) // bottom
                    {
                        AddVertex(pts[0]);
                        AddVertex(pts[4]);
                    }
                    else
                    {
                        AddVertex(pts[8]);
                        AddVertex(pts[9]);
                    }

                    AddVertex(pts[5]);
                    AddVertex(pts[1]);

                    AddVertex(pts[4]); // right
                    AddVertex(pts[0]);
                    AddVertex(pts[2]);
                    AddVertex(pts[6]);

                    var normal0Out = new Vector3(pts[4].x, 0f, pts[4].z);
                    normal0Out.Normalize();
                    var normal0In = new Vector3(-pts[4].x, 0f, -pts[4].z);
                    normal0In.Normalize();

                    var normal1Out = new Vector3(pts[5].x, 0f, pts[5].z);
                    normal1Out.Normalize();
                    var normal1In = new Vector3(-pts[5].x, 0f, -pts[5].z);
                    normal1In.Normalize();

                    AddNormal(normal0In);
                    AddNormal(normal1In);
                    AddNormal(normal1In);
                    AddNormal(normal0In);

                    AddNormal(normal1Out);
                    AddNormal(normal0Out);
                    AddNormal(normal0Out);
                    AddNormal(normal1Out);

                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);

                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);

                    var capNormal1 = ComputeNormal(pts[2], pts[0], pts[4]);

                    AddNormal(capNormal1);
                    AddNormal(capNormal1);
                    AddNormal(capNormal1);
                    AddNormal(capNormal1);

                    var nSteps = properties.steps;
                    if (i == 0)
                    {
                        AddUV(new Vector2((float)i / nSteps, (float)i / nSteps));
                        AddUV(new Vector2((i + 1f) / nSteps, (float)i / nSteps));
                    }
                    else
                    {
                        AddUV(new Vector2((float)i / nSteps, (i - 1f) / nSteps));
                        AddUV(new Vector2((i + 1f) / nSteps, (float)i / nSteps));
                    }

                    AddUV(new Vector2((i + 1f) / nSteps, (i + 1f) / nSteps));
                    AddUV(new Vector2((float)i / nSteps, (i + 1f) / nSteps));

                    if (i == 0)
                    {
                        AddUV(new Vector2(1 - (i + 1f) / nSteps, (float)i / nSteps));
                        AddUV(new Vector2(1 - (float)i / nSteps, (float)i / nSteps));
                    }
                    else
                    {
                        AddUV(new Vector2(1 - (i + 1f) / nSteps, (float)i / nSteps));
                        AddUV(new Vector2(1 - (float)i / nSteps, (i - 1f) / nSteps));
                    }

                    AddUV(new Vector2(1 - (float)i / nSteps, (i + 1f) / nSteps));
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, (i + 1f) / nSteps));

                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1.0f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1.0f) / nSteps, 0));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 0));

                    AddUV(new Vector2(1 - (float)i / (nSteps + 1), 0));
                    AddUV(new Vector2(1 - (float)i / (nSteps + 1), 1));
                    AddUV(new Vector2(1 - (i + 1f) / (nSteps + 1), 1));
                    AddUV(new Vector2(1 - (i + 1f) / (nSteps + 1), 0));

                    AddUV(new Vector2(1 - (float)i / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 0));

                    if (i == properties.steps - 1)
                    {
                        AddVertex(pts[1]); // left
                        AddVertex(pts[5]);
                        AddVertex(pts[7]);
                        AddVertex(pts[3]);

                        var capNormal2 = ComputeNormal(pts[7], pts[5], pts[1]);

                        AddNormal(capNormal2);
                        AddNormal(capNormal2);
                        AddNormal(capNormal2);
                        AddNormal(capNormal2);

                        AddUV(new Vector2(1.0f / (nSteps + 1), 0));
                        AddUV(new Vector2(1.0f / (nSteps + 1), 1));
                        AddUV(new Vector2(0, 1));
                        AddUV(new Vector2(0, 0));
                    }
                }
                else // counterclockwise
                {
                    AddVertex(pts[1]); // back(inner)
                    if (i == 0)
                        AddVertex(pts[0]);
                    else
                        AddVertex(pts[8]);
                    AddVertex(pts[2]);
                    AddVertex(pts[3]);

                    if (i == 0)
                        AddVertex(pts[4]); // front(outer)
                    else
                        AddVertex(pts[9]);
                    AddVertex(pts[5]);
                    AddVertex(pts[7]);
                    AddVertex(pts[6]);

                    AddVertex(pts[6]); // top
                    AddVertex(pts[7]);
                    AddVertex(pts[3]);
                    AddVertex(pts[2]);

                    if (i == 0)
                    {
                        AddVertex(pts[4]); // bottom
                        AddVertex(pts[0]);
                        AddVertex(pts[1]);
                        AddVertex(pts[5]);
                    }
                    else
                    {
                        AddVertex(pts[9]); // bottom
                        AddVertex(pts[8]);
                        AddVertex(pts[1]);
                        AddVertex(pts[5]);
                    }

                    AddVertex(pts[0]); // left
                    AddVertex(pts[4]);
                    AddVertex(pts[6]);
                    AddVertex(pts[2]);

                    var normal0Out = new Vector3(pts[4].x, 0f, pts[4].z);
                    normal0Out.Normalize();
                    var normal0In = new Vector3(-pts[4].x, 0f, -pts[4].z);
                    normal0In.Normalize();

                    var normal1Out = new Vector3(pts[5].x, 0f, pts[5].z);
                    normal0Out.Normalize();
                    var normal1In = new Vector3(-pts[5].x, 0f, -pts[5].z);
                    normal0In.Normalize();

                    AddNormal(normal1In);
                    AddNormal(normal0In);
                    AddNormal(normal0In);
                    AddNormal(normal1In);

                    AddNormal(normal0Out);
                    AddNormal(normal1Out);
                    AddNormal(normal1Out);
                    AddNormal(normal0Out);

                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);
                    AddNormal(Vector3.up);

                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);
                    AddNormal(Vector3.down);

                    var capNormal1 = ComputeNormal(pts[6], pts[4], pts[0]);

                    AddNormal(capNormal1);
                    AddNormal(capNormal1);
                    AddNormal(capNormal1);
                    AddNormal(capNormal1);

                    var nSteps = properties.steps;
                    if (i == 0)
                    {
                        AddUV(new Vector2(1 - (i + 1f) / nSteps, (float)i / nSteps));
                        AddUV(new Vector2(1 - (float)i / nSteps, (float)i / nSteps));
                    }
                    else
                    {
                        AddUV(new Vector2(1 - (i + 1f) / nSteps, (float)i / nSteps));
                        AddUV(new Vector2(1 - (float)i / nSteps, (i - 1f) / nSteps));
                    }

                    AddUV(new Vector2(1 - (float)i / nSteps, (i + 1f) / nSteps));
                    AddUV(new Vector2(1 - (i + 1f) / nSteps, (i + 1f) / nSteps));

                    if (i == 0)
                    {
                        AddUV(new Vector2((float)i / nSteps, (float)i / nSteps));
                        AddUV(new Vector2((i + 1f) / nSteps, (float)i / nSteps));
                    }
                    else
                    {
                        AddUV(new Vector2((float)i / nSteps, (i - 1f) / nSteps));
                        AddUV(new Vector2((i + 1f) / nSteps, (float)i / nSteps));
                    }

                    AddUV(new Vector2((i + 1f) / nSteps, (i + 1f) / nSteps));
                    AddUV(new Vector2((float)i / nSteps, (i + 1f) / nSteps));

                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1.0f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 1.0f) / nSteps, 0));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 0));

                    AddUV(new Vector2(1 - (float)i / (nSteps + 1), 0));
                    AddUV(new Vector2(1 - (float)i / (nSteps + 1), 1));
                    AddUV(new Vector2(1 - (i + 1f) / (nSteps + 1), 1));
                    AddUV(new Vector2(1 - (i + 1f) / (nSteps + 1), 0));

                    AddUV(new Vector2(1 - (float)i / nSteps, 0));
                    AddUV(new Vector2(1 - (float)i / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 1));
                    AddUV(new Vector2(1 - (i + 0.5f) / nSteps, 0));

                    if (i == properties.steps - 1)
                    {
                        AddVertex(pts[5]); // right
                        AddVertex(pts[1]);
                        AddVertex(pts[3]);
                        AddVertex(pts[7]);

                        var capNormal2 = ComputeNormal(pts[3], pts[1], pts[5]);

                        AddNormal(capNormal2);
                        AddNormal(capNormal2);
                        AddNormal(capNormal2);
                        AddNormal(capNormal2);

                        AddUV(new Vector2(1.0f / (nSteps + 1), 0));
                        AddUV(new Vector2(1.0f / (nSteps + 1), 1));
                        AddUV(new Vector2(0, 1));
                        AddUV(new Vector2(0, 0));
                    }
                }

                for (var j = 0; j < 5; ++j)
                {
                    var bi = i * 20 + j * 4;
                    faces.Add(new TriangleIndices(bi + 1, bi + 0, bi + 2));
                    faces.Add(new TriangleIndices(bi + 2, bi + 0, bi + 3));
                }

                var ci = i * 20 + 20;
                faces.Add(new TriangleIndices(ci + 1, ci + 0, ci + 2));
                faces.Add(new TriangleIndices(ci + 2, ci + 0, ci + 3));
            }
        }

        #endregion

        #region GenerateVerticesSpiralOpen

        private void GenerateVerticesSpiralOpen()
        {
            var stepHeight = properties.height / properties.steps;
            var treadThickness = properties.treadThickness;
            var treadDepth = properties.treadDepth;

            var rotations = properties.rotations;

            var innerRadius = properties.innerRadius;
            var outerRadius = innerRadius + properties.width;

            for (var i = 0; i < properties.steps; ++i)
            {
                var stepBaseHeight = i * stepHeight;
                var stepHalfDepth = treadDepth * 0.5f;

                if (properties.conical)
                {
                    innerRadius = properties.innerRadius +
                                  (float)i * (properties.radius - properties.innerRadius) / (properties.steps - 1);
                    outerRadius = innerRadius + properties.width;
                }

                var pts = new Vector3[8];
                pts[0] = new Vector3(innerRadius, stepBaseHeight + stepHeight - treadThickness, -stepHalfDepth);
                pts[1] = new Vector3(outerRadius, stepBaseHeight + stepHeight - treadThickness, -stepHalfDepth);
                pts[2] = new Vector3(outerRadius, stepBaseHeight + stepHeight - treadThickness, stepHalfDepth);
                pts[3] = new Vector3(innerRadius, stepBaseHeight + stepHeight - treadThickness, stepHalfDepth);

                pts[4] = new Vector3(innerRadius, stepBaseHeight + stepHeight, -stepHalfDepth);
                pts[5] = new Vector3(outerRadius, stepBaseHeight + stepHeight, -stepHalfDepth);
                pts[6] = new Vector3(outerRadius, stepBaseHeight + stepHeight, stepHalfDepth);
                pts[7] = new Vector3(innerRadius, stepBaseHeight + stepHeight, stepHalfDepth);

                var rotDegree = properties.windingDirection == QcStairProperties.WindingDirection.Clockwise
                    ? 360.0f * rotations / properties.steps * i
                    : -360.0f * rotations / properties.steps * i;
                //float rotRad = rotDegree * Mathf.Deg2Rad;
                for (var pi = 0; pi < 8; ++pi) pts[pi] = Quaternion.Euler(0, rotDegree, 0) * pts[pi];

                AddVertex(pts[3]); // step bottom
                AddVertex(pts[2]);
                AddVertex(pts[1]);
                AddVertex(pts[0]);

                AddVertex(pts[0]); // step front
                AddVertex(pts[1]);
                AddVertex(pts[5]);
                AddVertex(pts[4]);

                AddVertex(pts[4]); // step top
                AddVertex(pts[5]);
                AddVertex(pts[6]);
                AddVertex(pts[7]);

                AddVertex(pts[7]); // step back
                AddVertex(pts[6]);
                AddVertex(pts[2]);
                AddVertex(pts[3]);

                AddVertex(pts[3]); // step left
                AddVertex(pts[0]);
                AddVertex(pts[4]);
                AddVertex(pts[7]);

                AddVertex(pts[1]); // step right
                AddVertex(pts[2]);
                AddVertex(pts[6]);
                AddVertex(pts[5]);

                AddNormal(Vector3.down);
                AddNormal(Vector3.down);
                AddNormal(Vector3.down);
                AddNormal(Vector3.down);


                var frontNormal = Quaternion.Euler(0, rotDegree, 0) * Vector3.back;
                AddNormal(frontNormal);
                AddNormal(frontNormal);
                AddNormal(frontNormal);
                AddNormal(frontNormal);

                AddNormal(Vector3.up);
                AddNormal(Vector3.up);
                AddNormal(Vector3.up);
                AddNormal(Vector3.up);

                var backNormal = Quaternion.Euler(0, rotDegree, 0) * Vector3.forward;
                AddNormal(backNormal);
                AddNormal(backNormal);
                AddNormal(backNormal);
                AddNormal(backNormal);

                var leftNormal = Quaternion.Euler(0, rotDegree, 0) * Vector3.left;
                AddNormal(leftNormal);
                AddNormal(leftNormal);
                AddNormal(leftNormal);
                AddNormal(leftNormal);

                var rightNormal = Quaternion.Euler(0, rotDegree, 0) * Vector3.right;
                AddNormal(rightNormal);
                AddNormal(rightNormal);
                AddNormal(rightNormal);
                AddNormal(rightNormal);

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));

                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));
                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));

                AddUV(new Vector2(0, 0));
                AddUV(new Vector2(1, 0));
                AddUV(new Vector2(1, 1));
                AddUV(new Vector2(0, 1));
            }
        }

        #endregion

        #region GenerateTrianglesBox

        private void GenerateTrianglesBox()
        {
            // bottom triangles
            faces.Add(new TriangleIndices(0, 3, 1));
            faces.Add(new TriangleIndices(1, 3, 2));

            //// top triangles
            //faces.Add(new TriangleIndices(5, 4, 6));
            //faces.Add(new TriangleIndices(6, 4, 7));

            // back triangles
            faces.Add(new TriangleIndices(4, 7, 5));
            faces.Add(new TriangleIndices(5, 7, 6));

            // side triangles
            faces.Add(new TriangleIndices(8, 10, 9));
            faces.Add(new TriangleIndices(11, 13, 12));

            for (var i = 0; i < properties.steps; ++i)
            {
                var baseIndex = 14 + i * 14;
                faces.Add(new TriangleIndices(baseIndex + 1, baseIndex + 0, baseIndex + 2)); // step front
                faces.Add(new TriangleIndices(baseIndex + 2, baseIndex + 0, baseIndex + 3));

                faces.Add(new TriangleIndices(baseIndex + 5, baseIndex + 4, baseIndex + 6)); // step top
                faces.Add(new TriangleIndices(baseIndex + 6, baseIndex + 4, baseIndex + 7));

                faces.Add(new TriangleIndices(baseIndex + 8, baseIndex + 10, baseIndex + 9)); // step sides
                faces.Add(new TriangleIndices(baseIndex + 12, baseIndex + 13, baseIndex + 11));
            }
        }

        #endregion

        #region GenerateTrianglesClosed

        private void GenerateTrianglesClosed()
        {
            // bottom triangles
            faces.Add(new TriangleIndices(0, 3, 1));
            faces.Add(new TriangleIndices(1, 3, 2));

            // slanted bottom triangles
            faces.Add(new TriangleIndices(4, 5, 7));
            faces.Add(new TriangleIndices(7, 5, 6));

            // back triangles
            faces.Add(new TriangleIndices(8, 11, 9));
            faces.Add(new TriangleIndices(9, 11, 10));

            // side triangles
            faces.Add(new TriangleIndices(12, 15, 13));
            faces.Add(new TriangleIndices(13, 15, 14));

            faces.Add(new TriangleIndices(16, 17, 19));
            faces.Add(new TriangleIndices(19, 17, 18));

            for (var i = 0; i < properties.steps; ++i)
            {
                var baseIndex = 20 + i * 14;
                faces.Add(new TriangleIndices(baseIndex + 1, baseIndex + 0, baseIndex + 2)); // step front
                faces.Add(new TriangleIndices(baseIndex + 2, baseIndex + 0, baseIndex + 3));

                faces.Add(new TriangleIndices(baseIndex + 5, baseIndex + 4, baseIndex + 6)); // step top
                faces.Add(new TriangleIndices(baseIndex + 6, baseIndex + 4, baseIndex + 7));

                faces.Add(new TriangleIndices(baseIndex + 8, baseIndex + 10, baseIndex + 9)); // step sides
                faces.Add(new TriangleIndices(baseIndex + 12, baseIndex + 13, baseIndex + 11));
            }
        }

        #endregion

        #region GenerateTrianglesOpen

        private void GenerateTrianglesOpen()
        {
            for (var i = 0; i < properties.steps; ++i)
            {
                var baseIndex = i * 24;

                faces.Add(new TriangleIndices(baseIndex + 1, baseIndex + 0, baseIndex + 2)); // step bottom
                faces.Add(new TriangleIndices(baseIndex + 2, baseIndex + 0, baseIndex + 3));

                faces.Add(new TriangleIndices(baseIndex + 4, baseIndex + 7, baseIndex + 5)); // step front
                faces.Add(new TriangleIndices(baseIndex + 5, baseIndex + 7, baseIndex + 6));

                faces.Add(new TriangleIndices(baseIndex + 9, baseIndex + 8, baseIndex + 10)); // step top
                faces.Add(new TriangleIndices(baseIndex + 10, baseIndex + 8, baseIndex + 11));

                faces.Add(new TriangleIndices(baseIndex + 12, baseIndex + 15, baseIndex + 13)); // step back
                faces.Add(new TriangleIndices(baseIndex + 13, baseIndex + 15, baseIndex + 14));

                faces.Add(new TriangleIndices(baseIndex + 17, baseIndex + 16, baseIndex + 18)); // step left side
                faces.Add(new TriangleIndices(baseIndex + 18, baseIndex + 16, baseIndex + 19));

                faces.Add(new TriangleIndices(baseIndex + 21, baseIndex + 20, baseIndex + 22)); // step right side
                faces.Add(new TriangleIndices(baseIndex + 22, baseIndex + 20, baseIndex + 23));
            }
        }

        #endregion
    }
}