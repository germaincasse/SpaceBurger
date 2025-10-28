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

namespace TTS_o_matic
{
    public static class WavUtility
    {
        public static void SaveWav(AudioClip clip, string filepath)
        {
            var wavBytes = ConvertToWav(clip);
            File.WriteAllBytes(filepath, wavBytes);
        }

        public static byte[] ConvertToWav(AudioClip clip)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            byte[] wav = new byte[44 + samples.Length * 2];
            int hz = clip.frequency;
            int channels = clip.channels;
            int samplesLength = samples.Length;

            // WAV Header
            System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, wav, 0, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(wav.Length - 8), 0, wav, 4, 4);
            System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, wav, 8, 4);
            System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, wav, 12, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(16), 0, wav, 16, 4); // Subchunk1 size
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)1), 0, wav, 20, 2); // Audio format (1 = PCM)
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)channels), 0, wav, 22, 2);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(hz), 0, wav, 24, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(hz * channels * 2), 0, wav, 28, 4); // Byte rate
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)(channels * 2)), 0, wav, 32, 2); // Block align
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)16), 0, wav, 34, 2); // Bits per sample
            System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("data"), 0, wav, 36, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(samplesLength * 2), 0, wav, 40, 4);

            // PCM Data
            int offset = 44;
            for (int i = 0; i < samplesLength; i++)
            {
                short val = (short)(samples[i] * 32767);
                byte[] bytes = System.BitConverter.GetBytes(val);
                wav[offset++] = bytes[0];
                wav[offset++] = bytes[1];
            }

            return wav;
        }
    }
}