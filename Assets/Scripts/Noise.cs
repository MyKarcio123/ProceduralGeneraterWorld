using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode
    {
        Local,
        Global
    };

    public static float[,] GetNoiseMap(int width,int height,int seed,float scale,int octaves, float persistance, float lacunarity,Vector2 offset,NormalizeMode normalizeMode)
    {
        float [,] noiseMap = new float[width, height];

        System.Random prng = new System.Random(seed);
        Vector2[] octavesOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;

        for (int i=0; i < octaves; ++i)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octavesOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }
        if (scale <= 0) scale = 0.001f;

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;
        for(int i = 0; i < height; ++i)
        {
            for(int j = 0; j < width; ++j)
            {
                amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                for (int k = 0; k < octaves; ++k)
                {
                    float sampleI = (i-halfHeight + octavesOffsets[k].y) / scale * frequency;
                    float sampleJ = (j-halfWidth + octavesOffsets[k].x) / scale * frequency;
                    
                    float perlinValue = Mathf.PerlinNoise(sampleJ, sampleI) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                else if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                noiseMap[j, i] = noiseHeight;
            }
        }
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[j, i] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[j, i]);
                }
                else
                {
                    float normalizeHeight = (noiseMap[j, i] + 1) / (maxPossibleHeight/0.9f);
                    noiseMap[j, i] = Mathf.Clamp(normalizeHeight,0,int.MaxValue);
                }
            }
        }
        return noiseMap;
    }

}
