using System;
using System.Collections.Generic;
using System.IO;
using FirstBloom.ArcadeFramework.Audio;
using UnityEditor;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaAudioAssetGenerator
    {
        private const string GameRoot = "Assets/_FirstBloom/Games/GassyGorilla";
        private const string MusicRoot = GameRoot + "/Audio/Music";
        private const string SfxRoot = GameRoot + "/Audio/SFX";
        private const string VoiceRoot = GameRoot + "/Audio/Voice";
        private const string LibraryPath = GameRoot + "/ScriptableObjects/GG_AudioLibrary.asset";
        private const int SampleRate = 32000;
        private const float BeatsPerMinute = 88f;
        private const int Bars = 8;
        private const int BeatsPerBar = 4;

        private static float LoopDuration
        {
            get { return 60f / BeatsPerMinute * BeatsPerBar * Bars; }
        }

        [MenuItem("First Bloom/Gassy Gorilla/Generate Production Audio")]
        public static ArcadeAudioLibrary GenerateProductionAudioAssets()
        {
            Directory.CreateDirectory(ToFullPath(MusicRoot));
            Directory.CreateDirectory(ToFullPath(SfxRoot));

            WriteWaveAsset(MusicRoot + "/GG_Music_JungleStride_Base.wav", GenerateBaseMusic(), 2, 0.47f);
            WriteWaveAsset(MusicRoot + "/GG_Music_JungleStride_Intensity.wav", GenerateIntensityMusic(), 2, 0.45f);
            WriteWaveAsset(MusicRoot + "/GG_Ambience_JungleWater.wav", GenerateAmbience(), 2, 0.3f);

            RequireAuthoredSfxFamily("Boost", 6);
            RequireAuthoredSfxFamily("BoostFailed", 3);
            GenerateSfxFamily("Pickup", 4, SfxKind.Pickup);
            GenerateSfxFamily("VineGrab", 2, SfxKind.VineGrab);
            GenerateSfxFamily("VineSwing", 2, SfxKind.VineSwing);
            GenerateSfxFamily("VineRelease", 3, SfxKind.VineRelease);
            GenerateSfxFamily("CrocodileWarning", 2, SfxKind.CrocodileWarning);
            GenerateSfxFamily("Splash", 2, SfxKind.Splash);
            GenerateSfxFamily("Chomp", 2, SfxKind.Chomp);
            GenerateSfxFamily("Crash", 2, SfxKind.Crash);
            GenerateSfxFamily("Milestone", 2, SfxKind.Milestone);
            GenerateSfxFamily("UIConfirm", 1, SfxKind.UiConfirm);
            GenerateSfxFamily("UIBack", 1, SfxKind.UiBack);
            GenerateSfxFamily("UIError", 1, SfxKind.UiError);
            GenerateSfxFamily("GameOver", 1, SfxKind.GameOver);
            GenerateSfxFamily("GeyserWarning", 2, SfxKind.GeyserWarning);
            GenerateSfxFamily("GeyserBurst", 2, SfxKind.GeyserBurst);
            GenerateSfxFamily("SapCatch", 2, SfxKind.SapCatch);
            GenerateSfxFamily("SapPop", 3, SfxKind.SapPop);
            GenerateSfxFamily("Updraft", 2, SfxKind.Updraft);
            GenerateSfxFamily("BounceBloom", 3, SfxKind.BounceBloom);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureMusicImporter(MusicRoot + "/GG_Music_JungleStride_Base.wav");
            ConfigureMusicImporter(MusicRoot + "/GG_Music_JungleStride_Intensity.wav");
            ConfigureMusicImporter(MusicRoot + "/GG_Ambience_JungleWater.wav");
            ConfigureAllSfxImporters();
            ConfigureAllVoiceImporters();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ArcadeAudioLibrary library = AssetDatabase.LoadAssetAtPath<ArcadeAudioLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<ArcadeAudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.Configure(
                LoadClip(MusicRoot + "/GG_Music_JungleStride_Base.wav"),
                LoadClip(MusicRoot + "/GG_Music_JungleStride_Intensity.wav"),
                LoadClip(MusicRoot + "/GG_Ambience_JungleWater.wav"),
                0.82f,
                0.72f,
                0.2f,
                BuildLibraryEntries());
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Generated the Gassy Gorilla production mix while preserving the authored comedic boost families.");
            return AssetDatabase.LoadAssetAtPath<ArcadeAudioLibrary>(LibraryPath);
        }

        private static ArcadeSfxEntry[] BuildLibraryEntries()
        {
            return new[]
            {
                Entry(
                    ArcadeSfxType.Boost,
                    "Boost",
                    6,
                    0.62f,
                    0.985f,
                    1.015f,
                    false,
                    3,
                    0f,
                    ArcadeSfxVoiceLimitMode.ReplaceOldest,
                    5,
                    8),
                Entry(
                    ArcadeSfxType.BoostFailed,
                    "BoostFailed",
                    3,
                    0.42f,
                    0.985f,
                    1.015f,
                    false,
                    2,
                    0.05f,
                    ArcadeSfxVoiceLimitMode.ReplaceOldest),
                Entry(
                    ArcadeSfxType.Pickup,
                    "Pickup",
                    4,
                    0.2f,
                    0.995f,
                    1.005f,
                    false,
                    1,
                    0.06f,
                    ArcadeSfxVoiceLimitMode.ReplaceOldest),
                Entry(ArcadeSfxType.VineGrab, "VineGrab", 2, 0.62f, 0.96f, 1.03f),
                Entry(ArcadeSfxType.VineSwing, "VineSwing", 2, 0.32f, 0.96f, 1.02f, true),
                Entry(ArcadeSfxType.VineRelease, "VineRelease", 3, 0.6f, 0.97f, 1.05f),
                Entry(ArcadeSfxType.Crash, "Crash", 2, 0.72f, 0.95f, 1.02f),
                Entry(ArcadeSfxType.UiClick, "UIConfirm", 1, 0.34f, 0.99f, 1.02f),
                Entry(ArcadeSfxType.UiBack, "UIBack", 1, 0.32f, 0.99f, 1.02f),
                Entry(ArcadeSfxType.UiError, "UIError", 1, 0.36f, 0.99f, 1.01f),
                Entry(ArcadeSfxType.Splash, "Splash", 2, 0.64f, 0.96f, 1.03f),
                Entry(ArcadeSfxType.Chomp, "Chomp", 2, 0.68f, 0.96f, 1.03f),
                Entry(ArcadeSfxType.CrocodileWarning, "CrocodileWarning", 2, 0.68f, 0.97f, 1.02f),
                Entry(ArcadeSfxType.Milestone, "Milestone", 2, 0.42f, 0.99f, 1.02f),
                Entry(ArcadeSfxType.GameOver, "GameOver", 1, 0.56f, 1f, 1f),
                Entry(ArcadeSfxType.GeyserWarning, "GeyserWarning", 2, 0.4f, 0.98f, 1.03f),
                Entry(ArcadeSfxType.GeyserBurst, "GeyserBurst", 2, 0.56f, 0.96f, 1.02f),
                Entry(ArcadeSfxType.SapCatch, "SapCatch", 2, 0.4f, 0.97f, 1.03f),
                Entry(ArcadeSfxType.SapPop, "SapPop", 3, 0.48f, 0.97f, 1.05f),
                Entry(ArcadeSfxType.Updraft, "Updraft", 2, 0.44f, 0.97f, 1.04f),
                Entry(
                    ArcadeSfxType.BounceBloom,
                    "BounceBloom",
                    3,
                    0.46f,
                    0.97f,
                    1.05f,
                    false,
                    1,
                    0.08f,
                    ArcadeSfxVoiceLimitMode.ReplaceOldest)
            };
        }

        private static ArcadeSfxEntry Entry(
            ArcadeSfxType type,
            string family,
            int count,
            float volume,
            float minPitch,
            float maxPitch,
            bool loop = false,
            int maximumSimultaneousVoices = 0,
            float minimumRetriggerInterval = 0f,
            ArcadeSfxVoiceLimitMode voiceLimitMode = ArcadeSfxVoiceLimitMode.ReplaceOldest,
            int rareClipIndex = -1,
            int rareClipCooldownPlays = 0)
        {
            return new ArcadeSfxEntry(
                type,
                LoadSfxFamily(family, count),
                volume,
                new Vector2(minPitch, maxPitch),
                loop,
                maximumSimultaneousVoices,
                minimumRetriggerInterval,
                voiceLimitMode,
                rareClipIndex,
                rareClipCooldownPlays);
        }

        private static void GenerateSfxFamily(string family, int count, SfxKind kind)
        {
            for (int i = 0; i < count; i++)
            {
                float[] samples = GenerateSfx(kind, i);
                string path = SfxRoot + "/GG_SFX_" + family + "_" + (i + 1).ToString("D2") + ".wav";
                WriteWaveAsset(path, samples, 1, 0.5f);
            }
        }

        private static void RequireAuthoredSfxFamily(string family, int count)
        {
            for (int i = 0; i < count; i++)
            {
                string path = SfxRoot + "/GG_SFX_" + family + "_" + (i + 1).ToString("D2") + ".wav";
                if (!File.Exists(ToFullPath(path)))
                {
                    throw new InvalidOperationException("Missing authored production SFX: " + path + ".");
                }
            }
        }

        private static float[] GenerateBaseMusic()
        {
            float[] data = NewBuffer(LoopDuration, 2);
            System.Random random = new System.Random(31051985);
            int[] roots = { 48, 53, 57, 55, 48, 53, 55, 48 };
            int[] melody = { 12, 16, 19, 24, 19, 16, 14, 19, 12, 17, 21, 24, 21, 17, 16, 14 };
            float eighth = 60f / BeatsPerMinute * 0.5f;

            for (int step = 0; step < Bars * BeatsPerBar * 2; step++)
            {
                int bar = step / 8;
                int root = roots[Mathf.Clamp(bar, 0, roots.Length - 1)];
                float start = step * eighth;
                float pan = (step % 4 - 1.5f) * 0.18f;
                AddMarimba(data, 2, start, 0.34f, MidiToFrequency(root + melody[step % melody.Length]), 0.115f, pan);

                if ((step & 1) == 0)
                {
                    int quarter = step / 2;
                    AddBass(data, 2, start, 0.5f, MidiToFrequency(root), 0.115f, -0.08f);
                    AddHandDrum(data, 2, start, 0.18f, quarter % 4 == 0 ? 112f : 164f, 0.08f, random, 0f);
                }

                if (step % 4 == 2)
                {
                    AddWoodClick(data, 2, start, 0.09f, 520f + bar * 11f, 0.055f, random, 0.18f);
                }

                if ((step & 1) == 1)
                {
                    AddShaker(data, 2, start, 0.075f, 0.025f, random, -0.25f);
                }
            }

            AddMarimba(data, 2, 0f, 0.65f, MidiToFrequency(72), 0.055f, 0.32f);
            ApplyEdgeFade(data, 2, 0.006f);
            return data;
        }

        private static float[] GenerateIntensityMusic()
        {
            float[] data = NewBuffer(LoopDuration, 2);
            System.Random random = new System.Random(27182818);
            float sixteenth = 60f / BeatsPerMinute * 0.25f;

            for (int step = 0; step < Bars * BeatsPerBar * 4; step++)
            {
                float start = step * sixteenth;
                float shakerAmplitude = step % 4 == 2 ? 0.055f : 0.037f;
                AddShaker(data, 2, start, 0.065f, shakerAmplitude, random, step % 2 == 0 ? -0.42f : 0.42f);

                if (step % 8 == 3 || step % 8 == 7)
                {
                    AddHandDrum(data, 2, start, 0.16f, step % 8 == 3 ? 190f : 145f, 0.075f, random, 0.25f);
                }

                if (step % 32 >= 28)
                {
                    float fillPitch = 210f - (step % 32 - 28) * 26f;
                    AddHandDrum(data, 2, start, 0.18f, fillPitch, 0.09f, random, -0.2f);
                }
            }

            float barDuration = 60f / BeatsPerMinute * BeatsPerBar;
            AddHeroicHorn(data, 2, barDuration * 3f, 0.72f, MidiToFrequency(67), 0.07f, -0.12f);
            AddHeroicHorn(data, 2, barDuration * 3f + 0.36f, 0.64f, MidiToFrequency(72), 0.065f, 0.12f);
            AddHeroicHorn(data, 2, barDuration * 7f, 0.62f, MidiToFrequency(67), 0.068f, -0.12f);
            AddHeroicHorn(data, 2, barDuration * 7f + 0.34f, 0.55f, MidiToFrequency(72), 0.06f, 0.12f);
            ApplyEdgeFade(data, 2, 0.008f);
            return data;
        }

        private static float[] GenerateAmbience()
        {
            float[] data = NewBuffer(LoopDuration, 2);
            int frames = data.Length / 2;
            for (int frame = 0; frame < frames; frame++)
            {
                float t = frame / (float)SampleRate;
                float phase = t / LoopDuration * Mathf.PI * 2f;
                float waterLeft =
                    Mathf.Sin(phase * 3f + 0.4f) * 0.028f +
                    Mathf.Sin(phase * 11f + 1.2f) * 0.014f +
                    Mathf.Sin(phase * 37f + 2.1f) * 0.006f;
                float waterRight =
                    Mathf.Sin(phase * 5f + 1.6f) * 0.026f +
                    Mathf.Sin(phase * 13f + 0.2f) * 0.013f +
                    Mathf.Sin(phase * 41f + 2.7f) * 0.006f;
                data[frame * 2] += waterLeft;
                data[frame * 2 + 1] += waterRight;
            }

            AddBubble(data, 2, 3.2f, 0.42f, 310f, 0.045f, -0.45f);
            AddBubble(data, 2, 3.45f, 0.28f, 440f, 0.03f, -0.28f);
            AddBirdChirp(data, 2, 7.8f, 0.42f, 1180f, 1660f, 0.028f, 0.5f);
            AddBubble(data, 2, 13.6f, 0.36f, 350f, 0.04f, 0.35f);
            AddBirdChirp(data, 2, 17.25f, 0.34f, 1420f, 1040f, 0.024f, -0.48f);
            ApplyEdgeFade(data, 2, 0.02f);
            return data;
        }

        private static float[] GenerateSfx(SfxKind kind, int variant)
        {
            System.Random random = new System.Random(9001 + (int)kind * 977 + variant * 131);
            switch (kind)
            {
                case SfxKind.Boost:
                    return GenerateBoost(variant, random);
                case SfxKind.BoostFailed:
                    return GenerateBoostFailed(variant, random);
                case SfxKind.Pickup:
                    return GeneratePickup(variant);
                case SfxKind.VineGrab:
                    return GenerateVineGrab(variant, random);
                case SfxKind.VineSwing:
                    return GenerateVineSwing(variant, random);
                case SfxKind.VineRelease:
                    return GenerateVineRelease(variant, random);
                case SfxKind.CrocodileWarning:
                    return GenerateCrocodileWarning(variant, random);
                case SfxKind.Splash:
                    return GenerateSplash(variant, random);
                case SfxKind.Chomp:
                    return GenerateChomp(variant, random);
                case SfxKind.Crash:
                    return GenerateCrash(variant, random);
                case SfxKind.Milestone:
                    return GenerateMilestone(variant);
                case SfxKind.UiConfirm:
                    return GenerateUiConfirm();
                case SfxKind.UiBack:
                    return GenerateUiBack();
                case SfxKind.UiError:
                    return GenerateUiError();
                case SfxKind.GeyserWarning:
                    return GenerateGeyserWarning(variant, random);
                case SfxKind.GeyserBurst:
                    return GenerateGeyserBurst(variant, random);
                case SfxKind.SapCatch:
                    return GenerateSapCatch(variant, random);
                case SfxKind.SapPop:
                    return GenerateSapPop(variant, random);
                case SfxKind.Updraft:
                    return GenerateUpdraft(variant, random);
                case SfxKind.BounceBloom:
                    return GenerateBounceBloom(variant, random);
                default:
                    return GenerateGameOver();
            }
        }

        private static float[] GenerateBoost(int variant, System.Random random)
        {
            float duration = 0.36f + variant * 0.035f;
            float[] data = NewBuffer(duration, 1);
            AddPitchSweep(data, 1, 0f, duration * 0.9f, 150f + variant * 13f, 52f + variant * 3f, 0.34f, 0f);
            AddNoiseBurst(data, 1, 0.015f, duration * 0.8f, 0.2f + variant * 0.018f, random, 0.3f, 0f);
            AddHandDrum(data, 1, 0f, 0.12f, 118f + variant * 9f, 0.2f, random, 0f);
            AddBubble(data, 1, 0.13f + variant * 0.012f, 0.16f, 92f + variant * 5f, 0.12f, 0f);
            ApplyEdgeFade(data, 1, 0.004f);
            return data;
        }

        private static float[] GenerateBoostFailed(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.34f, 1);
            int puffs = 2 + variant;
            for (int i = 0; i < puffs; i++)
            {
                float start = 0.03f + i * 0.085f;
                AddPitchSweep(data, 1, start, 0.09f, 105f - i * 9f, 54f, 0.17f, 0f);
                AddNoiseBurst(data, 1, start, 0.075f, 0.11f, random, 0.35f, 0f);
            }

            return data;
        }

        private static float[] GeneratePickup(int variant)
        {
            float[] data = NewBuffer(0.16f + variant * 0.006f, 1);
            int root = 69 + variant * 2;
            AddMarimba(data, 1, 0f, 0.13f, MidiToFrequency(root), 0.17f, 0f);
            AddBell(data, 1, 0.04f, 0.11f, MidiToFrequency(root + 4), 0.065f, 0f);
            ApplyEdgeFade(data, 1, 0.003f);
            return data;
        }

        private static float[] GenerateVineGrab(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.38f, 1);
            AddWoodClick(data, 1, 0f, 0.11f, 430f + variant * 55f, 0.24f, random, 0f);
            AddPitchSweep(data, 1, 0.02f, 0.28f, 290f + variant * 18f, 170f, 0.18f, 0f);
            AddNoiseBurst(data, 1, 0.035f, 0.16f, 0.12f, random, 0.55f, 0f);
            AddBell(data, 1, 0.08f, 0.25f, 610f + variant * 40f, 0.08f, 0f);
            return data;
        }

        private static float[] GenerateVineSwing(int variant, System.Random random)
        {
            const float duration = 1.6f;
            float[] data = NewBuffer(duration, 1);
            for (int i = 0; i < 4; i++)
            {
                float start = 0.14f + i * 0.38f;
                AddPitchSweep(data, 1, start, 0.2f, 260f + variant * 25f + i * 8f, 185f, 0.075f, 0f);
                AddNoiseBurst(data, 1, start, 0.16f, 0.045f, random, 0.72f, 0f);
            }

            ApplyEdgeFade(data, 1, 0.035f);
            return data;
        }

        private static float[] GenerateVineRelease(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.48f, 1);
            AddNoiseBurst(data, 1, 0f, 0.34f, 0.22f, random, 0.08f, 0f);
            AddPitchSweep(data, 1, 0f, 0.31f, 260f + variant * 35f, 780f + variant * 45f, 0.15f, 0f);
            AddBell(data, 1, 0.045f, 0.34f, 430f + variant * 24f, 0.1f, 0f);
            return data;
        }

        private static float[] GenerateCrocodileWarning(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.78f, 1);
            AddPitchSweep(data, 1, 0f, 0.7f, 82f + variant * 7f, 58f, 0.16f, 0f);
            AddBubble(data, 1, 0.08f, 0.24f, 260f + variant * 30f, 0.17f, 0f);
            AddBubble(data, 1, 0.29f, 0.2f, 350f + variant * 24f, 0.14f, 0f);
            AddBubble(data, 1, 0.48f, 0.17f, 470f + variant * 18f, 0.11f, 0f);
            AddNoiseBurst(data, 1, 0.03f, 0.7f, 0.075f, random, 0.2f, 0f);
            return data;
        }

        private static float[] GenerateSplash(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.76f + variant * 0.08f, 1);
            AddNoiseBurst(data, 1, 0f, 0.58f, 0.34f, random, 0.18f, 0f);
            AddPitchSweep(data, 1, 0f, 0.42f, 210f + variant * 18f, 68f, 0.17f, 0f);
            for (int i = 0; i < 4; i++)
            {
                AddBubble(data, 1, 0.16f + i * 0.1f, 0.14f, 340f + i * 85f, 0.07f, 0f);
            }

            return data;
        }

        private static float[] GenerateChomp(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.44f, 1);
            AddWoodClick(data, 1, 0.025f, 0.1f, 220f + variant * 22f, 0.31f, random, 0f);
            AddWoodClick(data, 1, 0.115f, 0.12f, 155f + variant * 15f, 0.28f, random, 0f);
            AddHandDrum(data, 1, 0.1f, 0.28f, 92f, 0.18f, random, 0f);
            return data;
        }

        private static float[] GenerateCrash(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.62f, 1);
            AddHandDrum(data, 1, 0f, 0.42f, 86f + variant * 8f, 0.28f, random, 0f);
            AddWoodClick(data, 1, 0.02f, 0.18f, 170f + variant * 26f, 0.2f, random, 0f);
            AddNoiseBurst(data, 1, 0f, 0.34f, 0.16f, random, 0.4f, 0f);
            return data;
        }

        private static float[] GenerateMilestone(int variant)
        {
            float[] data = NewBuffer(0.72f, 1);
            int root = variant == 0 ? 67 : 65;
            AddHeroicHorn(data, 1, 0f, 0.48f, MidiToFrequency(root), 0.18f, 0f);
            AddHeroicHorn(data, 1, 0.13f, 0.46f, MidiToFrequency(root + 4), 0.17f, 0f);
            AddHeroicHorn(data, 1, 0.27f, 0.42f, MidiToFrequency(root + 7), 0.16f, 0f);
            return data;
        }

        private static float[] GenerateUiConfirm()
        {
            float[] data = NewBuffer(0.16f, 1);
            AddBell(data, 1, 0f, 0.15f, 760f, 0.18f, 0f);
            AddBell(data, 1, 0.035f, 0.12f, 1020f, 0.12f, 0f);
            return data;
        }

        private static float[] GenerateUiBack()
        {
            float[] data = NewBuffer(0.17f, 1);
            AddBell(data, 1, 0f, 0.15f, 680f, 0.15f, 0f);
            AddBell(data, 1, 0.04f, 0.12f, 510f, 0.11f, 0f);
            return data;
        }

        private static float[] GenerateUiError()
        {
            float[] data = NewBuffer(0.22f, 1);
            AddPitchSweep(data, 1, 0f, 0.2f, 210f, 145f, 0.2f, 0f);
            AddBell(data, 1, 0.035f, 0.17f, 265f, 0.09f, 0f);
            return data;
        }

        private static float[] GenerateGameOver()
        {
            float[] data = NewBuffer(1.14f, 1);
            int[] notes = { 67, 63, 60, 55 };
            for (int i = 0; i < notes.Length; i++)
            {
                AddMarimba(data, 1, i * 0.22f, 0.42f, MidiToFrequency(notes[i]), 0.19f - i * 0.02f, 0f);
            }

            AddBass(data, 1, 0.7f, 0.42f, MidiToFrequency(43), 0.11f, 0f);
            return data;
        }

        private static float[] GenerateGeyserWarning(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.72f, 1);
            for (int i = 0; i < 4; i++)
            {
                float start = 0.04f + i * 0.15f;
                AddBubble(
                    data,
                    1,
                    start,
                    0.16f,
                    240f + variant * 22f + i * 54f,
                    0.12f,
                    0f);
            }

            AddPitchSweep(data, 1, 0.08f, 0.58f, 118f, 184f + variant * 12f, 0.09f, 0f);
            AddNoiseBurst(data, 1, 0.02f, 0.62f, 0.045f, random, 0.5f, 0f);
            return data;
        }

        private static float[] GenerateGeyserBurst(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.64f + variant * 0.04f, 1);
            AddNoiseBurst(data, 1, 0f, 0.56f, 0.3f, random, 0.18f, 0f);
            AddPitchSweep(data, 1, 0f, 0.48f, 128f + variant * 12f, 54f, 0.22f, 0f);
            AddHandDrum(data, 1, 0.015f, 0.3f, 82f + variant * 7f, 0.18f, random, 0f);
            AddBubble(data, 1, 0.22f, 0.25f, 170f + variant * 20f, 0.12f, 0f);
            return data;
        }

        private static float[] GenerateSapCatch(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.42f, 1);
            AddPitchSweep(data, 1, 0f, 0.36f, 280f + variant * 24f, 84f, 0.2f, 0f);
            AddNoiseBurst(data, 1, 0f, 0.31f, 0.13f, random, 0.72f, 0f);
            AddBubble(data, 1, 0.09f, 0.24f, 128f + variant * 18f, 0.16f, 0f);
            AddBubble(data, 1, 0.21f, 0.17f, 188f + variant * 26f, 0.1f, 0f);
            return data;
        }

        private static float[] GenerateSapPop(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.36f + variant * 0.025f, 1);
            AddWoodClick(
                data,
                1,
                0.015f,
                0.095f,
                310f + variant * 52f,
                0.24f,
                random,
                0f);
            AddBubble(
                data,
                1,
                0.025f,
                0.18f,
                180f + variant * 35f,
                0.16f,
                0f);
            AddPitchSweep(
                data,
                1,
                0.055f,
                0.25f,
                230f + variant * 28f,
                520f + variant * 68f,
                0.13f,
                0f);
            return data;
        }

        private static float[] GenerateUpdraft(int variant, System.Random random)
        {
            float[] data = NewBuffer(0.72f, 1);
            AddNoiseBurst(data, 1, 0f, 0.62f, 0.14f, random, 0.04f, 0f);
            AddPitchSweep(
                data,
                1,
                0.02f,
                0.58f,
                260f + variant * 34f,
                760f + variant * 76f,
                0.12f,
                0f);
            AddBell(data, 1, 0.22f, 0.36f, 690f + variant * 90f, 0.07f, 0f);
            return data;
        }

        private static float[] GenerateBounceBloom(
            int variant,
            System.Random random)
        {
            float[] data = NewBuffer(0.52f + variant * 0.025f, 1);
            AddPitchSweep(
                data,
                1,
                0f,
                0.38f,
                118f + variant * 12f,
                360f + variant * 38f,
                0.24f,
                0f);
            AddWoodClick(
                data,
                1,
                0.015f,
                0.11f,
                280f + variant * 36f,
                0.17f,
                random,
                0f);
            AddNoiseBurst(
                data,
                1,
                0.04f,
                0.32f,
                0.09f,
                random,
                0.25f,
                0f);
            AddBell(
                data,
                1,
                0.17f,
                0.28f,
                510f + variant * 52f,
                0.055f,
                0f);
            ApplyEdgeFade(data, 1, 0.004f);
            return data;
        }

        private static float[] NewBuffer(float duration, int channels)
        {
            int frames = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            return new float[frames * Mathf.Max(1, channels)];
        }

        private static void AddMarimba(
            float[] data,
            int channels,
            float start,
            float duration,
            float frequency,
            float amplitude,
            float pan)
        {
            Mix(data, channels, start, duration, pan, (t, _) =>
            {
                float attack = 1f - Mathf.Exp(-t * 95f);
                float envelope = attack * Mathf.Exp(-t * 8.4f);
                float phase = Mathf.PI * 2f * frequency * t;
                float tone =
                    Mathf.Sin(phase) * 0.78f +
                    Mathf.Sin(phase * 3.02f) * 0.17f +
                    Mathf.Sin(phase * 6.01f) * 0.05f;
                return tone * envelope * amplitude;
            });
        }

        private static void AddBass(
            float[] data,
            int channels,
            float start,
            float duration,
            float frequency,
            float amplitude,
            float pan)
        {
            Mix(data, channels, start, duration, pan, (t, _) =>
            {
                float envelope = (1f - Mathf.Exp(-t * 65f)) * Mathf.Exp(-t * 3.8f);
                float phase = Mathf.PI * 2f * frequency * t;
                return (Mathf.Sin(phase) * 0.82f + Mathf.Sin(phase * 2f) * 0.18f) * envelope * amplitude;
            });
        }

        private static void AddBell(
            float[] data,
            int channels,
            float start,
            float duration,
            float frequency,
            float amplitude,
            float pan)
        {
            Mix(data, channels, start, duration, pan, (t, _) =>
            {
                float envelope = Mathf.Exp(-t * 10f);
                float phase = Mathf.PI * 2f * frequency * t;
                float tone = Mathf.Sin(phase) * 0.64f + Mathf.Sin(phase * 2.7f) * 0.23f + Mathf.Sin(phase * 4.1f) * 0.13f;
                return tone * envelope * amplitude;
            });
        }

        private static void AddHeroicHorn(
            float[] data,
            int channels,
            float start,
            float duration,
            float frequency,
            float amplitude,
            float pan)
        {
            Mix(data, channels, start, duration, pan, (t, normalized) =>
            {
                float attack = Mathf.Clamp01(t / 0.055f);
                float release = Mathf.Clamp01((1f - normalized) / 0.25f);
                float envelope = Mathf.SmoothStep(0f, 1f, attack) * Mathf.SmoothStep(0f, 1f, release);
                float vibrato = 1f + Mathf.Sin(t * Mathf.PI * 2f * 5.2f) * 0.004f;
                float phase = Mathf.PI * 2f * frequency * vibrato * t;
                float tone = Mathf.Sin(phase) * 0.62f + Mathf.Sin(phase * 2f) * 0.25f + Mathf.Sin(phase * 3f) * 0.13f;
                return tone * envelope * amplitude;
            });
        }

        private static void AddPitchSweep(
            float[] data,
            int channels,
            float start,
            float duration,
            float startFrequency,
            float endFrequency,
            float amplitude,
            float pan)
        {
            Mix(data, channels, start, duration, pan, (t, normalized) =>
            {
                float frequency = Mathf.Lerp(startFrequency, endFrequency, normalized);
                float phase = Mathf.PI * 2f * frequency * t;
                float envelope = (1f - Mathf.Exp(-t * 75f)) * Mathf.Pow(1f - normalized, 1.5f);
                return (Mathf.Sin(phase) * 0.82f + Mathf.Sin(phase * 0.5f) * 0.18f) * envelope * amplitude;
            });
        }

        private static void AddHandDrum(
            float[] data,
            int channels,
            float start,
            float duration,
            float frequency,
            float amplitude,
            System.Random random,
            float pan)
        {
            float noiseState = 0f;
            Mix(data, channels, start, duration, pan, (t, normalized) =>
            {
                float envelope = Mathf.Exp(-t * 18f) * Mathf.Clamp01(t * 160f);
                float sweptFrequency = Mathf.Lerp(frequency * 1.45f, frequency, normalized);
                float tone = Mathf.Sin(Mathf.PI * 2f * sweptFrequency * t);
                float noise = NextNoise(random);
                noiseState = Mathf.Lerp(noiseState, noise, 0.2f);
                return (tone * 0.82f + noiseState * 0.18f) * envelope * amplitude;
            });
        }

        private static void AddWoodClick(
            float[] data,
            int channels,
            float start,
            float duration,
            float frequency,
            float amplitude,
            System.Random random,
            float pan)
        {
            Mix(data, channels, start, duration, pan, (t, _) =>
            {
                float envelope = Mathf.Exp(-t * 34f);
                float phase = Mathf.PI * 2f * frequency * t;
                return (Mathf.Sin(phase) * 0.72f + Mathf.Sin(phase * 1.73f) * 0.18f + NextNoise(random) * 0.1f) * envelope * amplitude;
            });
        }

        private static void AddShaker(
            float[] data,
            int channels,
            float start,
            float duration,
            float amplitude,
            System.Random random,
            float pan)
        {
            float previousNoise = 0f;
            Mix(data, channels, start, duration, pan, (t, normalized) =>
            {
                float noise = NextNoise(random);
                float highPass = noise - previousNoise * 0.92f;
                previousNoise = noise;
                float envelope = Mathf.Sin(Mathf.Clamp01(normalized) * Mathf.PI) * Mathf.Exp(-t * 8f);
                return highPass * envelope * amplitude;
            });
        }

        private static void AddNoiseBurst(
            float[] data,
            int channels,
            float start,
            float duration,
            float amplitude,
            System.Random random,
            float smoothing,
            float pan)
        {
            float filtered = 0f;
            Mix(data, channels, start, duration, pan, (t, normalized) =>
            {
                filtered = Mathf.Lerp(filtered, NextNoise(random), Mathf.Clamp01(1f - smoothing));
                float envelope = Mathf.Clamp01(t * 90f) * Mathf.Pow(1f - normalized, 1.4f);
                return filtered * envelope * amplitude;
            });
        }

        private static void AddBubble(
            float[] data,
            int channels,
            float start,
            float duration,
            float frequency,
            float amplitude,
            float pan)
        {
            Mix(data, channels, start, duration, pan, (t, normalized) =>
            {
                float sweptFrequency = frequency * (1f + normalized * 0.7f);
                float phase = Mathf.PI * 2f * sweptFrequency * t;
                float envelope = Mathf.Sin(normalized * Mathf.PI) * Mathf.Pow(1f - normalized, 0.4f);
                return Mathf.Sin(phase) * envelope * amplitude;
            });
        }

        private static void AddBirdChirp(
            float[] data,
            int channels,
            float start,
            float duration,
            float startFrequency,
            float endFrequency,
            float amplitude,
            float pan)
        {
            Mix(data, channels, start, duration, pan, (t, normalized) =>
            {
                float frequency = Mathf.Lerp(startFrequency, endFrequency, Mathf.SmoothStep(0f, 1f, normalized));
                float phase = Mathf.PI * 2f * frequency * t;
                float tremolo = 0.7f + Mathf.Sin(t * Mathf.PI * 2f * 18f) * 0.3f;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                return Mathf.Sin(phase) * tremolo * envelope * amplitude;
            });
        }

        private static void Mix(
            float[] data,
            int channels,
            float start,
            float duration,
            float pan,
            Func<float, float, float> sampleFunction)
        {
            int totalFrames = data.Length / channels;
            int startFrame = Mathf.Clamp(Mathf.RoundToInt(start * SampleRate), 0, totalFrames);
            int frameCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            int endFrame = Mathf.Min(totalFrames, startFrame + frameCount);
            float leftGain = channels == 1 ? 1f : Mathf.Sqrt(Mathf.Clamp01((1f - pan) * 0.5f));
            float rightGain = channels == 1 ? 0f : Mathf.Sqrt(Mathf.Clamp01((1f + pan) * 0.5f));

            for (int frame = startFrame; frame < endFrame; frame++)
            {
                int localFrame = frame - startFrame;
                float t = localFrame / (float)SampleRate;
                float normalized = localFrame / (float)Mathf.Max(1, frameCount - 1);
                float sample = sampleFunction(t, normalized);
                int index = frame * channels;
                data[index] += sample * leftGain;
                if (channels > 1)
                {
                    data[index + 1] += sample * rightGain;
                }
            }
        }

        private static void ApplyEdgeFade(float[] data, int channels, float duration)
        {
            int frames = data.Length / channels;
            int fadeFrames = Mathf.Clamp(Mathf.RoundToInt(duration * SampleRate), 1, Mathf.Max(1, frames / 2));
            for (int frame = 0; frame < fadeFrames; frame++)
            {
                float gain = Mathf.SmoothStep(0f, 1f, frame / (float)fadeFrames);
                int endFrame = frames - 1 - frame;
                for (int channel = 0; channel < channels; channel++)
                {
                    data[frame * channels + channel] *= gain;
                    data[endFrame * channels + channel] *= gain;
                }
            }
        }

        private static float MidiToFrequency(int midiNote)
        {
            return 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
        }

        private static float NextNoise(System.Random random)
        {
            return (float)(random.NextDouble() * 2d - 1d);
        }

        private static void WriteWaveAsset(
            string assetPath,
            float[] samples,
            int channels,
            float targetPeak)
        {
            Normalize(samples, channels, targetPeak);
            byte[] bytes = EncodePcm16Wave(samples, channels, SampleRate);
            string fullPath = ToFullPath(assetPath);
            if (File.Exists(fullPath))
            {
                byte[] existing = File.ReadAllBytes(fullPath);
                if (ByteArraysEqual(existing, bytes))
                {
                    return;
                }
            }

            File.WriteAllBytes(fullPath, bytes);
        }

        private static void Normalize(float[] samples, int channels, float targetPeak)
        {
            int safeChannels = Mathf.Max(1, channels);
            int frames = samples.Length / safeChannels;
            double[] channelSums = new double[safeChannels];
            for (int i = 0; i < samples.Length; i++)
            {
                channelSums[i % safeChannels] += samples[i];
            }

            for (int i = 0; i < samples.Length; i++)
            {
                float mean = frames > 0 ? (float)(channelSums[i % safeChannels] / frames) : 0f;
                samples[i] -= mean;
            }

            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }

            if (peak <= 0.00001f)
            {
                return;
            }

            float gain = Mathf.Max(0.01f, targetPeak) / peak;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Clamp(samples[i] * gain, -0.999f, 0.999f);
            }
        }

        private static byte[] EncodePcm16Wave(float[] samples, int channels, int sampleRate)
        {
            using (MemoryStream stream = new MemoryStream(44 + samples.Length * 2))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                int dataSize = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                for (int i = 0; i < samples.Length; i++)
                {
                    short value = (short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                    writer.Write(value);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static bool ByteArraysEqual(byte[] first, byte[] second)
        {
            if (first == null || second == null || first.Length != second.Length)
            {
                return false;
            }

            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void ConfigureMusicImporter(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Unable to configure generated music: " + path);
            }

            importer.forceToMono = false;
            importer.loadInBackground = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.38f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SetOverrideSampleSettings("WebGL", settings);
            importer.SaveAndReimport();
        }

        private static void ConfigureAllSfxImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.forceToMono = true;
                importer.loadInBackground = false;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.48f;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SetOverrideSampleSettings("WebGL", settings);
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureAllVoiceImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { VoiceRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.forceToMono = true;
                importer.loadInBackground = false;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.52f;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SetOverrideSampleSettings("WebGL", settings);
                importer.SaveAndReimport();
            }
        }

        private static AudioClip[] LoadSfxFamily(string family, int count)
        {
            AudioClip[] clips = new AudioClip[count];
            for (int i = 0; i < count; i++)
            {
                string path = SfxRoot + "/GG_SFX_" + family + "_" + (i + 1).ToString("D2") + ".wav";
                clips[i] = LoadClip(path);
            }

            return clips;
        }

        private static AudioClip LoadClip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                throw new InvalidOperationException("Generated audio clip failed to import: " + path);
            }

            return clip;
        }

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private enum SfxKind
        {
            Boost,
            BoostFailed,
            Pickup,
            VineGrab,
            VineSwing,
            VineRelease,
            CrocodileWarning,
            Splash,
            Chomp,
            Crash,
            Milestone,
            UiConfirm,
            UiBack,
            UiError,
            GameOver,
            GeyserWarning,
            GeyserBurst,
            SapCatch,
            SapPop,
            Updraft,
            BounceBloom
        }
    }
}
