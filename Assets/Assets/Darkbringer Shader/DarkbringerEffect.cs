using UnityEngine;
using System.Collections;

namespace Picturesque.Darkbringer
{
    [ExecuteInEditMode]
    public class DarkbringerEffect : MonoBehaviour
    {
        public Shader sha;
        public Material mat;
        public Texture2D flatLut;
        public Texture2D bayerTexture;

        public float ColorBleed = 0.25f;
        public float LinesMult = 0.8f;
        public float ColorShift = 0.29f;
        public Vector2 newScreenSize = new Vector2(160, 200);
        public Vector2 screenStretching = new Vector2(2, 1);

        public bool CustomScreenSize = true;
        public bool AspectRatioBars = true;
        public bool OneBitColor = false;
        public bool VerticalLines = false;
        public bool HorizontalLines = false;
        public bool Dithering = true;
        public bool Paletting = true;

        public float ShadowLift = 0.1f;      // New: softens shadows
        public float Saturation = 0.9f;      // New: reduces oversaturation

        private Texture3D vTex;
        private Texture2D v2DTex;
        public bool clearNextUpdate;

        void Awake()
        {
            if (sha != null)
            {
                mat = new Material(sha);
                mat.SetTexture("_BayerTex", bayerTexture);
            }
        }

        void OnDisable()
        {
            if (mat)
            {
                DestroyImmediate(mat);
                mat = null;
            }
        }

        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (sha == null || flatLut == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (mat == null)
            {
                mat = new Material(sha);
            }

            if (clearNextUpdate || v2DTex == null || mat.GetTexture("_Lut2D") == null ||
                ((mat.GetTexture("_Lut2D") != null) && flatLut.name != mat.GetTexture("_Lut2D").name))
            {
                if (v2DTex != null || clearNextUpdate)
                {
                    DestroyImmediate(v2DTex);
                    v2DTex = null;
                }

                int dim = flatLut.height;
                v2DTex = new Texture2D(dim * dim, dim * dim, TextureFormat.ARGB32, false);
                v2DTex.filterMode = FilterMode.Point;
                v2DTex.name = flatLut.name;

                Color[] c = flatLut.GetPixels();
                Color[] newC = new Color[dim * dim * dim * dim];

                for (int i = 0; i < dim; i++)
                {
                    for (int j = 0; j < dim; j++)
                    {
                        for (int x = 0; x < dim; x++)
                        {
                            for (int y = 0; y < dim; y++)
                            {
                                float b = (i + j * dim * 1.0f) / dim;
                                int bi0 = Mathf.FloorToInt(b);
                                int bi1 = Mathf.Min(bi0 + 1, dim - 1);
                                float f = b - bi0;

                                int index = x + (dim - y - 1) * dim * dim;
                                Color col1 = c[index + bi0 * dim];
                                Color col2 = c[index + bi1 * dim];

                                newC[x + i * dim + y * dim * dim + j * dim * dim * dim] = (f < 0.5f) ? col1 : col2;
                            }
                        }
                    }
                }

                v2DTex.wrapMode = TextureWrapMode.Clamp;
                v2DTex.SetPixels(newC);
                v2DTex.Apply();
                mat.SetTexture("_Lut2D", v2DTex);
            }

            float lutSquare = 16;

            mat.SetFloat("_ScaleRG", 0.05859375f);
            mat.SetFloat("_Dim", lutSquare);
            mat.SetFloat("_Offset", 0.001953125f);

            mat.SetTexture("_Lut2d", v2DTex);
            mat.SetFloat("_HORLINES", HorizontalLines ? 1 : 0);
            mat.SetFloat("_VERTLINES", VerticalLines ? 1 : 0);
            mat.SetFloat("_BIT1", OneBitColor ? 1 : 0);
            mat.SetFloat("_DITHER", Dithering ? 1 : 0);
            mat.SetFloat("_PALETTE", Paletting ? 1 : 0);
            mat.SetFloat("_ARC", AspectRatioBars ? 1 : 0);
            mat.SetFloat("_CSIZE", CustomScreenSize ? 1 : 0);
            mat.SetFloat("_LinesMult", LinesMult);
            mat.SetFloat("_ColorBleed", ColorBleed);
            mat.SetFloat("_ColorShift", ColorShift);
            mat.SetVector("_ScreenSize", new Vector4(newScreenSize.x, newScreenSize.y, 0, 0));
            mat.SetVector("_Stretching", new Vector4(screenStretching.x, screenStretching.y, 0, 0));

            // 🔥 New controls for shadows and saturation
            mat.SetFloat("_ShadowLift", ShadowLift);
            mat.SetFloat("_Saturation", Saturation);

            Graphics.Blit(source, destination, mat);
        }
    }
}
