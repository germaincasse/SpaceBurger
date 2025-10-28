using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IngredientGroup
{
    public List<Ingredient> ingredients = new List<Ingredient>();

    public IngredientGroup(Ingredient root)
    {
        if (root != null)
            BuildFrom(root);
    }

    public void BuildFrom(Ingredient root)
    {
        ingredients.Clear();
        if (root == null) return;

        Ingredient current = root;
        while (current.stackedBelow != null)
            current = current.stackedBelow;

        while (current != null)
        {
            Add(current);
            current = current.stackedAbove;
        }
    }

    public void Add(Ingredient ing)
    {
        if (ing == null || ingredients.Contains(ing)) return;
        ingredients.Add(ing);
    }

    public Ingredient GetTop()
    {
        foreach (var i in ingredients)
            if (i.stackedAbove == null)
                return i;
        return null;
    }

    public Ingredient GetBottom()
    {
        foreach (var i in ingredients)
            if (i.stackedBelow == null)
                return i;
        return null;
    }
}
