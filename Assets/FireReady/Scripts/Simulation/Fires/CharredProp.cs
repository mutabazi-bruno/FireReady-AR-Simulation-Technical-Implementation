using System.Collections.Generic;
using UnityEngine;

namespace FireReady.Simulation
{
    // Darkens a prop towards charred black the longer the nearby fire burns.
    public class CharredProp : FireLinkedEffect
    {
        [SerializeField] Color charColor = new Color(0.07f, 0.06f, 0.055f);
        [SerializeField, Range(0f, 1f)] float maxChar = 0.85f;

        // URP Lit and the glTF shaders name their base colour differently.
        static readonly int[] ColorProperties = { Shader.PropertyToID("_BaseColor"), Shader.PropertyToID("baseColorFactor") };

        readonly List<(Renderer renderer, int index, int property, Color original)> slots = new List<(Renderer, int, int, Color)>();
        MaterialPropertyBlock block;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    foreach (var property in ColorProperties)
                    {
                        if (!materials[i].HasProperty(property)) continue;
                        slots.Add((r, i, property, materials[i].GetColor(property)));
                        break;
                    }
                }
            }
        }

        protected override void Apply(float amount)
        {
            float t = amount * maxChar;
            foreach (var slot in slots)
            {
                slot.renderer.GetPropertyBlock(block, slot.index);
                var c = Color.Lerp(slot.original, charColor, t);
                c.a = slot.original.a;
                block.SetColor(slot.property, c);
                slot.renderer.SetPropertyBlock(block, slot.index);
            }
        }
    }
}
