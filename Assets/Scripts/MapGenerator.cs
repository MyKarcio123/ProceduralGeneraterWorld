using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        ColorMap,
        Mesh,
        FalloffMap
    };
    public DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

    public const int chunkSize = 239;
    [Range(0, 6)]
    public int editorPrevievLOD;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool useFalloff;

    public float heightMultiplayer;
    public AnimationCurve heightCurve;

    public TerrainType[] regions;

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
    public bool autoUpdate;

    private void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(chunkSize);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = gameObject.GetComponent<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap) display.DrawTexture(TextureGeneration.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.ColorMap) display.DrawTexture(TextureGeneration.TextureFromColorMap(mapData.colorMap, chunkSize, chunkSize));
        else if (drawMode == DrawMode.Mesh) display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplayer, heightCurve, editorPrevievLOD), TextureGeneration.TextureFromColorMap(mapData.colorMap, chunkSize, chunkSize));
        else if (drawMode == DrawMode.FalloffMap) display.DrawTexture(TextureGeneration.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(chunkSize)));
    }
    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre,callback);
        };

        new Thread(threadStart).Start();
    }
    void MapDataThread(Vector2 centre,Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }
    public void RequestMeshData(MapData mapData, int lod,Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData,lod,callback);
        };

        new Thread(threadStart).Start();
    }
    void MeshDataThread(MapData mapData, int lod,Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplayer, heightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }
    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; ++i)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }        
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < meshDataThreadInfoQueue.Count; ++i)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    private MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GetNoiseMap(chunkSize + 2, chunkSize + 2,seed, noiseScale,octaves,persistance,lacunarity,centre+offset, normalizeMode);

        Color[] colors = new Color[chunkSize * chunkSize];
        for(int i = 0; i < chunkSize; ++i)
        {
            for(int j = 0; j < chunkSize; ++j)
            {
                if (useFalloff)
                {
                    noiseMap[j, i] = Mathf.Clamp01(noiseMap[j, i] - falloffMap[j, i]);
                }
                float currentchunkSize = noiseMap[j, i];
                for(int k = 0; k < regions.Length; k++)
                {
                    if (currentchunkSize >= regions[k].chunkSize)
                    {
                        colors[i * chunkSize + j] = regions[k].colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colors);
    }

    void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;

        falloffMap = FalloffGenerator.GenerateFalloffMap(chunkSize);
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback,T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float chunkSize;
    public Color colour;
}
public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}

