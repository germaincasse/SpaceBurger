/*
 

                      _                      _  _           _                                       
                     (_ )                   ( )(_ )        ( )                                      
   __   _   _    _    | |  _   _    __     _| | | |    _ _ | |_     ___       ___    _     ___ ___  
 /'__`\( ) ( ) /'_`\  | | ( ) ( ) /'__`\ /'_` | | |  /'_` )| '_`\ /',__)    /'___) /'_`\ /' _ ` _ `\
(  ___/| \_/ |( (_) ) | | | \_/ |(  ___/( (_| | | | ( (_| || |_) )\__, \ _ ( (___ ( (_) )| ( ) ( ) |
`\____)`\___/'`\___/'(___)`\___/'`\____)`\__,_)(___)`\__,_)(_,__/'(____/(_)`\____)`\___/'(_) (_) (_)
                                                                                                    
                                                                                                    
    TTS-o-matic (c) 2025 by EvolvedLabs SAS - www.evolvedlabs.com
  
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace TTS_o_matic
{
    public static class NomTTS
    {
        private const string DllName = "sherpatts";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Speak([MarshalAs(UnmanagedType.LPStr)] string text, double speed, int speaker_id = 0);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool LoadModel(
            [MarshalAs(UnmanagedType.LPStr)] string modelDir,
            [MarshalAs(UnmanagedType.LPStr)] string tokensPath,
            [MarshalAs(UnmanagedType.LPStr)] string configPath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern UIntPtr GetBufferSize();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetBufferData();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetSampleRate();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetNumSpeakers();

        public static int getNumSpeakers() 
        {
            return GetNumSpeakers();
        }

        public static AudioClip SpeakToClip(string text, double speed = 1.0, int speaker_id = 0 )
        {
            //This version is UI blocking and should not be used. Using the SpeakToClipAsync below is safer.

            if (!SpeakText(text, speed, speaker_id))
                return null;

            int sampleCount = (int)GetBufferSize();
            if (sampleCount <= 0)
            {
                Debug.LogError("[NomTTS] No audio generated.");
                return null;
            }

            IntPtr bufferPtr = GetBufferData();
            float[] samples = new float[sampleCount];
            Marshal.Copy(bufferPtr, samples, 0, sampleCount);

            int sampleRate = GetSampleRate();
            if (sampleRate <= 0)
            {
                sampleRate = 16000;
                Debug.LogWarning("[NomTTS] Sample rate unknown. Using default 16000.");
            }

            AudioClip clip = AudioClip.Create("TTSClip", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static async Task<AudioClip> SpeakToClipAsync(string text, double speed = 1.0, int speaker_id = 0)
        {
            //This is the preferred way to call the TTS functionality.

            var result = await Task.Run(() =>
            {
                if (!SpeakText(text, speed, speaker_id))
                    return (samples: (float[])null, sampleRate: 0, error: true);

                int sampleCount = (int)GetBufferSize();
                if (sampleCount <= 0)
                    return (samples: (float[])null, sampleRate: 0, error: true);

                var samples = new float[sampleCount];
                IntPtr bufferPtr = GetBufferData();
                Marshal.Copy(bufferPtr, samples, 0, sampleCount);

                int sr = GetSampleRate();
                if (sr <= 0) sr = 16000;

                return (samples: samples, sampleRate: sr, error: false);
            });

            if (result.error || result.samples == null)
            {
                Debug.LogError("[NomTTSAsync] Failed to generate audio.");
                return null;
            }

            AudioClip clip = AudioClip.Create("TTSClip", result.samples.Length, 1, result.sampleRate, false);
            clip.SetData(result.samples, 0);
            return clip;
        }


        public static bool InitModel(string iModelDirectory, string iTokensPath = "tokens.txt" , string iConfigPath = "espeak-ng-data")
        {
            string basePath = Path.Combine(Application.streamingAssetsPath, "ttsmodels" );

            string modelDirectory = Path.Combine(basePath, iModelDirectory);
            string tokensPath = Path.Combine(modelDirectory, iTokensPath);
            string configPath = Path.Combine(modelDirectory, iConfigPath);

            Debug.Log($"[NomTTS] StreamingAssetsPath: {Application.streamingAssetsPath}");
            Debug.Log($"[NomTTS] modelDirectory: {modelDirectory}");
            Debug.Log($"[NomTTS]   → Directory.Exists: {Directory.Exists(modelDirectory)}");

            // Automatically find the first .onnx file in the model directory
            string[] onnxFiles = Directory.GetFiles(modelDirectory, "*.onnx", SearchOption.TopDirectoryOnly);
            if (onnxFiles.Length == 0)
            {
                Debug.LogError("[NomTTS] No .onnx model file found in directory.");
                return false;
            }

            string modelPath = onnxFiles[0]; // Pick the first .onnx file found
            Debug.Log($"[NomTTS] modelPath (auto-detected): {modelPath}");

            Debug.Log($"[NomTTS] tokensPath: {tokensPath}");
            Debug.Log($"[NomTTS]   → File.Exists: {File.Exists(tokensPath)}");
            Debug.Log($"[NomTTS] configPath: {configPath}");
            Debug.Log($"[NomTTS]   → Directory.Exists: {Directory.Exists(configPath)}");

            bool result = LoadModel(modelPath, tokensPath, configPath);
            if (!result)
                Debug.LogError("[NomTTS] Failed to load SherpaTTS model.");
            return result;
        }

        public static bool SpeakText(string text, double speed = 1.0, int speaker_id = 0)
        {
            if (speaker_id < 0 || speaker_id > getNumSpeakers())
                speaker_id = 0;

            bool result = Speak(text, speed, speaker_id );
            if (!result)
                Debug.LogError("[NomTTS] Failed to synthesize speech.");
            return result;
        }
    }
}