using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexTileMeshGenerator : MonoBehaviour
{
    [Header("Shape")]
    public float radius = 0.5f;     // center -> corner
    public float height = 0.1f;     // thickness (Y)
    public bool addCollider = true;

    void Awake()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        var mf = GetComponent<MeshFilter>();
        var mesh = new Mesh { name = "HexPrism" };

        // pointy-top hex in XZ plane
        // top and bottom rings: 6 vertices each + center each
        Vector3[] verts = new Vector3[6 + 6 + 2];
        int topCenter = 12;
        int botCenter = 13;

        float topY = +height * 0.5f;
        float botY = -height * 0.5f;

        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i - 30f; // pointy-top
            float a = angleDeg * Mathf.Deg2Rad;
            float x = radius * Mathf.Cos(a);
            float z = radius * Mathf.Sin(a);

            verts[i] = new Vector3(x, topY, z);     // top ring 0..5
            verts[6 + i] = new Vector3(x, botY, z); // bottom ring 6..11
        }

        verts[topCenter] = new Vector3(0, topY, 0);
        verts[botCenter] = new Vector3(0, botY, 0);

        // triangles
        // top: 6
        // bottom: 6
        // sides: 12 (2 per side)
        int[] tris = new int[(6 + 6 + 12) * 3];
        int t = 0;

        // top face (clockwise when looking from above? Unity uses clockwise = front by default depends; we’ll ensure normals via RecalculateNormals)
        for (int i = 0; i < 6; i++)
        {
            int a = i;
            int b = (i + 1) % 6;
            tris[t++] = topCenter;
            tris[t++] = a;
            tris[t++] = b;
        }

        // bottom face
        for (int i = 0; i < 6; i++)
        {
            int a = 6 + i;
            int b = 6 + ((i + 1) % 6);
            tris[t++] = botCenter;
            tris[t++] = b;
            tris[t++] = a;
        }

        // sides
        for (int i = 0; i < 6; i++)
        {
            int topA = i;
            int topB = (i + 1) % 6;
            int botA = 6 + i;
            int botB = 6 + ((i + 1) % 6);

            // quad = (topA, topB, botB, botA) -> two tris
            tris[t++] = topA;
            tris[t++] = topB;
            tris[t++] = botB;

            tris[t++] = topA;
            tris[t++] = botB;
            tris[t++] = botA;
        }

        // UV (간단 planar)
        Vector2[] uvs = new Vector2[verts.Length];
        for (int i = 0; i < 12; i++)
            uvs[i] = new Vector2(verts[i].x / (radius * 2f) + 0.5f, verts[i].z / (radius * 2f) + 0.5f);
        uvs[topCenter] = new Vector2(0.5f, 0.5f);
        uvs[botCenter] = new Vector2(0.5f, 0.5f);

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;

        if (addCollider)
        {
            var mc = GetComponent<MeshCollider>();
            if (!mc) mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = false;
        }
    }
}
