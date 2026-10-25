using Nekuzaky.HorrorAI.Editor;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Dev
{
    internal static class PresetBuilder
    {
        #region Main API

        private const string Folder = SceneFixes.PackagePath + "/Runtime/Presets";

        [MenuItem("Tools/Horror AI Dev/Build Presets")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder(SceneFixes.PackagePath + "/Runtime", "Presets");
            var perception = Load<PerceptionProfile>(Folder + "/Patroller Perception.asset");
            var behavior = Load<BehaviorProfile>(Folder + "/Patroller Behavior.asset");
            var archetype = Load<HorrorArchetype>(SceneFixes.PatrollerPath);
            archetype.description = "Walks a patrol route at a steady pace. Notices a lit player quickly and a player in the dark slowly, " +
                                    "goes to check noises, searches the area for a while, then gives up and resumes its round. " +
                                    "The all-round enemy to start with.";
            archetype.perception = perception;
            archetype.behavior = behavior;
            EditorUtility.SetDirty(archetype);
            EditorUtility.SetDirty(perception);
            EditorUtility.SetDirty(behavior);
            AssetDatabase.SaveAssets();
            Debug.Log("[HorrorAI] Presets written to " + Folder);
        }

        #endregion


        #region Tools and Utilities

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        #endregion
    }
}
