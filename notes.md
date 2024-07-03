# ownCreate-RenderPipeline
from catlike coding

**1.Custom Render Pipeline**
`注意：该rp只适用于color space为linear的情况，不适用于gamma。`

**1.1 RenderPipelineAsset**
```
[CreateAssetMenu{menuName = ".../......"}]
public class ...RenderPipelineAsset : RenderPipelineAsset{}
```

CreateAssetMenu在菜单中建立一个可选选项，以生成相关组件
使用RenderPipelineAsset作为类型

```
protected override RenderPipeline CreatePipeline() {}
```

使用override重载->可以随时更改函数并使用，多线并行？

**1.2 RenderPipeline**
`管线的最外层主体，决定cameras的渲染顺序等等
同样使用RenderPipelineAsset作为类型`

**1.3 CameraRenderer**

```
public class CameraRenderer
```

无需引用类型

```
ScriptableRenderContext context;
Camera camera;
public void Render (ScriptableRenderContext context, Camera camera) {
		this.context = context;
		this.camera = camera;
	}
```

Render： 渲染所有摄像机视野范围内的图形

```
public void Render (ScriptableRenderContext context, Camera camera) {
		this.context = context;
		this.camera = camera;
		DrawVisibleGeometry();
	}
	void DrawVisibleGeometry () {
		context.DrawSkybox(camera);
	}
```

单独绘制天空盒，注意此步只是将绘制动作放入了缓冲队列中，并未真正绘制

```
public void Render (ScriptableRenderContext context, Camera camera) {
		this.context = context;
		this.camera = camera;

		DrawVisibleGeometry();
		Submit();
	}

	void Submit () {
		context.Submit();
	}
```

提交context中的buffer，与上一步一同构成一个draw mesh。代表着一次draw call

```
public void Render (ScriptableRenderContext context, Camera camera) {
		this.context = context;
		this.camera = camera;

		Setup();
		DrawVisibleGeometry();
		Submit();
	}

	void Setup () {
		context.SetupCameraProperties(camera);
	}
```

设置view-projection矩阵(unity_MatrixVP),应用相机属性

```
const string bufferName = "Render Camera";

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};
```

为相机建立缓冲队列

```
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
```

将设好的buffer名写入队列中，这样在frame debugger里就能展示出来

使用ExecuteBuffer来执行buffer，并且在每次执行后对buffer进行clear

```
CameraRenderer renderer = new CameraRenderer();
	protected override void Render (
		ScriptableRenderContext context, Camera[] cameras
	) {}
		protected override void Render (
		ScriptableRenderContext context, List<Camera> cameras
	) {
		for (int i = 0; i < cameras.Count; i++) {
			renderer.Render(context, cameras[i]);
		}
	}
```

在第二个Render重载里->在一个循环里按顺序渲染camera
