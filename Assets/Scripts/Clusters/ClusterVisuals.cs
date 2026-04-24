using UnityEngine;

namespace MahjongGame.Clusters
{
    internal static class ClusterVisuals
    {
        public static Material CreateBrightMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = color;
            return material;
        }
    }
}
