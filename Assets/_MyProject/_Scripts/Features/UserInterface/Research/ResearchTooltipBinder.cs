using Game.Research;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI.Research
{
	[DisallowMultipleComponent]
	public sealed class ResearchTooltipBinder :
		MonoBehaviour,
		IPointerEnterHandler,
		IPointerMoveHandler,
		IPointerExitHandler
	{
		[SerializeField] private ResearchTooltipView tooltipView;

		private ResearchDefinition definition;
		private ResearchState state;
		private bool hasState;
		private bool isPointerInside;
		private Vector2 lastPointerScreenPosition;
		private Camera lastEventCamera;
		private bool hasPointerPosition;

		private void OnDisable()
		{
			if (isPointerInside)
			{
				tooltipView?.Hide();
			}

			isPointerInside = false;
			hasPointerPosition = false;
		}

		public void Initialize(ResearchDefinition researchDefinition, ResearchTooltipView view)
		{
			definition = researchDefinition;
			tooltipView = view;
		}

		public void ApplyState(ResearchState researchState)
		{
			state = researchState;
			hasState = true;

			if (isPointerInside)
			{
				ShowTooltip(lastPointerScreenPosition, lastEventCamera);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			isPointerInside = true;
			StorePointer(eventData);
			ShowTooltip(lastPointerScreenPosition, lastEventCamera);
		}

		public void OnPointerMove(PointerEventData eventData)
		{
			if (!isPointerInside)
			{
				return;
			}

			StorePointer(eventData);
			tooltipView?.MoveToCursor(lastPointerScreenPosition, lastEventCamera);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			isPointerInside = false;
			hasPointerPosition = false;
			tooltipView?.Hide();
		}

		private void ShowTooltip(Vector2 pointerScreenPosition, Camera eventCamera)
		{
			if (definition == null || tooltipView == null || !hasPointerPosition)
			{
				return;
			}

			ResearchState tooltipState = hasState
				? state
				: ResearchState.CreateInactive(definition.Id, ResearchStatus.Locked);

			tooltipView.Show(definition, tooltipState, pointerScreenPosition, eventCamera);
		}

		private void StorePointer(PointerEventData eventData)
		{
			if (eventData == null)
			{
				return;
			}

			lastPointerScreenPosition = eventData.position;
			lastEventCamera = eventData.enterEventCamera;
			hasPointerPosition = true;
		}
	}
}
