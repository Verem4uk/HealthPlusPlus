using UnityEngine;
using UnityEngine.UI;

// Vector ornament remains crisp on both phones and wide desktop canvases.
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class VikingOrnament : MaskableGraphic
{
    public bool Vertical;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        float length = Vertical ? rect.height : rect.width;
        float breadth = Vertical ? rect.width : rect.height;
        int count = Mathf.Max(1, Mathf.FloorToInt(length / Mathf.Max(24, breadth * 2.2f)));
        float step = length / count;
        for (int i = 0; i < count; i++)
        {
            float start = -length * .5f + i * step;
            for (int strand = -1; strand <= 1; strand += 2)
            {
                Vector2 previous = default;
                for (int j = 0; j <= 16; j++)
                {
                    float t = j / 16f;
                    float across = Mathf.Sin(t * Mathf.PI * 2) * breadth * .31f * strand;
                    var point = Vertical ? new Vector2(across, start + t * step) : new Vector2(start + t * step, across);
                    point += rect.center;
                    if (j > 0) Line(mesh, previous, point, Mathf.Max(1, breadth * .055f), color);
                    previous = point;
                }
            }
        }
    }
    private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
    {
        var side = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
        int index = mesh.currentVertCount;
        mesh.AddVert(a - side, tint, Vector2.zero);
        mesh.AddVert(a + side, tint, Vector2.zero);
        mesh.AddVert(b + side, tint, Vector2.zero);
        mesh.AddVert(b - side, tint, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }
}
