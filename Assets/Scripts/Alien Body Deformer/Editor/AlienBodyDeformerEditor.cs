using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AlienBodyDeformer))]
public class AlienBodyDeformerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AlienBodyDeformer deformer = (AlienBodyDeformer)target;

        GUILayout.Space(10);

        if (GUILayout.Button("🎲 Randomize Curves"))
        {
            Undo.RecordObject(deformer, "Randomize Curves");
            deformer.RandomizeCurves();
            deformer.UpdateDeformation();
            EditorUtility.SetDirty(deformer);
        }

        if (GUILayout.Button("🔄 Reset Form"))
        {
            Undo.RecordObject(deformer, "Reset Form");
            deformer.ResetForm();
            deformer.UpdateDeformation();
            EditorUtility.SetDirty(deformer);
        }

        if (GUILayout.Button("🔁 Force Update"))
        {
            deformer.UpdateDeformation();
            EditorUtility.SetDirty(deformer);
        }
    }
}
