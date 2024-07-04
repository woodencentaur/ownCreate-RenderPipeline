using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

partial class CameraRenderer
{
#if UNITY_EDITOR
    static ShaderTagId[] _legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    private static Material errorMaterial;

    partial void DrawGizmos();
    partial void DrawUnsupportedShaders();

    partial void PrepareForSceneWindow();

    partial void PrepareBuffer();

    
    #if UNITY_EDITOR
    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var _drawingSettings = new DrawingSettings(
            _legacyShaderTagIds[0], new SortingSettings(_camera))
        {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < _legacyShaderTagIds.Length; i++)
        {
            _drawingSettings.SetShaderPassName(i, _legacyShaderTagIds[i]);
        }

        var _filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(
            _cullingResults, ref _drawingSettings, ref _filteringSettings);
    }

    partial void PrepareForSceneWindow()
    {
        if (_camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
        }
    }
    
    private string SampleName { get; set; }
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Edition Only");
        _buffer.name = SampleName = _camera.name;
        Profiler.EndSample();
    }
    #else
        const string SamplerName = bufferName;
    #endif

#endif
}