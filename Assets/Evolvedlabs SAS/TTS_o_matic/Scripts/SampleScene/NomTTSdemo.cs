/*
 

                      _                      _  _           _                                       
                     (_ )                   ( )(_ )        ( )                                      
   __   _   _    _    | |  _   _    __     _| | | |    _ _ | |_     ___       ___    _     ___ ___  
 /'__`\( ) ( ) /'_`\  | | ( ) ( ) /'__`\ /'_` | | |  /'_` )| '_`\ /',__)    /'___) /'_`\ /' _ ` _ `\
(  ___/| \_/ |( (_) ) | | | \_/ |(  ___/( (_| | | | ( (_| || |_) )\__, \ _ ( (___ ( (_) )| ( ) ( ) |
`\____)`\___/'`\___/'(___)`\___/'`\____)`\__,_)(___)`\__,_)(_,__/'(____/(_)`\____)`\___/'(_) (_) (_)
                                                                                                    
                                                                                                    
    TTS-o-matic (c) 2025 by EvolvedLabs SAS - www.evolvedlabs.com
  
 */

using System.IO;
using UnityEngine;

using TTS_o_matic;

using System.Threading;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System;

namespace TTS_o_matic
{
    public class NomTTSdemo : MonoBehaviour
    {
        public TTS_o_matic_SampleScene mSceneUI;

        private AudioSource audioSource;

        void Start()
        {
            audioSource = this.gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private string mCurrentModel = "";

        public bool loadModel( string model )
        {
            if (mCurrentModel != model)
            {
                bool loaded = NomTTS.InitModel(model);
                if (!loaded)
                {
                    return false;
                }
                mCurrentModel = model;
            }

            return true;
        }

        private IEnumerator speakCO(string model, string text, float speed, bool waitforspeech, bool savewav, int speakerid)
        {
            /* 
                While not mandatory, you might want to block the UI (preventing the submit button to be clicked for example) as long texts (or very slow texts)
                can take a long time to compute.
            */

            mSceneUI.setUI(false);


            if (speed < 0.1f)
                speed = 0.1f;
            /*
                Speed 1.0 is the normal speed. It is suggested to have values >= 0.1f as too slow will take a long time to compute.
                Very long texts as well might take a long time to compute.
             */

            if( !loadModel( model ) )
            {
                Debug.LogError("[NOMtts] failed: failed to load "+model);
                yield break;
            }
            

            var speakTask = NomTTS.SpeakToClipAsync(text, speed, speakerid);

            yield return new WaitUntil(() => speakTask.IsCompleted);

            if (speakTask.Exception != null)
            {
                Debug.LogError("[NOMtts] failed: " + speakTask.Exception);
                yield break;
            }

            AudioClip clip = speakTask.Result;
            if (clip == null)
            {
                Debug.LogError("[NOMtts] No AudioClip returned.");
                yield break;
            }

            if( savewav )
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // Generate unique filename using current time in milliseconds
                // this file will be save in the user's document directory, adapt for your needs
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                string fileName = $"TTS-o-matic_{timestamp}.wav";

                // Combine to full path
                string fullPath = Path.Combine(documentsPath, fileName);

                try
                {
                    WavUtility.SaveWav(clip, fullPath);
                }
                catch( Exception e )
                {
                    Debug.LogError("[NOMtts] Unable to save to "+fullPath);
                }

            }


            //In this example, the clip is played on the the audiosource attached to this gameobject. Consider dumping it or using it anywhere else if necessary.
            audioSource.PlayOneShot(clip);

            if (waitforspeech)
            {
                float adjustedlenght = (clip.length / Mathf.Abs(audioSource.pitch));
                yield return new WaitForSeconds(adjustedlenght);
            }


            mSceneUI.setUI(true);
        }


        public int getNumSpeakers()
        {
            //Member wrapper function for readability clairty - feel free to use this directly if desired.
            return NomTTS.getNumSpeakers();
        }

        public void speak(string model, string text, float speed = 1.0f, bool waitforspeech = true, bool savewav = false, float pitch = 1.0f, int speakerid = 0)
        {
            //Pitch is applied on the audiosource, not on the TTS itself.
            //the speed value is internal to the TTS system - while you can change the speed on the audiosource, that will simply speed it up/down with no phonetic adjustment.
            audioSource.pitch = pitch;

            StartCoroutine(speakCO(model, text, speed, waitforspeech, savewav, speakerid));
        }


        void OnDestroy()
        {

        }
    }
}