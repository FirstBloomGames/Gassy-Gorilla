using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [CreateAssetMenu(fileName = "GG_ExpeditionCatalog", menuName = "First Bloom/Gassy Gorilla/Expedition Catalog")]
    public sealed class GassyExpeditionCatalog : ScriptableObject
    {
        [SerializeField] private GassyExpeditionDefinition[] expeditions = Array.Empty<GassyExpeditionDefinition>();

        public GassyExpeditionDefinition[] Expeditions { get { return expeditions; } }
        public int Count { get { return expeditions != null ? expeditions.Length : 0; } }

        public void Configure(GassyExpeditionDefinition[] definitions)
        {
            expeditions = definitions ?? Array.Empty<GassyExpeditionDefinition>();
        }

        public GassyExpeditionDefinition GetByIndex(int index)
        {
            return expeditions != null && index >= 0 && index < expeditions.Length
                ? expeditions[index]
                : null;
        }

        public GassyExpeditionDefinition FindById(string expeditionId)
        {
            if (expeditions == null || string.IsNullOrWhiteSpace(expeditionId))
            {
                return null;
            }

            for (int i = 0; i < expeditions.Length; i++)
            {
                GassyExpeditionDefinition definition = expeditions[i];
                if (definition != null &&
                    string.Equals(definition.ExpeditionId, expeditionId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public int IndexOf(GassyExpeditionDefinition definition)
        {
            if (expeditions == null || definition == null)
            {
                return -1;
            }

            for (int i = 0; i < expeditions.Length; i++)
            {
                if (expeditions[i] == definition)
                {
                    return i;
                }
            }

            return -1;
        }

        public void AppendValidationErrors(List<string> errors)
        {
            if (expeditions == null || expeditions.Length != 5)
            {
                errors.Add("Gassy Gorilla needs exactly five Version 1.0 Expeditions.");
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < expeditions.Length; i++)
            {
                GassyExpeditionDefinition definition = expeditions[i];
                if (definition == null)
                {
                    errors.Add("Expedition catalog has a missing definition at index " + i + ".");
                    continue;
                }

                if (definition.OrderIndex != i)
                {
                    errors.Add(definition.DisplayTitle + " has order " + definition.OrderIndex + " but appears at index " + i + ".");
                }

                if (!ids.Add(definition.ExpeditionId))
                {
                    errors.Add("Expedition id is duplicated: " + definition.ExpeditionId + ".");
                }

                definition.AppendValidationErrors(errors);
            }
        }
    }
}
