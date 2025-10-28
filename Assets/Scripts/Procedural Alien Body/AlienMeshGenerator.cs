// AlienMeshGenerator.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AlienMeshGenerator : MonoBehaviour {
    public bool autoUpdate = true;
    public float surfaceThreshold = 0.5f;
    public float sampleDensity = 0.2f;
    public int maxSurfaceSamples = 3000;
    public float projectionSteps = 8;
    public float projectionStepSize = 0.1f;
    public float minDistanceBetweenPoints = 0.1f;
    public float maxTriangulationDistance = 0.5f;

    private MeshFilter meshFilter;
    private AlienSkeletonPoint[] skeletonPoints;
    private List<Vector3> surfacePoints = new List<Vector3>();
    private List<Vector3> surfaceNormals = new List<Vector3>();

    private void OnValidate() {
        if (autoUpdate) Generate();
    }

    public void Generate() {
        meshFilter = GetComponent<MeshFilter>();
        skeletonPoints = GetComponentsInChildren<AlienSkeletonPoint>();
        SampleSurfacePoints();
        Mesh mesh = TriangulateSurfacePoints();
        meshFilter.sharedMesh = mesh;
    }

    public void ClearMesh() {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = null;
    }

    public void ClearPoints() {
        surfacePoints.Clear();
        surfaceNormals.Clear();
    }

    public void ClearAll() {
        ClearMesh();
        ClearPoints();
    }

    private void SampleSurfacePoints() {
        surfacePoints.Clear();
        surfaceNormals.Clear();
        Bounds bounds = GetSkeletalBounds();

        int attempts = 0;
        while (surfacePoints.Count < maxSurfaceSamples && attempts < maxSurfaceSamples * 10) {
            attempts++;
            Vector3 point = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            for (int step = 0; step < projectionSteps; step++) {
                float d = SampleDensity(point);
                float delta = d - surfaceThreshold;
                if (Mathf.Abs(delta) < 0.02f) {
                    bool tooClose = false;
                    foreach (var existing in surfacePoints) {
                        if ((existing - point).sqrMagnitude < minDistanceBetweenPoints * minDistanceBetweenPoints) {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose) {
                        surfacePoints.Add(point);
                        surfaceNormals.Add(EstimateGradient(point).normalized);
                    }
                    break;
                }
                Vector3 grad = EstimateGradient(point);
                point -= grad.normalized * (delta * projectionStepSize);
            }
        }
    }

    private Mesh TriangulateSurfacePoints() {
        Mesh mesh = new Mesh();
        if (surfacePoints.Count < 3) return mesh;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int count = surfacePoints.Count;
        for (int i = 0; i < count; i++) {
            for (int j = i + 1; j < count; j++) {
                for (int k = j + 1; k < count; k++) {
                    Vector3 a = surfacePoints[i];
                    Vector3 b = surfacePoints[j];
                    Vector3 c = surfacePoints[k];

                    if ((a - b).sqrMagnitude > maxTriangulationDistance * maxTriangulationDistance) continue;
                    if ((a - c).sqrMagnitude > maxTriangulationDistance * maxTriangulationDistance) continue;
                    if ((b - c).sqrMagnitude > maxTriangulationDistance * maxTriangulationDistance) continue;

                    Vector3 center = (a + b + c) / 3f;
                    float d = SampleDensity(center);
                    if (Mathf.Abs(d - surfaceThreshold) > 0.05f) continue;

                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    Vector3 grad = EstimateGradient(center);

                    if (Vector3.Dot(normal, grad) < 0) {
                        triangles.Add(vertices.Count + 0);
                        triangles.Add(vertices.Count + 2);
                        triangles.Add(vertices.Count + 1);
                    } else {
                        triangles.Add(vertices.Count + 0);
                        triangles.Add(vertices.Count + 1);
                        triangles.Add(vertices.Count + 2);
                    }

                    vertices.Add(a);
                    vertices.Add(b);
                    vertices.Add(c);
                }
            }
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Bounds GetSkeletalBounds() {
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        skeletonPoints = GetComponentsInChildren<AlienSkeletonPoint>();
        if (skeletonPoints.Length == 0) return bounds;
        bounds = new Bounds(skeletonPoints[0].transform.position, Vector3.zero);
        foreach (var sp in skeletonPoints) {
            bounds.Encapsulate(sp.transform.position);
        }
        bounds.Expand(2f);
        return bounds;
    }

    private float SampleDensity(Vector3 p) {
        float minDist = float.MaxValue;
        foreach (var sp in skeletonPoints) {
            if (sp.next != null) {
                minDist = Mathf.Min(minDist, DistanceToSegment(p, sp, sp.next));
            } else {
                minDist = Mathf.Min(minDist, DistanceToPoint(p, sp));
            }
        }
        return Mathf.Clamp01(Mathf.Exp(-minDist * minDist * 4f));
    }

    private Vector3 EstimateGradient(Vector3 p) {
        float eps = 0.01f;
        float dx = SampleDensity(p + Vector3.right * eps) - SampleDensity(p - Vector3.right * eps);
        float dy = SampleDensity(p + Vector3.up * eps) - SampleDensity(p - Vector3.up * eps);
        float dz = SampleDensity(p + Vector3.forward * eps) - SampleDensity(p - Vector3.forward * eps);
        return new Vector3(dx, dy, dz) / (2f * eps);
    }

    private float DistanceToPoint(Vector3 p, AlienSkeletonPoint sp) {
        Quaternion orientation = sp.GetOrientation();
        Vector3 local = Quaternion.Inverse(orientation) * (p - sp.transform.position);
        float radiusZ = (sp.radiusX + sp.radiusY) * 0.5f;
        float x2 = (local.x * local.x) / (sp.radiusX * sp.radiusX);
        float y2 = (local.y * local.y) / (sp.radiusY * sp.radiusY);
        float z2 = (local.z * local.z) / (radiusZ * radiusZ);
        return Mathf.Sqrt(x2 + y2 + z2);
    }

    private float DistanceToSegment(Vector3 p, AlienSkeletonPoint a, AlienSkeletonPoint b) {
        Vector3 ab = b.transform.position - a.transform.position;
        Vector3 ap = p - a.transform.position;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
        Vector3 pointOnSegment = a.transform.position + t * ab;

        float rx = Mathf.Lerp(a.radiusX, b.radiusX, t);
        float ry = Mathf.Lerp(a.radiusY, b.radiusY, t);
        Vector3 forward = ab.normalized;
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.99f) up = Vector3.right;
        Quaternion rot = Quaternion.LookRotation(forward, up);

        Vector3 local = Quaternion.Inverse(rot) * (p - pointOnSegment);
        float rz = (rx + ry) * 0.5f;

        float x2 = (local.x * local.x) / (rx * rx);
        float y2 = (local.y * local.y) / (ry * ry);
        float z2 = (local.z * local.z) / (rz * rz);

        return Mathf.Sqrt(x2 + y2 + z2);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.magenta;
        foreach (var p in surfacePoints) {
            Gizmos.DrawSphere(p, sampleDensity * 0.2f);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AlienMeshGenerator))]
public class AlienMeshGeneratorEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        AlienMeshGenerator gen = (AlienMeshGenerator)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Mesh")) gen.Generate();
        if (GUILayout.Button("Clear Mesh")) gen.ClearMesh();
        if (GUILayout.Button("Clear Points")) gen.ClearPoints();
        if (GUILayout.Button("Clear All")) gen.ClearAll();
    }
}
#endif
