# ownCreate-RenderPipeline

**一、Custom SRP**
from catlike coding

**1.Custom Render Pipeline**
`注意：该rp只适用于color space为linear的情况，不适用于gamma。`

**1.1 RenderPipelineAsset**
```
[CreateAssetMenu{menuName = ".../......"}]
public class ...RenderPipelineAsset : RenderPipelineAsset{}

CreateAssetMenu在菜单中建立一个可选选项，以生成相关组件
使用RenderPipelineAsset作为类型

protected override RenderPipeline CreatePipeline() {}

使用override重载->可以随时更改函数并使用，多线并行？
```
**1.2 RenderPipeline**
`管线的最外层主体，决定cameras的渲染顺序等等
同样使用RenderPipelineAsset作为类型`

**1.3 CameraRenderer**


**2.1CameraRenderer**
```
public class CameraRenderer {

	ScriptableRenderContext context;

	Camera camera;

	public void Render (ScriptableRenderContext context, Camera camera) {
		this.context = context;
		this.camera = camera;
	}
}

Render： 渲染所有摄像机视野范围内的图形
```
**2.2 skybox**

```
	public void Render (ScriptableRenderContext context, Camera camera) {
		this.context = context;
		this.camera = camera;

		Setup();
		DrawVisibleGeometry();
		Submit();
	}

	void Setup () {
		context.SetupCameraProperties(camera);//对其摄像机矩阵（unity——MatrixVP正交矩阵）
	}

	void Submit () {
		context.Submit();
	}

	void DrawVisibleGeometry () {
		context.DrawSkybox(camera);
	}


 使用DrawSkybox来单独绘制skybox，是否绘制只由clearflag控制
```
**2.3 command buffer**
```
const string bufferName = "Render Camera";

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

为相机建立缓冲队列

void Setup () {
		buffer.BeginSample(bufferName);
		ExecuteBuffer();
		context.SetupCameraProperties(camera);
	}

	void Submit () {
		buffer.EndSample(bufferName);
		ExecuteBuffer();
		context.Submit();
	}

	void ExecuteBuffer () {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

将设好的buffer名写入队列中，这样在frame debugger里就能展示出来

使用ExecuteBuffer来执行buffer，并且在每次执行后对buffer进行clear
```
**2.4 clear flag**


```
	void Setup () {
		context.SetupCameraProperties(camera);//作为第一个动作，效率最高-> Clear(color+Z+stencil)
		buffer.ClearRenderTarget(true, true, Color.clear);//在BeginSample前
		buffer.BeginSample(bufferName);
		ExecuteBuffer();
	}
```
**2.5 Culling**
```
	bool Cull () {
		//ScriptableCullingParameters p
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
			return true;
		}
		return false;
	}

	public void Render (ScriptableRenderContext context, Camera camera) {
		this.context = context;
		this.camera = camera;

		if (!Cull()) {
			return;
		}

		Setup();
		DrawVisibleGeometry();
		Submit();
	}

 	CullingResults cullingResults;
  	...
	bool Cull () {
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
			cullingResults = context.Cull(ref p);//ref作为优化，放置传递大结构体副本
			return true;
		}
		return false;
	}
 ```
**2.6 Drawing Geometry**
```
	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	void DrawVisibleGeometry () {
		var sortingSettings = new SortingSettings(camera) {
			criteria = SortingCriteria.CommonOpaque
		};//通过前后顺序绘制（尤其是不透明物体）
		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

		context.DrawSkybox(camera);

		//对于透明对象反转绘制顺序
  		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;
  
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}
 ```
**3 Editor Rendering**
```
	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	public void Render (ScriptableRenderContext context, Camera camera) {
		…

		Setup();
		DrawVisibleGeometry();
		DrawUnsupportedShaders();
		Submit();
	}

	…

	static Material errorMaterial;

		void DrawUnsupportedShaders () {
		if (errorMaterial == null) {
			errorMaterial =
				new Material(Shader.Find("Hidden/InternalErrorShader"));
		}

		var drawingSettings = new DrawingSettings(
			legacyShaderTagIds[0], new SortingSettings(camera)
		){
			overrideMaterial = errorMaterial
		};

		for (int i = 1; i < legacyShaderTagIds.Length; i++) {
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}

		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);
	}
```
**3.3 Partical Class**//拆分代码
```
	partial class CameraRenderer {

	#if UNITY_EDITOR

	static ShaderTagId[] legacyShaderTagIds = { … }
	};

	static Material errorMaterial;

	void DrawUnsupportedShaders () { … }

	#endif

	partial void DrawUnsupportedShaders ();

	#if UNITY_EDITOR

	…

	partial void DrawUnsupportedShaders () { … }

	#endif
}
```
**3.4 Drawing Gizmos**
```
partial class CameraRenderer {
	
	partial void DrawGizmos ();

	partial void DrawUnsupportedShaders ();

	#if UNITY_EDITOR

	…

	partial void DrawGizmos () {
		if (Handles.ShouldRenderGizmos()) {
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}

	partial void DrawUnsupportedShaders () { … }

	#endif
}

	public void Render (ScriptableRenderContext context, Camera camera) {
		…

		Setup();
		DrawVisibleGeometry();
		DrawUnsupportedShaders();
		DrawGizmos();//在所有其他东西后绘制
		Submit();
	}

```
**3.5 Drawing unity UI**

```
	partial void PrepareForSceneWindow ();

	#if UNITY_EDITOR

	…

	partial void PrepareForSceneWindow () {
		if (camera.cameraType == CameraType.SceneView) {
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
		}
	}//使用world尺寸

		PrepareForSceneWindow();
		if (!Cull()) {
			return;
		}

```
**4 Multiple Cameras**//多个相机会按照Depth值从小到大绘制

```
	partial void PrepareBuffer ();

	…

	string SampleName { get; set; }
	
	…

#if UNITY_EDITOR

	…
	
	partial void PrepareBuffer () {
		Profiler.BeginSample("Editor Only");
		buffer.name = SampleName = camera.name;
		Profiler.EndSample();
	}//为相机分别取样

#else

	const string SampleName = bufferName;

#endif

#endif

	partial void PrepareBuffer ();

#if UNITY_EDITOR

	…
	
	partial void PrepareBuffer () {
		buffer.name = camera.name;
	}

#endif

	void Setup () {
		context.SetupCameraProperties(camera);
		buffer.ClearRenderTarget(true, true, Color.clear);
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
	}

	void Submit () {
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
	}

使用GC Alloc检索排序相机名称
```
**4.4 Clear Flags**//用于合并多个相机结果

···
	void Setup () {
		context.SetupCameraProperties(camera);
		CameraClearFlags flags = camera.clearFlags;
		buffer.ClearRenderTarget(true, true, Color.clear);
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
	}

	buffer.ClearRenderTarget(
			flags <= CameraClearFlags.Depth,
			flags == CameraClearFlags.Color,
			flags == CameraClearFlags.Color ?
			camera.backgroundColor.linear : Color.clear
	); 

 对渲染主要物体的摄像机，使用skybox/Color 的clear flags设置。
 对其他使用 Depth only。（比如绘制错误物体的摄像机）
