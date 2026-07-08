using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Grid.Presentation
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public sealed class GraphPanoramaBackgroundRenderer : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private Camera targetCamera;
		[SerializeField] private Material panoramaMaterial;

		[Header("Plane")]
		[SerializeField] private float backgroundPlaneZ = 1f;
		[SerializeField, Min(0f)] private float padding = 2f;

		[Header("Renderer")]
		[SerializeField] private string sortingLayerName = "Default";
		[SerializeField] private int sortingOrder = -2000;

		private static readonly int CameraWorldPositionId =
			Shader.PropertyToID("_CameraWorldPosition");

		private Mesh mesh;
		private MeshFilter meshFilter;
		private MeshRenderer meshRenderer;
		private Material runtimeMaterial;
		private MaterialPropertyBlock propertyBlock;

		private Vector3 lastCameraPosition;
		private float lastOrthographicSize;
		private float lastAspect;
		private int lastPixelWidth;
		private int lastPixelHeight;
		private float lastBackgroundPlaneZ;
		private float lastPadding;
		private bool dirty = true;

		private readonly Vector3[] vertices = new Vector3[4];
		private readonly Vector2[] uvs = new Vector2[4];
		private readonly Color[] colors = new Color[4];
		private readonly int[] triangles =
		{
			0, 1, 2,
			2, 3, 0
		};

		private void Reset()
		{
			targetCamera = Camera.main;
		}

		private void Awake()
		{
			EnsureInitialized();
		}

		private void OnEnable()
		{
			EnsureInitialized();
			dirty = true;
			RebuildIfNeeded();
			ApplyPropertyBlock();
		}

		private void LateUpdate()
		{
			EnsureInitialized();
			RebuildIfNeeded();
			ApplyPropertyBlock();
		}

		private void OnValidate()
		{
			padding = Mathf.Max(0f, padding);
			dirty = true;

			if (meshRenderer != null)
			{
				ApplyRendererSettings();
			}
		}

		private void EnsureInitialized()
		{
			if (targetCamera == null)
			{
				targetCamera = Camera.main;
			}

			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();

			if (propertyBlock == null)
			{
				propertyBlock = new MaterialPropertyBlock();
			}

			if (mesh == null)
			{
				mesh = new Mesh
				{
					name = "Graph Panorama Background Mesh"
				};

				mesh.MarkDynamic();

				uvs[0] = new Vector2(0f, 0f);
				uvs[1] = new Vector2(1f, 0f);
				uvs[2] = new Vector2(1f, 1f);
				uvs[3] = new Vector2(0f, 1f);

				colors[0] = Color.white;
				colors[1] = Color.white;
				colors[2] = Color.white;
				colors[3] = Color.white;
			}

			if (meshFilter.sharedMesh != mesh)
			{
				meshFilter.sharedMesh = mesh;
			}

			ApplyRendererSettings();
		}

		private void ApplyRendererSettings()
		{
			if (meshRenderer == null)
			{
				return;
			}

			meshRenderer.sharedMaterial = ResolveMaterial();
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			meshRenderer.lightProbeUsage = LightProbeUsage.Off;
			meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
			meshRenderer.sortingLayerName = sortingLayerName;
			meshRenderer.sortingOrder = sortingOrder;
		}

		private Material ResolveMaterial()
		{
			if (panoramaMaterial != null)
			{
				return panoramaMaterial;
			}

			if (runtimeMaterial != null)
			{
				return runtimeMaterial;
			}

			Shader shader = Shader.Find("GraphComputer/World/Panorama Background");

			if (shader == null)
			{
				shader = Shader.Find("Unlit/Color");
			}

			if (shader == null)
			{
				Debug.LogError(
					"Panorama material was not assigned and fallback shader was not found.",
					this
				);

				return null;
			}

			runtimeMaterial = new Material(shader)
			{
				name = "Runtime Graph Panorama Material",
				hideFlags = HideFlags.HideAndDontSave,
				renderQueue = 2900
			};

			return runtimeMaterial;
		}

		private void RebuildIfNeeded()
		{
			if (targetCamera == null || !targetCamera.orthographic)
			{
				return;
			}

			if (!dirty && !HasViewChanged())
			{
				return;
			}

			Rebuild();
			SaveViewState();
			dirty = false;
		}

		private bool HasViewChanged()
		{
			return targetCamera.transform.position != lastCameraPosition
				   || !Mathf.Approximately(targetCamera.orthographicSize, lastOrthographicSize)
				   || !Mathf.Approximately(targetCamera.aspect, lastAspect)
				   || targetCamera.pixelWidth != lastPixelWidth
				   || targetCamera.pixelHeight != lastPixelHeight
				   || !Mathf.Approximately(backgroundPlaneZ, lastBackgroundPlaneZ)
				   || !Mathf.Approximately(padding, lastPadding);
		}

		private void SaveViewState()
		{
			lastCameraPosition = targetCamera.transform.position;
			lastOrthographicSize = targetCamera.orthographicSize;
			lastAspect = targetCamera.aspect;
			lastPixelWidth = targetCamera.pixelWidth;
			lastPixelHeight = targetCamera.pixelHeight;
			lastBackgroundPlaneZ = backgroundPlaneZ;
			lastPadding = padding;
		}

		private void Rebuild()
		{
			float halfHeight = targetCamera.orthographicSize + padding;
			float halfWidth = halfHeight * targetCamera.aspect + padding;

			Vector3 cameraPosition = targetCamera.transform.position;

			float minX = cameraPosition.x - halfWidth;
			float maxX = cameraPosition.x + halfWidth;
			float minY = cameraPosition.y - halfHeight;
			float maxY = cameraPosition.y + halfHeight;

			vertices[0] = transform.InverseTransformPoint(new Vector3(minX, minY, backgroundPlaneZ));
			vertices[1] = transform.InverseTransformPoint(new Vector3(maxX, minY, backgroundPlaneZ));
			vertices[2] = transform.InverseTransformPoint(new Vector3(maxX, maxY, backgroundPlaneZ));
			vertices[3] = transform.InverseTransformPoint(new Vector3(minX, maxY, backgroundPlaneZ));

			mesh.Clear();
			mesh.SetVertices(vertices);
			mesh.SetUVs(0, uvs);
			mesh.SetColors(colors);
			mesh.SetTriangles(triangles, 0, true);
			mesh.RecalculateBounds();
		}

		private void ApplyPropertyBlock()
		{
			if (meshRenderer == null || targetCamera == null)
			{
				return;
			}

			meshRenderer.GetPropertyBlock(propertyBlock);

			propertyBlock.SetVector(
				CameraWorldPositionId,
				targetCamera.transform.position
			);

			meshRenderer.SetPropertyBlock(propertyBlock);
		}

		private void OnDestroy()
		{
			if (mesh != null)
			{
				if (Application.isPlaying)
				{
					Destroy(mesh);
				}
				else
				{
					DestroyImmediate(mesh);
				}
			}

			if (runtimeMaterial != null)
			{
				if (Application.isPlaying)
				{
					Destroy(runtimeMaterial);
				}
				else
				{
					DestroyImmediate(runtimeMaterial);
				}
			}
		}
	}
}