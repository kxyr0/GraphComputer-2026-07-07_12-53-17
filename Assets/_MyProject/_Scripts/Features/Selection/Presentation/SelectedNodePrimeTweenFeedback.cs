using Game.Selection.Runtime;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Selection.Presentation
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SelectableNode))]
	public sealed class SelectedNodePrimeTweenFeedback : MonoBehaviour
	{
		[SerializeField] private SelectableNode selectableNode;
		[SerializeField] private Graphic headerGraphic;
		[SerializeField] private Transform scaleTarget;

		[Header("Selected State")]
		[SerializeField, Range(0f, 1f)] private float headerBrightness = 0.18f;
		[SerializeField, Min(1f)] private float selectedScaleMultiplier = 1.035f;

		[Header("Animation")]
		[SerializeField, Min(0.01f)] private float duration = 0.14f;
		[SerializeField] private Ease selectEase = Ease.OutCubic;
		[SerializeField] private Ease deselectEase = Ease.InOutCubic;
		[SerializeField] private bool useUnscaledTime = true;

		private bool hasRestState;
		private Color restHeaderColor;
		private Vector3 restScale;

		private void Reset()
		{
			selectableNode = GetComponent<SelectableNode>();
			scaleTarget = transform;
		}

		private void Awake()
		{
			ResolveReferences();
		}

		private void OnEnable()
		{
			ResolveReferences();

			if (selectableNode != null)
			{
				selectableNode.SelectionChanged += HandleSelectionChanged;
			}
		}

		private void OnDisable()
		{
			if (selectableNode != null)
			{
				selectableNode.SelectionChanged -= HandleSelectionChanged;
			}

			StopTweens();
		}

		private void OnDestroy()
		{
			StopTweens();
		}

		private void HandleSelectionChanged(SelectableNode node, bool selected)
		{
			Animate(selected);
		}

		private void Animate(bool selected)
		{
			CaptureRestStateIfNeeded();
			StopTweens();

			Ease ease = selected ? selectEase : deselectEase;

			if (headerGraphic != null)
			{
				Color targetColor = selected
					? Brighten(restHeaderColor, headerBrightness)
					: restHeaderColor;

				if (Approximately(headerGraphic.color, targetColor))
				{
					headerGraphic.color = targetColor;
				}
				else
				{
					Tween.Color(
						headerGraphic,
						endValue: targetColor,
						duration: duration,
						ease: ease,
						useUnscaledTime: useUnscaledTime);
				}
			}

			if (scaleTarget != null)
			{
				Vector3 targetScale = selected
					? restScale * selectedScaleMultiplier
					: restScale;

				if ((scaleTarget.localScale - targetScale).sqrMagnitude <= 0.000001f)
				{
					scaleTarget.localScale = targetScale;
				}
				else
				{
					Tween.Scale(
						scaleTarget,
						endValue: targetScale,
						duration: duration,
						ease: ease,
						useUnscaledTime: useUnscaledTime);
				}
			}
		}

		private void ResolveReferences()
		{
			if (selectableNode == null)
			{
				selectableNode = GetComponent<SelectableNode>();
			}

			if (scaleTarget == null)
			{
				scaleTarget = transform;
			}
		}

		private void CaptureRestStateIfNeeded()
		{
			if (hasRestState)
			{
				return;
			}

			restHeaderColor = headerGraphic != null
				? headerGraphic.color
				: Color.white;

			restScale = scaleTarget != null
				? scaleTarget.localScale
				: Vector3.one;

			hasRestState = true;
		}

		private void StopTweens()
		{
			if (headerGraphic != null)
			{
				Tween.StopAll(onTarget: headerGraphic);
			}

			if (scaleTarget != null)
			{
				Tween.StopAll(onTarget: scaleTarget);
			}
		}

		private static Color Brighten(Color color, float amount)
		{
			Color result = Color.Lerp(color, Color.white, amount);
			result.a = color.a;
			return result;
		}

		private static bool Approximately(Color left, Color right)
		{
			return Mathf.Approximately(left.r, right.r) &&
				   Mathf.Approximately(left.g, right.g) &&
				   Mathf.Approximately(left.b, right.b) &&
				   Mathf.Approximately(left.a, right.a);
		}
	}
}
