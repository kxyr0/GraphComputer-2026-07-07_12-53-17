using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	public static class RuntimeTextRefresh
	{
		public static void SetText(TMP_Text text, string value)
		{
			if (text == null)
			{
				return;
			}

			text.text = value ?? string.Empty;
			Refresh(text);
		}

		public static void SetText(Text text, string value)
		{
			if (text == null)
			{
				return;
			}

			text.text = value ?? string.Empty;
			Refresh(text);
		}

		public static void SetTextWithoutNotify(TMP_InputField inputField, string value)
		{
			if (inputField == null)
			{
				return;
			}

			inputField.SetTextWithoutNotify(value ?? string.Empty);
			inputField.ForceLabelUpdate();
			Refresh(inputField.textComponent);
		}

		public static void SetButtonText(ButtonManager buttonManager, string value)
		{
			if (buttonManager == null)
			{
				return;
			}

			buttonManager.SetText(value ?? string.Empty);
			RefreshButtonText(buttonManager);
		}

		public static void SetColor(TMP_Text text, Color color)
		{
			if (text == null)
			{
				return;
			}

			text.color = color;
			Refresh(text);
		}

		public static void SetColor(Text text, Color color)
		{
			if (text == null)
			{
				return;
			}

			text.color = color;
			Refresh(text);
		}

		public static void RefreshButtonText(ButtonManager buttonManager)
		{
			if (buttonManager == null)
			{
				return;
			}

			Refresh(buttonManager.normalText);
			Refresh(buttonManager.highlightedText);
			Refresh(buttonManager.disabledText);

			if (buttonManager.transform is RectTransform rectTransform)
			{
				LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
				LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
			}
		}

		public static void Refresh(TMP_Text text)
		{
			if (text == null)
			{
				return;
			}

			text.SetLayoutDirty();
			text.SetVerticesDirty();
			text.SetMaterialDirty();
			text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);

			if (text.rectTransform != null)
			{
				LayoutRebuilder.MarkLayoutForRebuild(text.rectTransform);
			}
		}

		public static void Refresh(Text text)
		{
			if (text == null)
			{
				return;
			}

			text.SetLayoutDirty();
			text.SetVerticesDirty();
			text.SetMaterialDirty();

			if (text.rectTransform != null)
			{
				LayoutRebuilder.MarkLayoutForRebuild(text.rectTransform);
			}
		}
	}
}
