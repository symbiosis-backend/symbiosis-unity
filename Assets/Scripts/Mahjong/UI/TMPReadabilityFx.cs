using TMPro;
using UnityEngine;

namespace MahjongGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TMPReadabilityFx : MonoBehaviour
    {
        [SerializeField] private TMP_Text textTarget;

        [Header("Face")]
        [SerializeField] private Color faceColor = Color.white;

        [Header("Outline")]
        [SerializeField] private bool enableOutline = true;
        [SerializeField] private Color outlineColor = Color.black;
        [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.25f;

        [Header("Shadow / Underlay")]
        [SerializeField] private bool enableUnderlay = true;
        [SerializeField] private Color underlayColor = new Color(0f, 0f, 0f, 0.8f);
        [SerializeField, Range(-1f, 1f)] private float offsetX = 0.05f;
        [SerializeField, Range(-1f, 1f)] private float offsetY = -0.05f;
        [SerializeField, Range(0f, 1f)] private float dilate = 0.1f;
        [SerializeField, Range(0f, 1f)] private float softness = 0.2f;

        private void Awake()
        {
            if (textTarget == null)
                textTarget = GetComponent<TMP_Text>();

            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        [ContextMenu("Apply FX")]
        public void Apply()
        {
            if (textTarget == null)
                return;

            textTarget.color = faceColor;

            Material runtimeMat = textTarget.fontMaterial;
            if (runtimeMat == null)
                return;

            if (runtimeMat.HasProperty(ShaderUtilities.ID_FaceColor))
                runtimeMat.SetColor(ShaderUtilities.ID_FaceColor, faceColor);

            if (enableOutline)
            {
                if (runtimeMat.HasProperty(ShaderUtilities.ID_OutlineColor))
                    runtimeMat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);

                if (runtimeMat.HasProperty(ShaderUtilities.ID_OutlineWidth))
                    runtimeMat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            }
            else
            {
                if (runtimeMat.HasProperty(ShaderUtilities.ID_OutlineWidth))
                    runtimeMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            }

            if (enableUnderlay)
            {
                runtimeMat.EnableKeyword("UNDERLAY_ON");

                if (runtimeMat.HasProperty(ShaderUtilities.ID_UnderlayColor))
                    runtimeMat.SetColor(ShaderUtilities.ID_UnderlayColor, underlayColor);

                if (runtimeMat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
                    runtimeMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, offsetX);

                if (runtimeMat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
                    runtimeMat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, offsetY);

                if (runtimeMat.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                    runtimeMat.SetFloat(ShaderUtilities.ID_UnderlayDilate, dilate);

                if (runtimeMat.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                    runtimeMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
            }
            else
            {
                runtimeMat.DisableKeyword("UNDERLAY_ON");

                if (runtimeMat.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
                    runtimeMat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);

                if (runtimeMat.HasProperty(ShaderUtilities.ID_UnderlayDilate))
                    runtimeMat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            }

            textTarget.UpdateMeshPadding();
            textTarget.ForceMeshUpdate();
        }
    }
}