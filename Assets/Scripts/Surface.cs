using UnityEngine;
using Unity.Mathematics;

public class Surface : MonoBehaviour
{
    public int gridSize = 4, iteration = 16;
    public float dt = 1f / 30, damp = 0.95f;
    public Material material;

    float3[] points; // x: height, y: old_height z: velocity
    int[] constrains; // bitmask
    float4[] datas; // x: height, yzw: normal
    ComputeBuffer dataBuffer;

    void Start()
    {
        points = new float3[gridSize * gridSize];
        constrains = new int[gridSize * gridSize];
        datas = new float4[points.Length];
        dataBuffer = new ComputeBuffer(points.Length, 4 * sizeof(float));
        material.SetInt("_GridSize", gridSize);

        for (int i = 0; i < constrains.Length; i++)
        {
            int x = i / gridSize, y = i % gridSize;

            // Build Constrains
            if (x < gridSize - 1)
            {
                constrains[x * gridSize + y] = (constrains[x * gridSize + y] << 3) + 1; 
                constrains[(x + 1) * gridSize + y] = (constrains[(x + 1) * gridSize + y] << 3) + 2;
            }
            if (y < gridSize - 1)
            {
                constrains[x * gridSize + y] = (constrains[x * gridSize + y] << 3) + 3;
                constrains[x * gridSize + y + 1] = (constrains[x * gridSize + y + 1] << 3) + 4;
            }
        }
    }

    void Update()
    {
        // PBD Iteration
        for (int i = 0; i < points.Length; i++)
        {
            points[i].z *= damp;
            points[i].x += dt * points[i].z;
            points[i].x += UnityEngine.Random.Range(-0.01f, 0.01f);
            points[i].x = math.clamp(points[i].x, 0, 1);
        }
        for (int i = 0; i < iteration; i++) 
        {
            for (int j = 0; j < constrains.Length; j++) 
            {
                int num = constrains[j], cnt = 0; 
                float delta = 0;
                while (num > 0)
                {
                    switch(num & 7)
                    {
                        case 1:
                            delta += 0.5f * (points[j + gridSize].x - points[j].x);
                            cnt++; break;
                        case 2:
                            delta += 0.5f * (points[j - gridSize].x - points[j].x);
                            cnt++; break;
                        case 3:
                            delta += 0.5f * (points[j + 1].x - points[j].x);
                            cnt++; break;
                        case 4:
                            delta += 0.5f * (points[j - 1].x - points[j].x);
                            cnt++; break;
                    }
                    num >>= 3;
                }
            }
        }
        for (int i = 0; i < points.Length; i++)
        {
            points[i].z = (points[i].x - points[i].y) / dt;
            points[i].y = points[i].x;
        }

        // Calculate Normal
        for (int i = 0; i < points.Length; i++)
        {
            int x = i / gridSize, y = i % gridSize,
                xl = math.max(x - 1, 0), xr = math.min(x + 1, gridSize - 1),
                yl = math.max(y - 1, 0), yr = math.min(y + 1, gridSize - 1);

            float dx = (points[y * gridSize + xr].x - points[y * gridSize + xl].x) / (xr - xl),
                dz = (points[yr * gridSize + x].x - points[yl * gridSize + x].x) / (yr - yl);

            datas[i] = new float4(points[i].x, math.normalize(new float3(dx, 1.0f, dz)));
        }

        dataBuffer.SetData(datas);
        material.SetBuffer("_DataBuffer", dataBuffer);
    }

    void OnDrawGizmos()
    {
        if (points == null) Start();
        for (int i = 0; i < points.Length; i++)
        {
            int x = i / gridSize, y = i % gridSize;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + new Vector3(0.5f - (x + 0.5f) / (gridSize + 1), 
                points[i].x, 0.5f - (y + 0.5f) / (gridSize + 1)), 1f / (2 * gridSize + 2));
        }
    }
}
