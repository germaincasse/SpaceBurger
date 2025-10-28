using UnityEngine;
using TMPro;

public class PopupText : MonoBehaviour
{
    public TextMeshProUGUI textUI;
    private Transform cameraTransform;

    void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        AlignTextToCamera();
    }

    public void AlignTextToCamera()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        if (cameraTransform == null)
        {
            Debug.LogWarning("Camera principale non trouvée !");
            return;
        }

        transform.rotation = cameraTransform.rotation;
    }

    public void SetText(string newText)
    {
        if (textUI != null)
        {
            textUI.text = newText;
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI non assigné !");
        }
    }

    public void ClearText()
    {
        SetText("");
    }
}
