using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekuzaky.HorrorAI.Demo
{
    [DefaultExecutionOrder(-100)]
    public sealed class DemoPipelineMaterials : MonoBehaviour
    {
        #region Public

        public static readonly string[] LitShaders = { "Universal Render Pipeline/Lit", "HDRP/Lit", "Universal Render Pipeline/Simple Lit" };

        public int Apply()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null)
                return 0;
            var shader = LitShader(pipeline);
            if (shader == null)
            {
                Debug.LogWarning("[Horror AI demo] No lit shader of the active render pipeline was found: the demo materials stay as they are.");
                return 0;
            }
            var replaced = 0;
            var replacements = new Dictionary<Material, Material>();
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                replaced += Replace(renderer, shader, replacements);
            return replaced;
        }

        public static Shader LitShader(RenderPipelineAsset pipeline)
        {
            var material = pipeline.defaultMaterial;
            if (material != null && material.shader != null && material.shader.isSupported)
                return material.shader;
            if (pipeline.defaultShader != null && pipeline.defaultShader.isSupported)
                return pipeline.defaultShader;
            for (var i = 0; i < LitShaders.Length; i++)
            {
                var shader = Shader.Find(LitShaders[i]);
                if (shader != null && shader.isSupported)
                    return shader;
            }
            return null;
        }

        #endregion


        #region Private and Protected

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColor = Shader.PropertyToID("_Color");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");

        private void Awake() => Apply();

        private static int Replace(Renderer renderer, Shader shader, Dictionary<Material, Material> replacements)
        {
            var materials = renderer.sharedMaterials;
            var changed = 0;
            for (var i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (source == null || source.shader == shader || !NeedsReplacing(source.shader))
                    continue;
                if (!replacements.TryGetValue(source, out var replacement))
                {
                    replacement = Convert(source, shader);
                    replacements.Add(source, replacement);
                }
                materials[i] = replacement;
                changed++;
            }
            if (changed > 0)
                renderer.sharedMaterials = materials;
            return changed;
        }

        private static bool NeedsReplacing(Shader shader) => shader == null || !shader.isSupported || shader.name == "Standard";

        private static Material Convert(Material source, Shader shader)
        {
            var color = source.HasProperty(LegacyColor) ? source.GetColor(LegacyColor) : Color.white;
            var material = new Material(shader) { name = source.name + " (" + shader.name + ")", hideFlags = HideFlags.DontSave };
            if (material.HasProperty(BaseColor))
                material.SetColor(BaseColor, color);
            if (material.HasProperty(LegacyColor))
                material.SetColor(LegacyColor, color);
            if (material.HasProperty(Smoothness))
                material.SetFloat(Smoothness, 0.15f);
            return material;
        }

        #endregion
    }
}
