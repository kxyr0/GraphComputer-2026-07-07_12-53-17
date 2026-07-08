using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.UI.Input
{
	public static class UiPointerBlockUtility
	{
		private static readonly List<RaycastResult> RaycastResults = new();

		public static bool IsPointerOverBlockingUi()
		{
			if (EventSystem.current == null || Mouse.current == null)
			{
				return false;
			}

			Vector2 mousePosition = Mouse.current.position.ReadValue();

			PointerEventData eventData = new PointerEventData(EventSystem.current)
			{
				position = mousePosition
			};

			RaycastResults.Clear();
			EventSystem.current.RaycastAll(eventData, RaycastResults);

			foreach (RaycastResult result in RaycastResults)
			{
				if (result.gameObject == null)
				{
					continue;
				}

				if (result.gameObject.GetComponentInParent<BlocksCameraInput>() != null)
				{
					return true;
				}
			}

			return false;
		}
	}
}