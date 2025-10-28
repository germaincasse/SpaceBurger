using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class AlienBodyDeformer : MonoBehaviour
{
    [Header("Global Scale")]
    public Vector3 scale = Vector3.one;

    [Header("Radial Profiles")]
    public AnimationCurve profileX = AnimationCurve.Linear(0, 1, 1, 1);
    public AnimationCurve profileZ = AnimationCurve.Linear(0, 1, 1, 1);
    [Range(2, 10)] public int curvePoints = 4;

    [Header("Twist")]
    public float twistAmount = 0f; // degrés

    [Header("Noise")]
    public float noiseStrength = 0f;
    public float noiseScale = 2f;

    private Mesh originalMesh;
    private Mesh workingMesh;

    void OnEnable()
    {
        InitMesh();
        UpdateDeformation();
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            InitMesh();
            UpdateDeformation();
        }
    }

    void InitMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null) return;

        if (originalMesh == null)
        {
            originalMesh = mf.sharedMesh;
            workingMesh = Instantiate(originalMesh);
            workingMesh.name = originalMesh.name + "_deformed";
            mf.sharedMesh = workingMesh;
        }
    }

    public void UpdateDeformation()
    {
        if (originalMesh == null || workingMesh == null) return;

        Vector3[] originalVertices = originalMesh.vertices;
        Vector3[] newVertices = new Vector3[originalVertices.Length];

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector3 v in originalVertices)
        {
            if (v.y < minY) minY = v.y;
            if (v.y > maxY) maxY = v.y;
        }

        float height = Mathf.Max(0.0001f, maxY - minY);

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];
            float t = Mathf.InverseLerp(minY, maxY, v.y);

            float radialX = profileX.Evaluate(t);
            float radialZ = profileZ.Evaluate(t);

            float y = (t - 0.5f) * scale.y;

            Vector2 dir = new Vector2(v.x, v.z).normalized;
            float baseRadius = new Vector2(v.x, v.z).magnitude;

            float noise = 1f + (Mathf.PerlinNoise(v.x * noiseScale, v.y * noiseScale) - 0.5f) * 2f * noiseStrength;

            float x = dir.x * baseRadius * radialX * scale.x * noise;
            float z = dir.y * baseRadius * radialZ * scale.z * noise;

            float angle = Mathf.Lerp(-twistAmount, twistAmount, t);
            Quaternion twist = Quaternion.Euler(0f, angle, 0f);
            Vector3 twisted = twist * new Vector3(x, 0f, z);

            newVertices[i] = new Vector3(twisted.x, y, twisted.z);
        }

        workingMesh.vertices = newVertices;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();
    }

    public void RandomizeCurves()
    {
        profileX = GenerateRandomCurve(curvePoints);
        profileZ = GenerateRandomCurve(curvePoints);
    }

    public void ResetForm()
    {
        scale = Vector3.one;
        twistAmount = 0f;
        noiseStrength = 0f;
        noiseScale = 2f;
        curvePoints = 4;
        profileX = AnimationCurve.Linear(0, 1, 1, 1);
        profileZ = AnimationCurve.Linear(0, 1, 1, 1);
    }

    AnimationCurve GenerateRandomCurve(int points)
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        for (int i = 1; i < points - 1; i++)
        {
            float t = i / (float)(points - 1);
            float value = Random.Range(0.4f, 1.2f);
            curve.AddKey(new Keyframe(t, value));
        }
        curve.AddKey(1f, 1f);
        return curve;
    }
}
