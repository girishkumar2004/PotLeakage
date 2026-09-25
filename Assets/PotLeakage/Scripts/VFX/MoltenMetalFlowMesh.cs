using System.Collections.Generic;
using UnityEngine;

namespace PotLeakage.VFX
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MoltenMetalFlowMesh : MonoBehaviour
    {
        [Header("Transforms (Read-Only References)")]
        public Transform sourceTransform;
        public Transform dropTransform;

        [Header("Material & Mesh Parameters")]
        public Material flowMaterial;
        public float radius = 0.055f;
        public int longitudinalSegments = 60;
        public int radialSegments = 16;
        public float vTiling = 3.5f;
        public bool reverseUVDirection = false;

        [Header("Controlled Leakage Stream (Visual Setting)")]
        [Tooltip("Default wide flow tube radius (meters)")]
        public float defaultRadius = 0.055f;

        [Tooltip("Thinned controlled flow tube radius (meters) — visual parameter")]
        public float controlledRadius = 0.016f;

        [Tooltip("Whether the flow is currently in controlled leakage mode")]
        public bool isControlledLeakage = false;

        private Coroutine transitionCoroutine;

        // Authoritative Design Coordinates
        public static readonly Vector3 P0_Source = new Vector3(-4.32000017f, 0.262073308f, -68.1200027f);
        public static readonly Vector3 P1_Rise = new Vector3(-4.32000017f, 0.633f, -68.1200027f);
        public static readonly Vector3 P2_Curve1 = new Vector3(-4.20f, 0.633f, -67.95f);
        public static readonly Vector3 P3_Curve2 = new Vector3(-4.05f, 0.633f, -67.75f);
        public static readonly Vector3 P4_Curve3 = new Vector3(-3.90f, 0.633f, -67.55f);
        public static readonly Vector3 P5_Crest = new Vector3(-3.76804996f, 0.633f, -67.387001f);
        public static readonly Vector3 P6_Drop1 = new Vector3(-3.76804996f, 0.40f, -67.387001f);
        public static readonly Vector3 P7_Drop2 = new Vector3(-3.76804996f, 0.10f, -67.387001f);
        public static readonly Vector3 P8_DropTarget = new Vector3(-3.76804996f, -0.156000003f, -67.387001f);

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh generatedMesh;

        /// <summary>
        /// Transitions the continuous molten metal stream between default wider leakage and thinner controlled stream.
        /// Preserves the continuous stream, source, and drop coordinates.
        /// </summary>
        public void SetControlled(bool controlled, float duration = 1.0f)
        {
            isControlledLeakage = controlled;
            float targetR = controlled ? controlledRadius : defaultRadius;

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            if (Application.isPlaying && duration > 0.01f && gameObject.activeInHierarchy)
            {
                transitionCoroutine = StartCoroutine(TransitionRadiusRoutine(targetR, duration));
            }
            else
            {
                radius = targetR;
                BuildStreamMesh();
            }
        }

        private System.Collections.IEnumerator TransitionRadiusRoutine(float targetR, float duration)
        {
            float startR = radius;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                radius = Mathf.Lerp(startR, targetR, t);
                BuildStreamMesh();
                yield return null;
            }
            radius = targetR;
            BuildStreamMesh();
            transitionCoroutine = null;
        }

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            InitializeComponents();
            if (generatedMesh == null || generatedMesh.vertexCount == 0)
            {
                BuildStreamMesh();
            }
        }

        public void InitializeComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

            if (flowMaterial != null && meshRenderer != null && meshRenderer.sharedMaterial != flowMaterial)
            {
                meshRenderer.sharedMaterial = flowMaterial;
            }
        }

        /// <summary>
        /// Generates ONE continuous, organic tube mesh connecting P0 through P8 using Centripetal Catmull-Rom.
        /// </summary>
        public void BuildStreamMesh()
        {
            InitializeComponents();

            // Read source and destination positions if transforms assigned
            Vector3 p0 = sourceTransform != null ? sourceTransform.position : P0_Source;
            Vector3 p8 = dropTransform != null ? dropTransform.position : P8_DropTarget;

            Vector3[] controlPoints = new Vector3[]
            {
                p0,
                P1_Rise,
                P2_Curve1,
                P3_Curve2,
                P4_Curve3,
                P5_Crest,
                P6_Drop1,
                P7_Drop2,
                p8
            };

            int ringCount = Mathf.Max(longitudinalSegments + 1, 10);
            int radialCount = Mathf.Max(radialSegments, 6);

            // Sample spline points
            List<Vector3> centerline = SampleSpline(controlPoints, ringCount);

            // Compute tangents
            List<Vector3> tangents = new List<Vector3>(centerline.Count);
            for (int i = 0; i < centerline.Count; i++)
            {
                Vector3 tan;
                if (i == 0)
                {
                    tan = (centerline[1] - centerline[0]).normalized;
                }
                else if (i == centerline.Count - 1)
                {
                    tan = (centerline[centerline.Count - 1] - centerline[centerline.Count - 2]).normalized;
                }
                else
                {
                    tan = (centerline[i + 1] - centerline[i - 1]).normalized;
                }
                if (tan == Vector3.zero) tan = Vector3.up;
                tangents.Add(tan);
            }

            // Parallel transport frames (Bishop Frame) along the path
            List<Vector3> normals = new List<Vector3>(centerline.Count);
            List<Vector3> binormals = new List<Vector3>(centerline.Count);

            // Initial frame at ring 0
            Vector3 initTan = tangents[0];
            Vector3 initNorm = Vector3.Cross(initTan, Vector3.forward).normalized;
            if (initNorm.sqrMagnitude < 0.001f)
            {
                initNorm = Vector3.Cross(initTan, Vector3.right).normalized;
            }
            Vector3 initBinorm = Vector3.Cross(initTan, initNorm).normalized;

            normals.Add(initNorm);
            binormals.Add(initBinorm);

            for (int i = 1; i < centerline.Count; i++)
            {
                Vector3 prevTan = tangents[i - 1];
                Vector3 curTan = tangents[i];

                Vector3 v = Vector3.Cross(prevTan, curTan);
                Vector3 curNorm;
                if (v.sqrMagnitude < 0.00001f)
                {
                    curNorm = normals[i - 1];
                }
                else
                {
                    float angle = Vector3.Angle(prevTan, curTan);
                    Quaternion rot = Quaternion.AngleAxis(angle, v.normalized);
                    curNorm = (rot * normals[i - 1]).normalized;
                }
                Vector3 curBinorm = Vector3.Cross(curTan, curNorm).normalized;

                normals.Add(curNorm);
                binormals.Add(curBinorm);
            }

            // Generate vertices, normals, UVs
            int vertsPerRing = radialCount + 1; // duplicate first vertex for seamless UV seam
            int totalVerts = ringCount * vertsPerRing;
            Vector3[] vertices = new Vector3[totalVerts];
            Vector3[] vertexNormals = new Vector3[totalVerts];
            Vector4[] vertexTangents = new Vector4[totalVerts];
            Vector2[] uvs = new Vector2[totalVerts];

            // Calculate cumulative length for accurate UV V-coordinate
            float[] cumLengths = new float[ringCount];
            cumLengths[0] = 0f;
            for (int i = 1; i < ringCount; i++)
            {
                cumLengths[i] = cumLengths[i - 1] + Vector3.Distance(centerline[i], centerline[i - 1]);
            }
            float totalLength = cumLengths[ringCount - 1];

            // Transform world coordinates to local space of this GameObject
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

            for (int r = 0; r < ringCount; r++)
            {
                Vector3 centerWorld = centerline[r];
                Vector3 norm = normals[r];
                Vector3 binorm = binormals[r];
                Vector3 tan = tangents[r];

                float progress = totalLength > 0.0001f ? (cumLengths[r] / totalLength) : ((float)r / (ringCount - 1));

                // Organic radius profile:
                // Start (Source): ~0.050m
                // Upper horizontal flow: ~0.065m
                // Vertical descent: ~0.045m
                // Termination: ~0.060m
                float rScale = 1.0f;
                if (progress < 0.15f)
                {
                    // Rise from source (0.050 -> 0.065)
                    rScale = Mathf.Lerp(0.91f, 1.18f, progress / 0.15f);
                }
                else if (progress < 0.65f)
                {
                    // Upper channel: wide molten stream
                    rScale = 1.18f;
                }
                else if (progress < 0.90f)
                {
                    // Vertical descent: stretch/necking (0.065 -> 0.045)
                    float tFall = (progress - 0.65f) / 0.25f;
                    rScale = Mathf.Lerp(1.18f, 0.82f, tFall);
                }
                else
                {
                    // Contact pool flare (0.045 -> 0.060)
                    float tEnd = (progress - 0.90f) / 0.10f;
                    rScale = Mathf.Lerp(0.82f, 1.09f, tEnd);
                }

                float currentRadius = radius * rScale;

                float vCoord = reverseUVDirection ? (1f - progress) * vTiling : progress * vTiling;

                for (int s = 0; s <= radialCount; s++)
                {
                    float uCoord = (float)s / radialCount;
                    float angle = uCoord * Mathf.PI * 2f;

                    float cosA = Mathf.Cos(angle);
                    float sinA = Mathf.Sin(angle);

                    Vector3 radialDirWorld = (cosA * norm + sinA * binorm).normalized;
                    Vector3 vertPosWorld = centerWorld + radialDirWorld * currentRadius;

                    int vertIdx = r * vertsPerRing + s;

                    vertices[vertIdx] = worldToLocal.MultiplyPoint3x4(vertPosWorld);
                    vertexNormals[vertIdx] = worldToLocal.MultiplyVector(radialDirWorld).normalized;

                    Vector3 tanWorld = tan;
                    vertexTangents[vertIdx] = new Vector4(tanWorld.x, tanWorld.y, tanWorld.z, 1.0f);

                    uvs[vertIdx] = new Vector2(uCoord, vCoord);
                }
            }

            // Generate triangles
            int quadCount = (ringCount - 1) * radialCount;
            int[] triangles = new int[quadCount * 6];
            int triIdx = 0;

            for (int r = 0; r < ringCount - 1; r++)
            {
                int ringStart = r * vertsPerRing;
                int nextRingStart = (r + 1) * vertsPerRing;

                for (int s = 0; s < radialCount; s++)
                {
                    int i0 = ringStart + s;
                    int i1 = nextRingStart + s;
                    int i2 = nextRingStart + s + 1;
                    int i3 = ringStart + s + 1;

                    triangles[triIdx++] = i0;
                    triangles[triIdx++] = i1;
                    triangles[triIdx++] = i2;

                    triangles[triIdx++] = i0;
                    triangles[triIdx++] = i2;
                    triangles[triIdx++] = i3;
                }
            }

            if (generatedMesh == null)
            {
                generatedMesh = new Mesh();
                generatedMesh.name = "MoltenMetalFlow_Mesh";
            }
            else
            {
                generatedMesh.Clear();
            }

            generatedMesh.vertices = vertices;
            generatedMesh.normals = vertexNormals;
            generatedMesh.tangents = vertexTangents;
            generatedMesh.uv = uvs;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateBounds();

            if (meshFilter != null)
            {
                meshFilter.sharedMesh = generatedMesh;
            }
            if (meshRenderer != null && flowMaterial != null)
            {
                meshRenderer.sharedMaterial = flowMaterial;
            }
        }

        /// <summary>
        /// Samples the Centripetal Catmull-Rom spline across the control points.
        /// Clamps output to strictly avoid overshooting the defined envelope.
        /// </summary>
        private List<Vector3> SampleSpline(Vector3[] points, int totalSamples)
        {
            List<Vector3> result = new List<Vector3>(totalSamples);
            int segments = points.Length - 1;
            int samplesPerSegment = Mathf.Max(1, totalSamples / segments);

            for (int i = 0; i < segments; i++)
            {
                Vector3 p0 = i > 0 ? points[i - 1] : points[i] + (points[i] - points[i + 1]);
                Vector3 p1 = points[i];
                Vector3 p2 = points[i + 1];
                Vector3 p3 = i < segments - 1 ? points[i + 2] : points[i + 1] + (points[i + 1] - points[i]);

                float t0 = 0f;
                float t1 = t0 + Mathf.Pow(Mathf.Max(Vector3.Distance(p0, p1), 0.0001f), 0.5f);
                float t2 = t1 + Mathf.Pow(Mathf.Max(Vector3.Distance(p1, p2), 0.0001f), 0.5f);
                float t3 = t2 + Mathf.Pow(Mathf.Max(Vector3.Distance(p2, p3), 0.0001f), 0.5f);

                int count = (i == segments - 1) ? (totalSamples - result.Count - 1) : samplesPerSegment;

                for (int s = 0; s < count; s++)
                {
                    float t = (float)s / count;
                    float globalT = Mathf.Lerp(t1, t2, t);

                    Vector3 a1 = (t1 - globalT) / (t1 - t0) * p0 + (globalT - t0) / (t1 - t0) * p1;
                    Vector3 a2 = (t2 - globalT) / (t2 - t1) * p1 + (globalT - t1) / (t2 - t1) * p2;
                    Vector3 a3 = (t3 - globalT) / (t3 - t2) * p2 + (globalT - t2) / (t3 - t2) * p3;

                    Vector3 b1 = (t2 - globalT) / (t2 - t0) * a1 + (globalT - t0) / (t2 - t0) * a2;
                    Vector3 b2 = (t3 - globalT) / (t3 - t1) * a2 + (globalT - t1) / (t3 - t1) * a3;

                    Vector3 c = (t2 - globalT) / (t2 - t1) * b1 + (globalT - t1) / (t2 - t1) * b2;

                    // Clamp to strictly prevent overshoot beyond control points bounds
                    c.x = Mathf.Clamp(c.x, -4.32000017f, -3.76804996f);
                    c.y = Mathf.Clamp(c.y, -0.156000003f, 0.633f);
                    c.z = Mathf.Clamp(c.z, -68.1200027f, -67.387001f);

                    result.Add(c);
                }
            }

            result.Add(points[points.Length - 1]);
            return result;
        }

        public void ClearMesh()
        {
            if (generatedMesh != null)
            {
                generatedMesh.Clear();
            }
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = null;
            }
        }
    }
}
