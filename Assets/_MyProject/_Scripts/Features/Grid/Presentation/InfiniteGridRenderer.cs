using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Grid.Presentation
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public sealed class InfiniteGridRenderer : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private UnityEngine.Camera targetCamera;
		[SerializeField] private Material gridMaterial;

		[Header("Grid")]
		[SerializeField, Min(0.0001f)] private float cellSize = 1f;
		[SerializeField, Min(1)] private int majorLineEvery = 5;
		[SerializeField, Min(0)] private int extraCells = 2;
		[SerializeField] private float gridPlaneZ = 0f;

		[Header("Visual")]
		[SerializeField] private Color minorLineColor = new Color(1f, 1f, 1f, 0.08f);
		[SerializeField] private Color majorLineColor = new Color(1f, 1f, 1f, 0.18f);
		[SerializeField] private string sortingLayerName = "Default";
		[SerializeField] private int sortingOrder = -1000;

		[Header("Safety")]
		[SerializeField, Min(32)] private int maxLinesPerAxis = 2000;

		private readonly List<Vector3> vertices = new(4096);
		private readonly List<Color32> colors = new(4096);
		private readonly List<int> indices = new(4096);

		private Mesh mesh;
		private MeshFilter meshFilter;
		private MeshRenderer meshRenderer;
		private Material runtimeMaterial;

		private Vector3 lastCameraPosition;
		private float lastOrthographicSize;
		private float lastAspect;
		private int lastPixelWidth;
		private int lastPixelHeight;
		private float lastCellSize;
		private int lastMajorLineEvery;
		private int lastExtraCells;
		private Matrix4x4 lastLocalToWorld;
		private bool dirty = true;

		private void Reset()
		{
			targetCamera = UnityEngine.Camera.main;
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
		}

		private void LateUpdate()
		{
			EnsureInitialized();
			RebuildIfNeeded();
		}

		private void OnValidate()
		{
			cellSize = Mathf.Max(0.0001f, cellSize);
			majorLineEvery = Mathf.Max(1, majorLineEvery);
			extraCells = Mathf.Max(0, extraCells);
			maxLinesPerAxis = Mathf.Max(32, maxLinesPerAxis);
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
				targetCamera = UnityEngine.Camera.main;
			}

			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();

			if (mesh == null)
			{
				mesh = new Mesh
				{
					name = "Infinite Grid Mesh"
				};

				mesh.MarkDynamic();
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
			meshRenderer.sortingLayerName = sortingLayerName;
			meshRenderer.sortingOrder = sortingOrder;
		}

		private Material ResolveMaterial()
		{
			if (gridMaterial != null)
			{
				return gridMaterial;
			}

			if (runtimeMaterial != null)
			{
				return runtimeMaterial;
			}

			Shader shader = Shader.Find("Sprites/Default");

			if (shader == null)
			{
				shader = Shader.Find("Unlit/Transparent");
			}

			if (shader == null)
			{
				Debug.LogError("Grid material was not assigned and no fallback transparent shader was found.");
				return null;
			}

			runtimeMaterial = new Material(shader)
			{
				name = "Runtime Infinite Grid Material",
				hideFlags = HideFlags.HideAndDontSave,
				renderQueue = 3000
			};

			if (runtimeMaterial.HasProperty("_Color"))
			{
				runtimeMaterial.SetColor("_Color", Color.white);
			}

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
				   || !Mathf.Approximately(cellSize, lastCellSize)
				   || majorLineEvery != lastMajorLineEvery
				   || extraCells != lastExtraCells
				   || transform.localToWorldMatrix != lastLocalToWorld;
		}

		private void SaveViewState()
		{
			lastCameraPosition = targetCamera.transform.position;
			lastOrthographicSize = targetCamera.orthographicSize;
			lastAspect = targetCamera.aspect;
			lastPixelWidth = targetCamera.pixelWidth;
			lastPixelHeight = targetCamera.pixelHeight;
			lastCellSize = cellSize;
			lastMajorLineEvery = majorLineEvery;
			lastExtraCells = extraCells;
			lastLocalToWorld = transform.localToWorldMatrix;
		}

		private void Rebuild()
		{
			float halfHeight = targetCamera.orthographicSize;
			float halfWidth = halfHeight * targetCamera.aspect;
			Vector3 cameraPosition = targetCamera.transform.position;

			float padding = extraCells * cellSize;

			float minX = cameraPosition.x - halfWidth - padding;
			float maxX = cameraPosition.x + halfWidth + padding;
			float minY = cameraPosition.y - halfHeight - padding;
			float maxY = cameraPosition.y + halfHeight + padding;

			float step = CalculateSafeStep(maxX - minX, maxY - minY);

			int startX = Mathf.FloorToInt(minX / step);
			int endX = Mathf.CeilToInt(maxX / step);
			int startY = Mathf.FloorToInt(minY / step);
			int endY = Mathf.CeilToInt(maxY / step);

			vertices.Clear();
			colors.Clear();
			indices.Clear();

			for (int xIndex = startX; xIndex <= endX; xIndex++)
			{
				float x = xIndex * step;
				Color color = IsMajorLine(x) ? majorLineColor : minorLineColor;

				AddWorldLine(
					new Vector3(x, startY * step, gridPlaneZ),
					new Vector3(x, endY * step, gridPlaneZ),
					color
				);
			}

			for (int yIndex = startY; yIndex <= endY; yIndex++)
			{
				float y = yIndex * step;
				Color color = IsMajorLine(y) ? majorLineColor : minorLineColor;

				AddWorldLine(
					new Vector3(startX * step, y, gridPlaneZ),
					new Vector3(endX * step, y, gridPlaneZ),
					color
				);
			}

			mesh.Clear();

			mesh.indexFormat = vertices.Count > 65535
				? IndexFormat.UInt32
				: IndexFormat.UInt16;

			mesh.SetVertices(vertices);
			mesh.SetColors(colors);
			mesh.SetIndices(indices, MeshTopology.Lines, 0, true);
			mesh.RecalculateBounds();
		}

		private float CalculateSafeStep(float visibleWidth, float visibleHeight)
		{
			float step = cellSize;

			while (visibleWidth / step > maxLinesPerAxis ||
				   visibleHeight / step > maxLinesPerAxis)
			{
				step *= 2f;
			}

			return step;
		}

		private bool IsMajorLine(float coordinate)
		{
			float majorSize = cellSize * majorLineEvery;

			if (majorSize <= 0f)
			{
				return false;
			}

			float normalized = coordinate / majorSize;
			return Mathf.Abs(normalized - Mathf.Round(normalized)) < 0.001f;
		}

		private void AddWorldLine(Vector3 worldStart, Vector3 worldEnd, Color lineColor)
		{
			int startIndex = vertices.Count;
			Color32 color = lineColor;

			vertices.Add(transform.InverseTransformPoint(worldStart));
			vertices.Add(transform.InverseTransformPoint(worldEnd));

			colors.Add(color);
			colors.Add(color);

			indices.Add(startIndex);
			indices.Add(startIndex + 1);
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