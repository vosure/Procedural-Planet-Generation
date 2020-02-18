using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorGenerator
{
    ColorSettings settings;
    Texture2D texture;
    const int textureResolution = 50;
    INoiseFilter biomenoiseFilter;


    public void UpdateSettings(ColorSettings settings)
    {
        this.settings = settings;
        if (texture == null || texture.height != settings.biomeColorSettings.bioms.Length)
        {
            texture = new Texture2D(textureResolution * 2, settings.biomeColorSettings.bioms.Length, TextureFormat.RGBA32, false);
        }
        biomenoiseFilter = NoiseFilterFactory.CreateNoiseFilter(settings.biomeColorSettings.noise);
    }

    public void UpdateElevation(MinMax elevationMinMax)
    {
        settings.planetMaterial.SetVector("_elevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
    }

    public float BiomePercentFromPoint(Vector3 pointOnUnitSphere)
    {
        float heightPercent = (pointOnUnitSphere.y + 1) / 2.0f;
        heightPercent += (biomenoiseFilter.Evaluate(pointOnUnitSphere) - settings.biomeColorSettings.noiseOffset) * settings.biomeColorSettings.noiseStrength;
        float biomeIndex = 0;
        int numBiomes = settings.biomeColorSettings.bioms.Length;
        float blendRange = settings.biomeColorSettings.blendAmount / 2.0f + 0.001f;

        for (int i = 0; i < numBiomes; i++)
        {
            float dst = heightPercent - settings.biomeColorSettings.bioms[i].startHeight;
            float weigth = Mathf.InverseLerp(-blendRange, blendRange, dst);
            biomeIndex *= (1 - weigth);
            biomeIndex += i * weigth;
        }

        return biomeIndex / Mathf.Max(1, numBiomes - 1);
    }

    public void UpdateColors()
    {
        Color[] colors = new Color[texture.width * texture.height];
        int colorIndex = 0;
        foreach (var biome in settings.biomeColorSettings.bioms)
        {
            for (int i = 0; i < textureResolution * 2; i++)
            {
                Color gradientColor;
                if (i < textureResolution)
                {
                    gradientColor = settings.oceanColor.Evaluate(i / (textureResolution - 1.0f));
                }
                else
                {
                    gradientColor = biome.gradient.Evaluate((i - textureResolution) / (textureResolution - 1.0f));
                }      
                Color tintColor = biome.tint;
                colors[colorIndex] = gradientColor * (1 - biome.tintPercent) + tintColor * biome.tintPercent;
                colorIndex++;
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        settings.planetMaterial.SetTexture("_texture", texture);
    }
}
