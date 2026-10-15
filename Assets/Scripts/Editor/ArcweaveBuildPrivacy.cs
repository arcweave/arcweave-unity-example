using System.Collections.Generic;
using Arcweave;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Removes Editor web credentials from build scenes without changing source assets.</summary>
public sealed class ArcweaveBuildPrivacy : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (!BuildPipeline.isBuildingPlayer) return;

        var projects = new Dictionary<ArcweaveProjectAsset, ArcweaveProjectAsset>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var importer in root.GetComponentsInChildren<RuntimeArcweaveImporter>(true))
            {
                importer.apiKey = "";
                importer.projectHash = "";
            }
            foreach (var ui in root.GetComponentsInChildren<ArcweaveImporterUI>(true))
            {
                if (ui.apiKeyInput != null) ui.apiKeyInput.SetTextWithoutNotify("");
                if (ui.hashInput != null) ui.hashInput.SetTextWithoutNotify("");
            }

            // Replace every scene reference with a sanitized build-only copy of the project.
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        !(property.objectReferenceValue is ArcweaveProjectAsset original)) continue;

                    if (!projects.TryGetValue(original, out var clean))
                    {
                        clean = Object.Instantiate(original);
                        clean.name = original.name;
                        clean.userAPIKey = "";
                        clean.projectHash = "";
                        projects.Add(original, clean);
                        projects.Add(clean, clean);
                    }
                    property.objectReferenceValue = clean;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
