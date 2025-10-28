/*
 

                      _                      _  _           _                                       
                     (_ )                   ( )(_ )        ( )                                      
   __   _   _    _    | |  _   _    __     _| | | |    _ _ | |_     ___       ___    _     ___ ___  
 /'__`\( ) ( ) /'_`\  | | ( ) ( ) /'__`\ /'_` | | |  /'_` )| '_`\ /',__)    /'___) /'_`\ /' _ ` _ `\
(  ___/| \_/ |( (_) ) | | | \_/ |(  ___/( (_| | | | ( (_| || |_) )\__, \ _ ( (___ ( (_) )| ( ) ( ) |
`\____)`\___/'`\___/'(___)`\___/'`\____)`\__,_)(___)`\__,_)(_,__/'(____/(_)`\____)`\___/'(_) (_) (_)
                                                                                                    
                                                                                                    
    TTS-o-matic (c) 2025 by EvolvedLabs SAS - www.evolvedlabs.com
  
 */

using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TTS_o_matic
{
    public class TTS_o_matic_SampleScene : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument; // assign via Inspector

        private VisualElement uiContainer;

        private DropdownField modelsDropdown;
        private DropdownField speakerDropdown;
        private TextField inputField;
        private Slider speedSlider;
        private Button runButton;
        private Label statusLabel;
        private Toggle waitToggle;
        private Slider pitchSlider;
        private Toggle saveToggle;


        public NomTTSdemo myNomTTSgameobject;

        public void setUI(bool toWhat)
        {
            modelsDropdown.SetEnabled(toWhat);
            speedSlider.SetEnabled(toWhat);
            inputField.SetEnabled(toWhat);
            runButton.SetEnabled(toWhat);
            waitToggle.SetEnabled(toWhat);
            pitchSlider.SetEnabled(toWhat);
            saveToggle.SetEnabled(toWhat);
            speakerDropdown.SetEnabled(toWhat);
        }

        void Start()
        {

            if (uiDocument == null)
            {
                Debug.LogError("UIDocument not assigned on " + gameObject.name);
                return;
            }

            var root = uiDocument.rootVisualElement;

            // Create a container for ALL UI
            uiContainer = new VisualElement();
            uiContainer.style.flexDirection = FlexDirection.Column;
            uiContainer.style.justifyContent = Justify.Center;
            uiContainer.style.alignItems = Align.Center;
            uiContainer.style.width = Length.Percent(100);
            uiContainer.style.height = Length.Percent(100);
            root.Add(uiContainer);

            // Root styling
            uiContainer.style.flexDirection = FlexDirection.Column;
            uiContainer.style.justifyContent = Justify.Center;
            uiContainer.style.alignItems = Align.Center;
            uiContainer.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 1f));
            uiContainer.style.paddingTop = 20;
            uiContainer.style.paddingBottom = 20;
            uiContainer.style.paddingLeft = 20;
            uiContainer.style.paddingRight = 20;

            // Status label
            statusLabel = new Label();
            statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            statusLabel.style.marginBottom = 10;
            statusLabel.style.fontSize = 24;
            statusLabel.style.color = new StyleColor(Color.white);
            uiContainer.Add(statusLabel);

            // Model Dropdown Field
            modelsDropdown = new DropdownField("Select Model", new List<string>(), 0);
            modelsDropdown.style.width = 800;
            modelsDropdown.style.marginBottom = 15;
            modelsDropdown.style.fontSize = 16;
            modelsDropdown.style.unityFontStyleAndWeight = FontStyle.Normal;
            modelsDropdown.labelElement.style.color = new StyleColor(Color.white);
            uiContainer.Add(modelsDropdown);

            // Speaker Dropdown Field
            speakerDropdown = new DropdownField("Select Speaker ID", new List<string>(), 0);
            speakerDropdown.style.width = 800;
            speakerDropdown.style.marginBottom = 15;
            speakerDropdown.style.fontSize = 16;
            speakerDropdown.style.unityFontStyleAndWeight = FontStyle.Normal;
            speakerDropdown.labelElement.style.color = new StyleColor(Color.white);
            uiContainer.Add(speakerDropdown);


            modelsDropdown.RegisterValueChangedCallback(delegate {
                myNomTTSgameobject.loadModel( modelsDropdown.value );
                int availableSpeakers = myNomTTSgameobject.getNumSpeakers();
                List<string> speakersList = new List<string>();
                for (int i = 0; i < availableSpeakers; i++)
                    speakersList.Add(i.ToString());

                speakerDropdown.choices = speakersList;
                speakerDropdown.value = speakersList[0];
            });

            // TextField
            inputField = new TextField("Sample Text:");
            inputField.value = "Enter your text here: make sure to match the language of the model!";
            inputField.style.width = 800;
            inputField.style.marginBottom = 15;
            inputField.style.fontSize = 16;
            inputField.labelElement.style.color = new StyleColor(Color.white);
            uiContainer.Add(inputField);

            // Speed Slider
            speedSlider = new Slider("Speech Speed (Internal)", 0.1f, 200f);
            speedSlider.value = 1f;
            speedSlider.style.width = 800;
            speedSlider.style.marginBottom = 15;
            speedSlider.style.color = new StyleColor(Color.white);
            speedSlider.labelElement.style.color = new StyleColor(Color.white);
            uiContainer.Add(speedSlider);

            // Pitch Slider
            pitchSlider = new Slider("Pitch (on audiosource)", 0, 3);
            pitchSlider.value = 1f;
            pitchSlider.style.width = 800;
            pitchSlider.style.marginBottom = 15;
            pitchSlider.style.color = new StyleColor(Color.white);
            pitchSlider.labelElement.style.color = new StyleColor(Color.white);
            uiContainer.Add(pitchSlider);

            //Wait For speech Toggle
            saveToggle = new Toggle("Save file as .wav in Documents folder");
            saveToggle.value = false; // default to unchecked
            saveToggle.style.marginBottom = 15;
            saveToggle.labelElement.style.color = new StyleColor(Color.white);
            saveToggle.style.color = new StyleColor(Color.white);
            uiContainer.Add(saveToggle);


            //Wait For speech Toggle
            waitToggle = new Toggle("Wait for speech to finish");
            waitToggle.value = true; // default to checked
            waitToggle.style.marginBottom = 15;
            waitToggle.labelElement.style.color = new StyleColor(Color.white);
            waitToggle.style.color = new StyleColor(Color.white);
            uiContainer.Add(waitToggle);

            // Run button
            runButton = new Button(OnRun) { text = "Speak!" };
            runButton.style.width = 150;
            runButton.style.height = 40;
            runButton.style.fontSize = 16;
            runButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            runButton.style.backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 0.9f));
            runButton.style.color = new StyleColor(Color.white);
            runButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            uiContainer.Add(runButton);

            LoadModels();
            speakerDropdown.value = "0";
        }

        private void LoadModels()
        {
            string modelsPath = Path.Combine(Application.streamingAssetsPath, "ttsmodels");
            if (!Directory.Exists(modelsPath))
                Directory.CreateDirectory(modelsPath);

            List<string> folders = Directory.GetDirectories(modelsPath)
                                             .Select(Path.GetFileName)
                                             .ToList();

            if (folders.Count == 0)
            {
                statusLabel.text = "No TTS models found – please download some from Tools -> TTs-o-matic -> Model Asset Downloader then restart the scene.";
                modelsDropdown.style.display = DisplayStyle.None;
                inputField.style.display = DisplayStyle.None;
                speedSlider.style.display = DisplayStyle.None;
                runButton.style.display = DisplayStyle.None;
                waitToggle.style.display = DisplayStyle.None;
                saveToggle.style.display = DisplayStyle.None;
                pitchSlider.style.display = DisplayStyle.None;
                speakerDropdown.style.display = DisplayStyle.None;
            }
            else
            {
                statusLabel.text = "Select a TTS model, enter text, and adjust speed and pitch if desired:";
                modelsDropdown.choices = folders;
                modelsDropdown.value = folders[0];
            }
        }

        private void OnRun()
        {
            string modelName = modelsDropdown.value;
            string userText = inputField.value;
            float speed = speedSlider.value;
            bool waitForSpeech = waitToggle.value;
            bool savewav = saveToggle.value;
            float pitch = pitchSlider.value;

            int speakerid = 0;
            int.TryParse(speakerDropdown.value, out speakerid); 


            if (string.IsNullOrEmpty(modelName) || string.IsNullOrEmpty(userText))
                return;

            Debug.Log($"Running demo with model: {modelName}, text: {userText}, speed: {speed}, wait: {waitForSpeech}, pitch: {pitchSlider}");

            myNomTTSgameobject.speak(modelName, userText, speed, waitForSpeech, savewav, pitch, speakerid);
        }
    }
}