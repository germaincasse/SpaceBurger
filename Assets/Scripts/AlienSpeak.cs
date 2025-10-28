using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TTS_o_matic;

public class AlienSpeak : MonoBehaviour
{
    [Header("TTS Settings")]
    public AudioSource audioSource;
    public string modelName = "vits-piper-en_US-norman-medium";
    public float speechSpeed = 1.0f;
    public float repeatDelay = 5.0f;

    [Header("Text Display")]
    public PopupText popupText;  // assigné via l'inspecteur
    public float extraTextTime = 0.5f;

    [Header("Phrase File")]
    public string filePath = "Assets/AlienVoiceLines/alien_to_player_final.txt";

    private List<string> lines = new List<string>();

    void Start()
    {
        LoadLinesFromFile();
        StartCoroutine(LoopSpeak());
    }

    void LoadLinesFromFile()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("Fichier de phrases introuvable : " + filePath);
            return;
        }

        lines.Clear();
        foreach (string line in File.ReadAllLines(filePath))
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                lines.Add(trimmed);
            }
        }

        if (lines.Count == 0)
        {
            Debug.LogError("Aucune phrase valide trouvée dans le fichier.");
        }
    }

    IEnumerator LoopSpeak()
    {
        while (true)
        {
            if (lines.Count > 0)
            {
                string randomPhrase = lines[Random.Range(0, lines.Count)];
                yield return StartCoroutine(SpeakSplit(randomPhrase));
            }
            yield return new WaitForSeconds(repeatDelay);
        }
    }

    IEnumerator SpeakSplit(string fullPhrase)
    {
        string[] parts = fullPhrase.Split(';');
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                yield return StartCoroutine(SpeakAndDisplay(trimmed));
                yield return new WaitUntil(() => !audioSource.isPlaying);
            }
        }

        // Nettoyage du texte à la fin
        if (popupText != null)
        {
            yield return new WaitForSeconds(extraTextTime);
            popupText.ClearText();
        }
    }

    bool model_loaded = false;

    IEnumerator SpeakAndDisplay(string text)
    {
        if (!model_loaded)
        {

            bool loaded = NomTTS.InitModel(modelName);
            if (!loaded)
            {
                NomTTS.InitModel(modelName);
                if (!loaded)
                {
                    Debug.LogError("Erreur de chargement du modèle : " + modelName);
                    yield break;
                }
                else
                    model_loaded = true;
            }
        }

        var speakTask = NomTTS.SpeakToClipAsync(text, speechSpeed);
        yield return new WaitUntil(() => speakTask.IsCompleted);

        if (speakTask.Exception != null)
        {
            Debug.LogError("[TTS-o-matic] Échec : " + speakTask.Exception);
            yield break;
        }

        AudioClip clip = speakTask.Result;
        if (clip == null)
        {
            Debug.LogError("[TTS-o-matic] Aucun AudioClip généré.");
            yield break;
        }

        audioSource.clip = clip;
        audioSource.Play();

        if (popupText != null)
        {
            popupText.SetText(text);
        }
    }
}
