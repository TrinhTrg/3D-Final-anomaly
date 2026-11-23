using UnityEngine;
using UnityEngine.UI;

public sealed class AAH_PolaroidNoise : MonoBehaviour
{
    private RawImage overlay;
    private Texture2D noise;

    public static void SetFor(Graphic photo, bool isAnomaly)
    {
        if (photo == null) return;

        var effect = photo.GetComponent<AAH_PolaroidNoise>();
        if (effect == null)
        {
            if (!isAnomaly) return;
            effect = photo.gameObject.AddComponent<AAH_PolaroidNoise>();
        }

        if (isAnomaly)
            effect.ShowNoise();
        else if (effect.overlay != null)
            effect.overlay.enabled = false;
    }

    private void ShowNoise()
    {
        if (overlay == null)
        {
            var layer = new GameObject("Nhiễu ảnh", typeof(RectTransform));
            layer.transform.SetParent(transform, false);

            overlay = layer.AddComponent<RawImage>();
            overlay.raycastTarget = false;

            var rect = overlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            noise = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            noise.filterMode = FilterMode.Point;
            overlay.texture = noise;
        }

        // Mỗi lần chụp có một mẫu nhiễu riêng.
        var random = new System.Random();
        var pixels = new Color32[noise.width * noise.height];

        for (int y = 0; y < noise.height; y++)
        {
            bool darkBand = random.Next(24) == 0;

            for (int x = 0; x < noise.width; x++)
            {
                bool brightGrain = random.Next(6) == 0;
                pixels[y * noise.width + x] = darkBand
                    ? new Color32(20, 8, 20, 145)
                    : brightGrain
                        ? new Color32(215, 204, 210, 75)
                        : new Color32(18, 12, 24, (byte)random.Next(20, 75));
            }
        }

        noise.SetPixels32(pixels);
        noise.Apply(false);
        overlay.enabled = true;
    }

    private void OnDestroy()
    {
        if (noise != null) Destroy(noise);
    }
}