using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a procedural mesh ribbon from a list of positions.
/// Produces consistent-width lines without the corner artifacts of LineRenderer.
/// Full rebuild each frame — the chart Y-axis rescales continuously, moving all
/// positions, so incremental updates would leave stale vertices.
/// </summary>
public class ChartMeshLine
{
    private Mesh _mesh;
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<int> _triangles = new List<int>();
    private readonly List<Color> _colors = new List<Color>();

    public Mesh Mesh
    {
        get
        {
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.MarkDynamic();
            }
            return _mesh;
        }
    }

    /// <summary>
    /// Rebuilds the entire mesh from the given positions every frame.
    /// </summary>
    public void UpdateMesh(List<Vector3> positions, float width, Color color)
    {
        int count = positions.Count;
        if (count < 2)
        {
            Clear();
            return;
        }

        float halfWidth = width * 0.5f;

        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();

        for (int i = 0; i < count; i++)
        {
            Vector2 perp = GetPerp(positions, i, count);
            Vector3 offset = new Vector3(perp.x, perp.y, 0f) * halfWidth;
            _vertices.Add(positions[i] + offset);
            _vertices.Add(positions[i] - offset);
            _colors.Add(color);
            _colors.Add(color);
        }

        for (int i = 0; i < count - 1; i++)
        {
            int vi = i * 2;
            _triangles.Add(vi);
            _triangles.Add(vi + 2);
            _triangles.Add(vi + 1);
            _triangles.Add(vi + 1);
            _triangles.Add(vi + 2);
            _triangles.Add(vi + 3);
        }

        Mesh.Clear();
        Mesh.SetVertices(_vertices);
        Mesh.SetTriangles(_triangles, 0);
        Mesh.SetColors(_colors);
    }

    /// <summary>
    /// Rebuilds the entire mesh with per-point colors. Colors list must match positions length.
    /// </summary>
    public void UpdateMesh(List<Vector3> positions, float width, List<Color> colors)
    {
        int count = positions.Count;
        if (count < 2)
        {
            Clear();
            return;
        }

        float halfWidth = width * 0.5f;

        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();

        for (int i = 0; i < count; i++)
        {
            Vector2 perp = GetPerp(positions, i, count);
            Vector3 offset = new Vector3(perp.x, perp.y, 0f) * halfWidth;
            _vertices.Add(positions[i] + offset);
            _vertices.Add(positions[i] - offset);
            _colors.Add(colors[i]);
            _colors.Add(colors[i]);
        }

        for (int i = 0; i < count - 1; i++)
        {
            int vi = i * 2;
            _triangles.Add(vi);
            _triangles.Add(vi + 2);
            _triangles.Add(vi + 1);
            _triangles.Add(vi + 1);
            _triangles.Add(vi + 2);
            _triangles.Add(vi + 3);
        }

        Mesh.Clear();
        Mesh.SetVertices(_vertices);
        Mesh.SetTriangles(_triangles, 0);
        Mesh.SetColors(_colors);
    }

    public void Clear()
    {
        Mesh.Clear();
        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();
    }

    /// <summary>
    /// Calculates a unit-length perpendicular at a point for consistent width.
    /// Interior points average adjacent segment perpendiculars.
    /// </summary>
    private static Vector2 GetPerp(List<Vector3> positions, int index, int count)
    {
        if (index == 0)
        {
            Vector2 dir = ((Vector2)(positions[1] - positions[0])).normalized;
            return new Vector2(-dir.y, dir.x);
        }

        if (index == count - 1)
        {
            Vector2 dir = ((Vector2)(positions[count - 1] - positions[count - 2])).normalized;
            return new Vector2(-dir.y, dir.x);
        }

        Vector2 dirPrev = ((Vector2)(positions[index] - positions[index - 1])).normalized;
        Vector2 dirNext = ((Vector2)(positions[index + 1] - positions[index])).normalized;
        Vector2 perpPrev = new Vector2(-dirPrev.y, dirPrev.x);
        Vector2 perpNext = new Vector2(-dirNext.y, dirNext.x);
        Vector2 avg = perpPrev + perpNext;

        if (avg.sqrMagnitude < 0.001f)
            return perpPrev;

        return avg.normalized;
    }
}
