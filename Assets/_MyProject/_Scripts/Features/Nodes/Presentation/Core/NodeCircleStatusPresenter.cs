using UnityEngine;
using UnityEngine.UI;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeCircleStatusPresenter
	{
		private readonly Image statusImage;
		private readonly Color healthyColor;
		private readonly Color limitColor;
		private readonly Color errorColor;

		public NodeCircleStatusPresenter(
			Image statusImage,
			Color healthyColor,
			Color limitColor,
			Color errorColor)
		{
			this.statusImage = statusImage;
			this.healthyColor = healthyColor;
			this.limitColor = limitColor;
			this.errorColor = errorColor;
		}

		public void SetStatus(NodeCircleStatus status)
		{
			if (statusImage == null)
			{
				return;
			}

			statusImage.enabled = true;
			statusImage.color = status switch
			{
				NodeCircleStatus.Error => errorColor,
				NodeCircleStatus.Limit => limitColor,
				_ => healthyColor
			};
		}
	}
}
