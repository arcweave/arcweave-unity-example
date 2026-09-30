using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Arcweave.Project;
using UnityEngine;

namespace Arcweave
{
    /// <summary>Stores one demo save slot and restores only compatible project data.</summary>
    public sealed class ArcweaveProgressStore
    {
        private const int CurrentVersion = 1;
        private readonly string key;

        [Serializable]
        private sealed class Snapshot
        {
            public int version;
            public string[] boardIds;
            public string currentElement;
            public VariableValue[] variables;
        }

        [Serializable]
        private sealed class VariableValue
        {
            public string id;
            public string type;
            public string value;
        }

        public ArcweaveProgressStore(string key)
        {
            this.key = key;
        }

        public bool HasSavedProgress => PlayerPrefs.HasKey(key + "_progress") ||
                                        PlayerPrefs.HasKey(key + "_variables");

        public void Save(Project.Project project, string elementId)
        {
            // Keep the plugin's variable format, with demo-owned metadata around it.
            var snapshot = JsonUtility.FromJson<Snapshot>(project.SaveVariables());
            snapshot.version = CurrentVersion;
            snapshot.boardIds = project.Boards.Select(board => board.Id).ToArray();
            snapshot.currentElement = elementId;
            PlayerPrefs.SetString(key + "_progress", JsonUtility.ToJson(snapshot));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Restores matching IDs and types. New or incompatible variables retain their defaults.
        /// A missing saved element does not prevent restoring the variables.
        /// </summary>
        public bool TryRestore(Project.Project project, out string elementId)
        {
            elementId = null;
            bool legacy = !PlayerPrefs.HasKey(key + "_progress");
            string dataKey = key + (legacy ? "_variables" : "_progress");
            if (!PlayerPrefs.HasKey(dataKey)) return false;

            Snapshot snapshot;
            try
            {
                snapshot = JsonUtility.FromJson<Snapshot>(PlayerPrefs.GetString(dataKey));
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Ignoring unreadable Arcweave saved progress.");
                return false;
            }

            if (snapshot?.variables == null)
            {
                Debug.LogWarning("Ignoring Arcweave saved progress with missing variable data.");
                return false;
            }

            var variables = project.GetAllVariables().ToDictionary(variable => variable.Id);
            if (legacy)
            {
                snapshot.currentElement = PlayerPrefs.GetString(key + "_currentElement", "");
                // Old saves have no project metadata. Require a shared exported ID before migration.
                bool knownElement = FindElement(project, snapshot.currentElement) != null;
                bool knownVariable = snapshot.variables.Any(value => value != null &&
                    !string.IsNullOrEmpty(value.id) && variables.ContainsKey(value.id));
                if (!knownElement && !knownVariable) return false;
            }
            else
            {
                // Board GUIDs survive text edits, renames, and variable schema changes.
                // At least one board must remain shared; a wholly replaced project starts fresh.
                if (snapshot.version != CurrentVersion || snapshot.boardIds == null ||
                    !project.Boards.Any(board => snapshot.boardIds.Contains(board.Id))) return false;
            }

            project.ResetVariablesToDefaultValues();
            var restoredIds = new HashSet<string>();
            int skipped = 0;
            foreach (var saved in snapshot.variables)
            {
                if (saved == null || string.IsNullOrEmpty(saved.id) ||
                    !variables.TryGetValue(saved.id, out var variable) ||
                    saved.type != variable.Type.FullName || !TryParse(saved, out var value) ||
                    !restoredIds.Add(saved.id))
                {
                    skipped++;
                    continue;
                }

                variable.Value = value;
            }

            elementId = FindElement(project, snapshot.currentElement)?.Id;
            // Normalize obsolete entries once, so subsequent starts do not retry them.
            Save(project, elementId);
            if (legacy)
            {
                PlayerPrefs.DeleteKey(key + "_variables");
                PlayerPrefs.DeleteKey(key + "_currentElement");
                PlayerPrefs.Save();
            }

            if (skipped > 0)
                Debug.Log($"Restored Arcweave progress; skipped {skipped} missing or incompatible variable(s).");
            return true;
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(key + "_progress");
            PlayerPrefs.DeleteKey(key + "_variables");
            PlayerPrefs.DeleteKey(key + "_currentElement");
            PlayerPrefs.Save();
        }

        private static Element FindElement(Project.Project project, string id)
        {
            return string.IsNullOrEmpty(id) ? null : project.ElementWithId(id);
        }

        private static bool TryParse(VariableValue saved, out object value)
        {
            value = null;
            if (saved.type == typeof(string).FullName && saved.value != null)
                value = saved.value;
            else if (saved.type == typeof(int).FullName && int.TryParse(saved.value,
                         NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
                value = integer;
            else if (saved.type == typeof(double).FullName && double.TryParse(saved.value,
                         NumberStyles.Float, CultureInfo.InvariantCulture, out double number) &&
                     !double.IsNaN(number) && !double.IsInfinity(number))
                value = number;
            else if (saved.type == typeof(bool).FullName && bool.TryParse(saved.value, out bool boolean))
                value = boolean;
            return value != null;
        }
    }
}
