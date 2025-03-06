using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;


public class Wine: MonoBehaviour
{
    [Header("Related Objects")]
    public Material material;
    public Transform iceSpawner;
    public GameObject ice, spoon, grass;
    [Header("Mass-Spring Settings")]
    public int gridSize = 4;
    public int iteration = 8;
    public float dt = 1f / 30, damp = 0.95f;
    [HideInInspector] public int iceNum = 4, layerCnt;
    [HideInInspector] public Color color = new Color(0.45f, 0.8f, 1, 0.9f);
    [HideInInspector] public Vector4[] colors = new Vector4[3];

    // Ices
    Rigidbody[] ices;
    float wineDamp = 0.9f;
    // Mass-Spring
    Vector3[] points; // x: height, y: old_height z: velocity
    int[] constrains; // bitmask
    Vector4[] datas; // x: height, yzw: normal
    float height = -0.3f;
    ComputeBuffer dataBuffer;

    void Start()
    {
        points = new Vector3[gridSize * gridSize];
        constrains = new int[gridSize * gridSize];
        datas = new Vector4[points.Length];
        dataBuffer = new ComputeBuffer(points.Length, 4 * sizeof(float));
        material.SetInt("_LayerCnt", layerCnt);
        material.SetInt("_GridSize", gridSize);
        material.SetFloat("_Height", height);
        BuildConstrains();
    }

    void FixedUpdate()
    {
        UpdateIce();
        UpdateMassSpring();
    }

    // Mass-Spring System
    void OnDrawGizmos()
    {
        if (points == null) Start();
        for (int i = 0; i < points.Length; i++)
        {
            int x = i / gridSize, y = i % gridSize;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(x / (gridSize - 1f), 
                points[i].x, y / (gridSize - 1f)), 1f / (2 * gridSize - 2));
        }
    }

    void BuildConstrains()
    {
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

    void HandleCollision(Vector3 pos, float radius)
    {
        for (int i = 0; i < points.Length; i++)
        {
            int x = i / gridSize, y = i % gridSize;
            Vector3 pointPos = new Vector3(x / (gridSize - 1f),
                points[i].x, y / (gridSize - 1f));
            float d = (pos - pointPos).magnitude, dx = radius + 1f / (2 * gridSize - 2);
            if (d < dx) continue;
            points[i].x = -Mathf.Sqrt(d * d - dx * dx);
            points[i].z = (points[i].x - points[i].y) / dt;
        }
    }

    void UpdateMassSpring(bool updateVel = true)
    {
        // PBD Iteration
        float delta;
        if (updateVel) 
            for (int i = 0; i < points.Length; i++)
            {
                points[i].z *= damp;
                points[i].x += dt * points[i].z;
            }
        for (int i = 0; i < iteration; i++)
        {
            for (int j = 0; j < constrains.Length; j++)
            {
                int num = constrains[j], cnt = 0; delta = 0;
                while (num > 0)
                {
                    switch (num & 7)
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
                points[j].x += delta / cnt;
            }
        }
        // Volumn Conservation
        delta = 0;
        for (int i = 0; i < points.Length; i++) 
            delta += points[i].x; delta /= points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            points[i].x -= delta;
            points[i].z = (points[i].x - points[i].y) / dt;
            points[i].y = points[i].x;
        }

        // Calculate Normal
        for (int i = 0; i < points.Length; i++)
        {
            int x = i / gridSize, y = i % gridSize,
                xl = Mathf.Max(x - 1, 0), xr = Mathf.Min(x + 1, gridSize - 1),
                yl = Mathf.Max(y - 1, 0), yr = Mathf.Min(y + 1, gridSize - 1);

            float dx = (points[y * gridSize + xr].x - points[y * gridSize + xl].x) / (xr - xl),
                dz = (points[yr * gridSize + x].x - points[yl * gridSize + x].x) / (yr - yl);

            Vector3 normal = new Vector3(dx, 1.0f, dz).normalized;
            datas[i] = new Vector4(points[i].x, normal.x, normal.y, normal.z);
        }

        dataBuffer.SetData(datas);
        material.SetBuffer("_DataBuffer", dataBuffer);
    }

    // Ice Controller
    public void ResetIce()
    {
        if (ices != null) for (int i = 0; i < ices.Length; i++) Destroy(ices[i].gameObject);
        ices = new Rigidbody[iceNum];
        for (int i = 0; i < iceNum; i++)
        {
            ices[i] = Instantiate(ice, iceSpawner.position + new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(0f, 0.1f),
                Random.Range(-0.05f, 0.05f)), Quaternion.identity, iceSpawner).GetComponent<Rigidbody>();
            ices[i].gameObject.SetActive(false);
        }
        StartCoroutine(ActiveIce());
    }

    IEnumerator ActiveIce()
    {
        for (int i = 0; i < iceNum; i++)
        {
            ices[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(0.2f);
        }
    }

    void UpdateIce()
    {
        if (ices == null) return;
        for (int i = 0; i < ices.Length; i++)
        {
            if (ices[i].gameObject.activeSelf == false) continue;
            ices[i].linearVelocity *= damp; ices[i].angularVelocity *= damp;
            Vector3 pos = ices[i].transform.position;
            if (pos.y - 0.05f < height)
            {
                ices[i].linearVelocity *= wineDamp; ices[i].angularVelocity *= wineDamp;
                ices[i].AddForce(new Vector3(0, 0.098f * Mathf.Min(0.1f, height - ices[i].transform.position.y + 0.05f), 0));
                if(pos.y + 0.05f > height)
                    HandleCollision(new Vector3(0.5f - 5 * pos.z, height + 5 * (pos.y - height), 0.5f - 5 * pos.x), 0.2f);
            }
            else ices[i].linearVelocity *= damp; ices[i].angularVelocity *= damp;
            ices[i].AddForce(new Vector3(0, -0.00784f, 0));
        }
    }

    // Blend
    public void Blend()
    {
        StartCoroutine(Stir());
        StartCoroutine(ColorBlend());
    }

    IEnumerator Stir()
    {
        spoon.SetActive(true); Vector3 center = spoon.transform.position;
        float radiusX = 0.05f, radiusZ = 0.05f, deep = 1f / (gridSize - 1), period = 2.0f, time = 0;
        int xl = gridSize * gridSize / 2 - gridSize / 2 - 1, xr = xl + 1, yl = xl + gridSize, yr = yl + 1;
        while (time < period)
        {
            time += Time.deltaTime;
            float angle = 4 * Mathf.PI * time / period;
            HandleCollision(new Vector3(0.5f - Mathf.Cos(angle) / (gridSize - 1),
                height, 0.5f + Mathf.Sin(angle) / (gridSize - 1)), 1f / (3 * gridSize - 3));
            UpdateMassSpring(false);
            spoon.transform.position = center + new Vector3(radiusX * Mathf.Cos(angle), 0, radiusZ * Mathf.Sin(angle));
            yield return null;
        }
        spoon.SetActive(false); spoon.transform.position = center;
    }

    IEnumerator ColorBlend()
    {
        Vector4 color = Vector4.zero; Vector4[] cols = new Vector4[3];
        for (int i = 0; i < layerCnt; i++) color += cols[i] = colors[i]; color /= layerCnt;
        float time = 0;
        while (time < 2)
        {
            time += Time.deltaTime;
            material.SetFloat("_BlendFrac", 0.1f + 0.2f * time);
            for (int i = 0; i < layerCnt; i++) colors[i] = Vector4.Lerp(cols[i], color, 0.5f * time);
            material.SetVectorArray("_LayerCols", colors);
            yield return null;
        }
        material.SetFloat("_BlendFrac", 0.1f);
    }

    public void AddWine()
    {
        if (layerCnt >= 3) return;
        colors[layerCnt++] = color;
        material.SetInt("_LayerCnt", layerCnt);
        material.SetVectorArray("_LayerCols", colors);
        StartCoroutine(AddWineCor());
    }

    IEnumerator AddWineCor()
    {
        float time = 0, h = height;
        while (time < 2)
        {
            time += Time.deltaTime;
            material.SetFloat("_Height", height = Mathf.Lerp(h, layerCnt / 6f - 0.3f, 0.5f * time));
            yield return null;
        }
    }

    public void Finish()
    {
        if (!grass.activeSelf) grass.SetActive(true);
        else
        {
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            material.SetInt("_LayerCnt", layerCnt = 0);
            material.SetFloat("_Height", height = -0.3f);
            colors = new Vector4[3]; grass.SetActive(false);
            material.SetVectorArray("_LayerCols", colors);
        }
    }
}
