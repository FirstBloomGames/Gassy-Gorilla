using System;
using UnityEditor;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla.EditorTools
{
    public static class GassyGorillaParticleRepair
    {
        private const string GameRoot = "Assets/_FirstBloom/Games/GassyGorilla";

        [MenuItem("First Bloom/Gassy Gorilla/Repair Particle Compatibility")]
        public static void RepairProjectAssets()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { GameRoot });
            int repairedParticles = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    int repairedInPrefab = RepairParticles(root);
                    if (repairedInPrefab == 0)
                    {
                        continue;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    repairedParticles += repairedInPrefab;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Gassy Gorilla particle compatibility repair updated " + repairedParticles + " particle systems.");
        }

        private static int RepairParticles(GameObject root)
        {
            int repaired = 0;
            ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                ParticleSystem.VelocityOverLifetimeModule velocity = particle.velocityOverLifetime;
                if (!velocity.enabled || AxesUseOneMode(velocity))
                {
                    continue;
                }

                if (!UsesConstantMode(velocity.x)
                    || !UsesConstantMode(velocity.y)
                    || !UsesConstantMode(velocity.z))
                {
                    Debug.LogWarning(
                        "Skipped particle velocity repair for " + particle.name
                        + " because it uses animation curves.",
                        particle);
                    continue;
                }

                velocity.x = AsTwoConstants(velocity.x);
                velocity.y = AsTwoConstants(velocity.y);
                velocity.z = AsTwoConstants(velocity.z);
                EditorUtility.SetDirty(particle);
                repaired++;
            }

            return repaired;
        }

        private static bool AxesUseOneMode(ParticleSystem.VelocityOverLifetimeModule velocity)
        {
            return velocity.x.mode == velocity.y.mode && velocity.y.mode == velocity.z.mode;
        }

        private static bool UsesConstantMode(ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode == ParticleSystemCurveMode.Constant
                || curve.mode == ParticleSystemCurveMode.TwoConstants;
        }

        private static ParticleSystem.MinMaxCurve AsTwoConstants(ParticleSystem.MinMaxCurve curve)
        {
            if (curve.mode == ParticleSystemCurveMode.Constant)
            {
                return new ParticleSystem.MinMaxCurve(curve.constant, curve.constant);
            }

            return new ParticleSystem.MinMaxCurve(curve.constantMin, curve.constantMax);
        }
    }
}
