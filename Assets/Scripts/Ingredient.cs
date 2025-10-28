using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Ingredient : MonoBehaviour
{
    [Header("Stack Settings")]
    [Tooltip("Épaisseur vers le haut à partir du pivot (en mètres)")]
    public float thicknessTop = 0.05f;

    [Tooltip("Épaisseur vers le bas à partir du pivot (en mètres)")]
    public float thicknessBottom = 0.05f;

    [Tooltip("Distance maximale pour empiler (en mètres)")]
    public float stackRange = 0.4f;

    [Header("Prefab Reference")]
    [Tooltip("Prefab d’origine de cet ingrédient (doit pointer sur lui-même dans le prefab)")]
    public GameObject sourcePrefab;

    [Header("Connections")]
    public int maxConnections = 2;
    public List<Ingredient> connectedIngredients = new List<Ingredient>();

    [HideInInspector] public Ingredient stackedAbove;
    [HideInInspector] public Ingredient stackedBelow;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;

        // Si pas assigné manuellement, on s’assure que la référence prefab existe
        if (sourcePrefab == null)
            sourcePrefab = gameObject;
    }

    // --- STACK ACCESSORS ------------------------------------------------------

    public List<Ingredient> GetStackAbove()
    {
        List<Ingredient> list = new List<Ingredient>();
        Ingredient current = stackedAbove;
        while (current != null)
        {
            list.Add(current);
            current = current.stackedAbove;
        }
        return list;
    }

    public List<Ingredient> GetStackBelow()
    {
        List<Ingredient> list = new List<Ingredient>();
        Ingredient current = stackedBelow;
        while (current != null)
        {
            list.Add(current);
            current = current.stackedBelow;
        }
        return list;
    }

    public List<Ingredient> GetFullStack()
    {
        List<Ingredient> list = new List<Ingredient>();

        Ingredient bottom = this;
        while (bottom.stackedBelow != null)
            bottom = bottom.stackedBelow;

        while (bottom != null)
        {
            list.Add(bottom);
            bottom = bottom.stackedAbove;
        }

        return list;
    }

    // --- STACK LOGIC ----------------------------------------------------------

    public Ingredient FindClosestIngredient()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, stackRange);
        Ingredient closest = null;
        float minDist = Mathf.Infinity;

        foreach (var h in hits)
        {
            Ingredient ing = h.GetComponent<Ingredient>();
            if (ing == null || ing == this) continue;

            float d = Vector3.Distance(transform.position, ing.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = ing;
            }
        }

        return closest;
    }

    /// <summary>
    /// Fusionne deux groupes : détruit les anciens et fait spawn une pile unique avec jointures.
    /// </summary>
    public void StackWith(Ingredient other)
    {
        if (other == null || other == this) return;

        IngredientGroup groupA = new IngredientGroup(this);
        IngredientGroup groupB = new IngredientGroup(other);

        // Empêche de stacker si déjà liés
        if (groupA.ingredients.Exists(i => groupB.ingredients.Contains(i))) return;

        // Combine et trie par hauteur
        List<Ingredient> combined = new List<Ingredient>();
        combined.AddRange(groupA.ingredients);
        combined.AddRange(groupB.ingredients);
        combined.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

        // Calcule le centre
        Vector3 center = Vector3.zero;
        foreach (var i in combined) center += i.transform.position;
        center /= combined.Count;

        // Sauvegarde des prefabs
        List<GameObject> prefabs = new List<GameObject>();
        foreach (var i in combined)
            prefabs.Add(i.sourcePrefab != null ? i.sourcePrefab : i.gameObject);

        // Détruit les anciens objets
        foreach (var i in combined)
            Destroy(i.gameObject);

        // Crée la nouvelle pile
        float currentY = center.y - (combined.Count * 0.5f * 0.1f);
        GameObject newParent = new GameObject("Stack");

        Ingredient prev = null;

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            GameObject newIngObj = Instantiate(prefab, new Vector3(center.x, currentY, center.z), Quaternion.identity, newParent.transform);
            Ingredient ing = newIngObj.GetComponent<Ingredient>();
            currentY += ing.thicknessTop + ing.thicknessBottom;

            if (prev != null)
            {
                prev.stackedAbove = ing;
                ing.stackedBelow = prev;

                // Jointure physique
                Rigidbody rbA = prev.GetComponent<Rigidbody>();
                Rigidbody rbB = ing.GetComponent<Rigidbody>();
                if (rbA != null && rbB != null)
                {
                    FixedJoint j = rbA.gameObject.AddComponent<FixedJoint>();
                    j.connectedBody = rbB;
                    j.breakForce = 10000f;
                    j.breakTorque = 10000f;
                }

                // Connexions logiques
                prev.connectedIngredients.Add(ing);
                ing.connectedIngredients.Add(prev);
            }

            prev = ing;
        }

        Debug.Log($"[STACK] Nouvelle pile créée ({prefabs.Count} ingrédients) avec jointures physiques.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stackRange);
    }
}
