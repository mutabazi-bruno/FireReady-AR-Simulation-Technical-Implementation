using UnityEngine;

namespace FireReady.Simulation
{
    // Scorch mark on the floor or soot on a wall that fades in and spreads as the fire burns.
    public class BurnDecal : FireLinkedEffect
    {
        [SerializeField] Renderer decal;
        [SerializeField] float startScale = 0.45f;
        [SerializeField] float maxAlpha = 0.95f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;
        Vector3 fullScale;

        void Awake()
        {
            if (decal == null) decal = GetComponentInChildren<Renderer>();
            fullScale = decal.transform.localScale;
            block = new MaterialPropertyBlock();
            Apply(0f);
        }

        protected override void Apply(float amount)
        {
            decal.enabled = amount > 0.01f;
            decal.transform.localScale = fullScale * Mathf.Lerp(startScale, 1f, amount);
            decal.GetPropertyBlock(block);
            block.SetColor(BaseColor, new Color(1f, 1f, 1f, maxAlpha * Mathf.SmoothStep(0f, 1f, amount)));
            decal.SetPropertyBlock(block);
        }
    }
}
