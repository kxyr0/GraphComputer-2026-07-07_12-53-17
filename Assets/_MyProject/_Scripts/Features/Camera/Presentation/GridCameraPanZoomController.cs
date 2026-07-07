using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Features.Camera.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(UnityEngine.Camera))]
	public sealed class GridCameraPanZoomController : MonoBehaviour
	{
		private enum MouseDragButton
		{
			Left,
			Right,
			Middle
		}

		[Header("Mouse Drag")]
		[SerializeField] private MouseDragButton dragButton = MouseDragButton.Middle;
		[SerializeField, Min(0f)] private float dragSpeed = 1f;
		[SerializeField] private bool invertDrag;
		[SerializeField] private bool requirePointerInsideScreen = true;

		[Header("Keyboard Pan")]
		[SerializeField, Min(0f)] private float keyboardPanSpeed = 20f;
		[SerializeField] private bool keyboardSpeedScalesWithZoom = true;
		[SerializeField] private bool invertKeyboardPan;

		[Header("Zoom")]
		[SerializeField, Min(0.01f)] private float minOrthographicSize = 2f;
		[SerializeField, Min(0.01f)] private float maxOrthographicSize = 80f;
		[SerializeField, Min(1.01f)] private float zoomMultiplierPerWheelStep = 1.25f;
		[SerializeField] private bool zoomToMouse = true;
		[SerializeField] private bool requirePointerInsideScreenForZoom = true;

		[Header("World Plane")]
		[SerializeField] private float worldPlaneZ = 0f;

		[Header("Shortcuts")]
		[SerializeField] private bool resetViewWithHome = true;

		private UnityEngine.Camera targetCamera;

		private Vector3 startPosition;
		private float startOrthographicSize;

		private Vector3 lastMouseWorldPosition;
		private bool isDragging;

		private void Awake()
		{
			targetCamera = GetComponent<UnityEngine.Camera>();

			startPosition = transform.position;

			if (targetCamera != null)
			{
				startOrthographicSize = targetCamera.orthographicSize;
			}
		}

		private void Update()
		{
			if (targetCamera == null)
			{
				return;
			}

			HandleKeyboardPan();
			HandleMouseDrag();
			HandleZoom();
			HandleReset();
		}

		private void HandleKeyboardPan()
		{
			Keyboard keyboard = Keyboard.current;

			if (keyboard == null)
			{
				return;
			}

			Vector2 input = Vector2.zero;

			if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
			{
				input.x -= 1f;
			}

			if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
			{
				input.x += 1f;
			}

			if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
			{
				input.y -= 1f;
			}

			if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
			{
				input.y += 1f;
			}

			if (input.sqrMagnitude <= 0f)
			{
				return;
			}

			if (input.sqrMagnitude > 1f)
			{
				input.Normalize();
			}

			Vector3 right = targetCamera.transform.right;
			Vector3 up = targetCamera.transform.up;

			right.z = 0f;
			up.z = 0f;

			right.Normalize();
			up.Normalize();

			Vector3 delta = (right * input.x + up * input.y) *
							keyboardPanSpeed *
							Time.unscaledDeltaTime;

			if (keyboardSpeedScalesWithZoom && targetCamera.orthographic)
			{
				delta *= Mathf.Max(0.05f, targetCamera.orthographicSize / 5f);
			}

			if (invertKeyboardPan)
			{
				delta = -delta;
			}

			MoveCamera(delta);
		}

		private void HandleMouseDrag()
		{
			Mouse mouse = Mouse.current;

			if (mouse == null)
			{
				return;
			}

			ButtonControl button = GetDragButton(mouse);

			if (button.wasPressedThisFrame)
			{
				isDragging = TryGetMouseWorldPosition(
					out lastMouseWorldPosition,
					requirePointerInsideScreen
				);
			}

			if (button.wasReleasedThisFrame)
			{
				isDragging = false;
			}

			if (!isDragging || !button.isPressed)
			{
				return;
			}

			if (!TryGetMouseWorldPosition(out Vector3 currentMouseWorldPosition, false))
			{
				return;
			}

			Vector3 delta = lastMouseWorldPosition - currentMouseWorldPosition;

			if (invertDrag)
			{
				delta = -delta;
			}

			MoveCamera(delta * dragSpeed);

			TryGetMouseWorldPosition(out lastMouseWorldPosition, false);
		}

		private void HandleZoom()
		{
			Mouse mouse = Mouse.current;

			if (mouse == null || !targetCamera.orthographic)
			{
				return;
			}

			float scroll = mouse.scroll.ReadValue().y;

			if (Mathf.Approximately(scroll, 0f))
			{
				return;
			}

			if (!IsPointerInsideScreen() && requirePointerInsideScreenForZoom)
			{
				return;
			}

			Vector3 mouseWorldBeforeZoom = Vector3.zero;

			if (zoomToMouse)
			{
				TryGetMouseWorldPosition(out mouseWorldBeforeZoom, false);
			}

			float normalizedScroll = Mathf.Abs(scroll) >= 10f
				? scroll / 120f
				: scroll;

			float nextSize = targetCamera.orthographicSize /
							 Mathf.Pow(zoomMultiplierPerWheelStep, normalizedScroll);

			targetCamera.orthographicSize = Mathf.Clamp(
				nextSize,
				minOrthographicSize,
				maxOrthographicSize
			);

			if (!zoomToMouse)
			{
				return;
			}

			if (!TryGetMouseWorldPosition(out Vector3 mouseWorldAfterZoom, false))
			{
				return;
			}

			MoveCamera(mouseWorldBeforeZoom - mouseWorldAfterZoom);
		}

		private void HandleReset()
		{
			if (!resetViewWithHome)
			{
				return;
			}

			Keyboard keyboard = Keyboard.current;

			if (keyboard == null || !keyboard.homeKey.wasPressedThisFrame)
			{
				return;
			}

			transform.position = startPosition;

			if (targetCamera != null && targetCamera.orthographic)
			{
				targetCamera.orthographicSize = startOrthographicSize;
			}
		}

		private ButtonControl GetDragButton(Mouse mouse)
		{
			return dragButton switch
			{
				MouseDragButton.Left => mouse.leftButton,
				MouseDragButton.Right => mouse.rightButton,
				MouseDragButton.Middle => mouse.middleButton,
				_ => mouse.middleButton
			};
		}

		private bool TryGetMouseWorldPosition(out Vector3 worldPosition, bool requireInsideScreen)
		{
			worldPosition = Vector3.zero;

			Mouse mouse = Mouse.current;

			if (mouse == null || targetCamera == null)
			{
				return false;
			}

			if (requireInsideScreen && !IsPointerInsideScreen())
			{
				return false;
			}

			Vector2 screenPosition = mouse.position.ReadValue();
			Ray ray = targetCamera.ScreenPointToRay(screenPosition);

			Plane plane = new Plane(
				Vector3.forward,
				new Vector3(0f, 0f, worldPlaneZ)
			);

			if (!plane.Raycast(ray, out float distance))
			{
				return false;
			}

			worldPosition = ray.GetPoint(distance);
			return true;
		}

		private bool IsPointerInsideScreen()
		{
			Mouse mouse = Mouse.current;

			if (mouse == null)
			{
				return false;
			}

			Vector2 position = mouse.position.ReadValue();

			return
				position.x >= 0f &&
				position.y >= 0f &&
				position.x <= Screen.width &&
				position.y <= Screen.height;
		}

		private void MoveCamera(Vector3 delta)
		{
			delta.z = 0f;
			transform.position += delta;
		}

		private void OnValidate()
		{
			if (maxOrthographicSize < minOrthographicSize)
			{
				maxOrthographicSize = minOrthographicSize;
			}

			zoomMultiplierPerWheelStep = Mathf.Max(1.01f, zoomMultiplierPerWheelStep);
		}
	}
}