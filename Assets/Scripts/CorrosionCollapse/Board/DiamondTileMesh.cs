using UnityEngine;

namespace Dynasty.Legacy.CorrosionCollapse.Board
{
    public sealed class DiamondTileMesh : MonoBehaviour
    {
        private static readonly Vector3[] Vertices =
        {
            new Vector3(0f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(0f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0f)
        };

        private static readonly int[] Triangles = { 0, 1, 2, 0, 2, 3 };

        public static void Ensure(GameObject target)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = target.AddComponent<MeshFilter>();
            }

            if (target.GetComponent<MeshRenderer>() == null)
            {
                target.AddComponent<MeshRenderer>();
            }

            Mesh mesh = filter.sharedMesh;
            if (mesh == null || mesh.name != "CorrosionDiamondTile")
            {
                mesh = new Mesh
                {
                    name = "CorrosionDiamondTile"
                };
                mesh.vertices = Vertices;
                mesh.triangles = Triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                filter.sharedMesh = mesh;
            }
        }
    }
}
