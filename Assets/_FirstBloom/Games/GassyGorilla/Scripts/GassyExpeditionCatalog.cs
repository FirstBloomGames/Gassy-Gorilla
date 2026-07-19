using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    [CreateAssetMenu(fileName = "GG_ExpeditionCatalog", menuName = "First Bloom/Gassy Gorilla/Expedition Catalog")]
    public sealed class GassyExpeditionCatalog : ScriptableObject
    {
        public const int LevelsPerChapter = 5;
        public const int VersionOneExpeditionCount = 10;

        [SerializeField] private GassyExpeditionDefinition[] expeditions = Array.Empty<GassyExpeditionDefinition>();

        public GassyExpeditionDefinition[] Expeditions { get { return expeditions; } }
        public int Count { get { return expeditions != null ? expeditions.Length : 0; } }
        public int ChapterCount
        {
            get
            {
                return Count == 0 ? 0 : Mathf.CeilToInt(Count / (float)LevelsPerChapter);
            }
        }

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

        public string GetChapterTitle(int chapterIndex)
        {
            GassyExpeditionDefinition definition =
                GetByIndex(chapterIndex * LevelsPerChapter);
            return definition != null ? definition.ChapterTitle : string.Empty;
        }

        public void AppendValidationErrors(List<string> errors)
        {
            if (expeditions == null || expeditions.Length != VersionOneExpeditionCount)
            {
                errors.Add("Gassy Gorilla needs exactly ten Version 1.0 Expeditions in two chapters.");
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            string[] chapterTitles = new string[ChapterCount];
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

                int expectedChapter = i / LevelsPerChapter;
                if (definition.ChapterIndex != expectedChapter)
                {
                    errors.Add(
                        definition.DisplayTitle + " belongs to chapter " +
                        definition.ChapterIndex + " but index " + i +
                        " requires chapter " + expectedChapter + ".");
                }

                if (string.IsNullOrWhiteSpace(chapterTitles[expectedChapter]))
                {
                    chapterTitles[expectedChapter] = definition.ChapterTitle;
                }
                else if (!string.Equals(
                    chapterTitles[expectedChapter],
                    definition.ChapterTitle,
                    StringComparison.Ordinal))
                {
                    errors.Add("Chapter " + (expectedChapter + 1) + " uses inconsistent player-facing titles.");
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
