using UnityEngine;
using Unity.Mathematics;

public class Surface : MonoBehaviour
{
    public int gridSize = 4;
    public Texture2D heightMap;

    int mapSize;
    float2[] points;// x: height, y: velocity
    float4[] pixels;// xyz: normal, w: height

    void Start()
    {
        mapSize = Mathf.Min(heightMap.width, heightMap.height);
        points = new float2[gridSize * gridSize];
        pixels = new float4[mapSize * mapSize];

        for (int i = 0; i < gridSize * gridSize; i++)
        {
            int u = i / gridSize, v = i % gridSize;
            points[i] = new float2((u + 1) / gridSize, (v + 1) / gridSize);
        }
    }

    void Update()
    {
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int u = i / mapSize, v = i % mapSize;
            float x = (u + 1) / mapSize, y = (v + 1) / mapSize;
            int ul = (int)(x / gridSize), ur = Mathf.Min(ul + 1, gridSize - 1),
                vl = (int)(y / gridSize), vr = Mathf.Min(vl + 1, gridSize - 1);
            float uf = mapSize * (x - ul - 1) / gridSize, vf = mapSize * (y - vl - 1) / gridSize;
            pixels[i].w = 0.5f * (math.smoothstep(points[ul].x, points[ur].x, uf) +
                math.smoothstep(points[vl].x, points[vr].x, vf));
        }

        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int u = i / mapSize, v = i % mapSize,
                uDown = Mathf.Max(u - 1, 0), uUp = Mathf.Min(u + 1, mapSize - 1),
                vLeft = Mathf.Max(v - 1, 0), vRight = Mathf.Min(v + 1, mapSize - 1),
                idxDown = uDown * mapSize + v, idxUp = uUp * mapSize + v,
                idxLeft = u * mapSize + vLeft, idxRight = u * mapSize + vRight;
            
            float dX = 0.5f * (pixels[idxRight].w - pixels[idxLeft].w), 
                dZ = 0.5f * (pixels[idxUp].w - pixels[idxDown].w);

            pixels[i].xyz = math.normalize(new float3(-dX, 1.0f, -dZ));
        }
    }
}
