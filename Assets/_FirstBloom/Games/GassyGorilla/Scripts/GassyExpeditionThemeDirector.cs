using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class GassyExpeditionThemeDirector : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Light keyLight;
        [SerializeField] private Light fillLight;
        [SerializeField] private Renderer[] backdropRenderers;

        [Header("Moonlit Ruins")]
        [SerializeField] private Color moonCameraColor =
            new Color(0.025f, 0.055f, 0.12f, 1f);
        [SerializeField] private Color moonBackdropTint =
            new Color(0.18f, 0.33f, 0.62f, 1f);
        [SerializeField] private Color moonAmbientSky =
            new Color(0.16f, 0.27f, 0.5f, 1f);
        [SerializeField] private Color moonAmbientEquator =
            new Color(0.07f, 0.16f, 0.3f, 1f);
        [SerializeField] private Color moonAmbientGround =
            new Color(0.025f, 0.06f, 0.1f, 1f);
        [SerializeField] private Color moonKeyColor =
            new Color(0.62f, 0.74f, 1f, 1f);
        [SerializeField] private Color moonFillColor =
            new Color(0.55f, 0.34f, 0.9f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private Color baseCameraColor;
        private Color baseAmbientSky;
        private Color baseAmbientEquator;
        private Color baseAmbientGround;
        private Color baseKeyColor;
        private Color baseFillColor;
        private Color[] baseBackdropColors;

        public GassyExpeditionTheme ActiveTheme { get; private set; }
        public bool IsConfigured
        {
            get
            {
                return sceneCamera != null &&
                    keyLight != null &&
                    fillLight != null &&
                    backdropRenderers != null &&
                    backdropRenderers.Length >= 3;
            }
        }

        private void Awake()
        {
            CacheDefaults();
        }

        public void Apply(GassyExpeditionTheme theme)
        {
            if (!IsConfigured)
            {
                return;
            }

            if (baseBackdropColors == null ||
                baseBackdropColors.Length != backdropRenderers.Length)
            {
                CacheDefaults();
            }

            ActiveTheme = theme;
            bool moonlit =
                theme == GassyExpeditionTheme.MoonlitRuins;
            sceneCamera.backgroundColor =
                moonlit ? moonCameraColor : baseCameraColor;
            RenderSettings.ambientSkyColor =
                moonlit ? moonAmbientSky : baseAmbientSky;
            RenderSettings.ambientEquatorColor =
                moonlit ? moonAmbientEquator : baseAmbientEquator;
            RenderSettings.ambientGroundColor =
                moonlit ? moonAmbientGround : baseAmbientGround;
            keyLight.color = moonlit ? moonKeyColor : baseKeyColor;
            fillLight.color = moonlit ? moonFillColor : baseFillColor;

            for (int i = 0; i < backdropRenderers.Length; i++)
            {
                Renderer backdrop = backdropRenderers[i];
                if (backdrop == null)
                {
                    continue;
                }

                Color color = moonlit
                    ? Color.Lerp(
                        baseBackdropColors[i],
                        moonBackdropTint,
                        0.7f)
                    : baseBackdropColors[i];
                color.a = baseBackdropColors[i].a;
                backdrop.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", color);
                propertyBlock.SetColor("_BaseColor", color);
                backdrop.SetPropertyBlock(propertyBlock);
            }
        }

        private void CacheDefaults()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (sceneCamera != null)
            {
                baseCameraColor = sceneCamera.backgroundColor;
            }

            baseAmbientSky = RenderSettings.ambientSkyColor;
            baseAmbientEquator = RenderSettings.ambientEquatorColor;
            baseAmbientGround = RenderSettings.ambientGroundColor;
            baseKeyColor =
                keyLight != null ? keyLight.color : Color.white;
            baseFillColor =
                fillLight != null ? fillLight.color : Color.white;

            if (backdropRenderers == null)
            {
                baseBackdropColors = null;
                return;
            }

            baseBackdropColors = new Color[backdropRenderers.Length];
            for (int i = 0; i < backdropRenderers.Length; i++)
            {
                Renderer backdrop = backdropRenderers[i];
                Material material =
                    backdrop != null ? backdrop.sharedMaterial : null;
                baseBackdropColors[i] =
                    material != null && material.HasProperty("_Color")
                        ? material.color
                        : Color.white;
            }
        }
    }
}
