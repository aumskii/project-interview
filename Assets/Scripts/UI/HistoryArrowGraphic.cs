using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class HistoryArrowGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect rect = GetPixelAdjustedRect();
        vertices.AddVert(new Vector3(rect.xMin, rect.yMin, 0f), color, Vector2.zero);
        vertices.AddVert(new Vector3(rect.xMin, rect.yMax, 0f), color, Vector2.zero);
        vertices.AddVert(new Vector3(rect.xMax, rect.center.y, 0f), color, Vector2.zero);
        vertices.AddTriangle(0, 1, 2);
    }
}
