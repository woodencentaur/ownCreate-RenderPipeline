using UnityEngine;
using UnityEngine.Rendering;
public partial class CameraRenderer
{
    ScriptableRenderContext _context;

    Camera _camera;
    const string bufferName = "Render Camera";

    CommandBuffer _buffer = new CommandBuffer {
        name = bufferName
    };

    static ShaderTagId _unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this._context = context;
        this._camera = camera;
    
        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull())
        {
            return;
        }
       
        Setup();
        DrawVisibleGeometry ();
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }

    CullingResults _cullingResults;
    bool Cull()
    {
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            _cullingResults = _context.Cull(ref p);
            return true;
        }

        return false;
    }
    
    void Setup()
    {
        _context.SetupCameraProperties(_camera);
        CameraClearFlags flags = _camera.clearFlags;
        //注意顺序，先清屏再进缓冲
        _buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear
        );
        _buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }
    
    void Submit()
    {
        _buffer.EndSample(SampleName);
        ExecuteBuffer();
        _context.Submit();
    }

    void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
    
    void DrawVisibleGeometry()
    {
        var _sortingSettings = new SortingSettings(_camera) {
            criteria = SortingCriteria.CommonOpaque
        };
        var _drawingSettings = new DrawingSettings(_unlitShaderTagId, _sortingSettings);
        var _filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        _context.DrawRenderers(_cullingResults, ref _drawingSettings, ref _filteringSettings);
        
        _context.DrawSkybox(_camera);

        _sortingSettings.criteria = SortingCriteria.CommonTransparent;
        _drawingSettings.sortingSettings = _sortingSettings;
        _filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        
        _context.DrawRenderers( _cullingResults, ref _drawingSettings, ref _filteringSettings);
    } 
    
}
