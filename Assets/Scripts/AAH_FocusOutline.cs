using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class AAH_FocusOutline
{
    private readonly List<GameObject> _layers = new List<GameObject>();
    private readonly Material _mask, _edge;
    private bool _visible;
    public AAH_FocusOutline(Transform root)
    {
        var shader = Resources.Load<Shader>("VetNightmare/FocusOutline");
        if (shader == null) { Debug.LogError("Thiếu shader VetNightmare/FocusOutline", root); return; }
        _mask = LayerMaterial(shader, 3010, 0, CompareFunction.Always, StencilOp.Replace);
        _edge = LayerMaterial(shader, 3011, 3, CompareFunction.NotEqual, StencilOp.Keep);
        foreach (var source in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (source.sharedMesh == null || !source.TryGetComponent<MeshRenderer>(out var original) || !original.enabled) continue;
            AddLayer(source, _mask);
            AddLayer(source, _edge);
        }
    }
    private static Material LayerMaterial(Shader shader, int queue, int pixels, CompareFunction test, StencilOp op)
    {
        var m = new Material(shader) { renderQueue = queue };
        m.SetInt("_ColorMask", pixels == 0 ? 0 : 15);
        m.SetInt("_Test", (int)test);
        m.SetInt("_Op", (int)op);
        m.SetFloat("_Pixels", pixels);
        return m;
    }
    private void AddLayer(MeshFilter source, Material material)
    {
        var obj = new GameObject("Focus outline");
        obj.SetActive(false);
        obj.layer = source.gameObject.layer;
        obj.transform.SetParent(source.transform, false);
        var mesh = source.sharedMesh;
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.AddComponent<MeshRenderer>();
        var materials = new Material[mesh.subMeshCount];
        for (int i = 0; i < materials.Length; i++) materials[i] = material;
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var block = new MaterialPropertyBlock();
        block.SetVector("_Center", mesh.bounds.center);
        renderer.SetPropertyBlock(block);
        _layers.Add(obj);
    }
    public void Show(bool visible)
    {
        if (_visible == visible) return;
        _visible = visible;
        foreach (var layer in _layers) layer.SetActive(visible);
    }
    public void Dispose()
    {
        foreach (var layer in _layers) if (layer != null) Object.Destroy(layer);
        if (_mask != null) Object.Destroy(_mask);
        if (_edge != null) Object.Destroy(_edge);
    }
}
