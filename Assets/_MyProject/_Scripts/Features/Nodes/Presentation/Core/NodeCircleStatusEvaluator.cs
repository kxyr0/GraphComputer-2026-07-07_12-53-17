using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Game.Nodes.Presentation
{
	internal sealed class NodeCircleStatusEvaluator
	{
		private static readonly Regex NumberRegex =
			new Regex(@"[-+]?\d+([.,]\d+)?", RegexOptions.Compiled);

		private readonly Dictionary<string, NodeCircleStatus> statusesByLabel =
			new Dictionary<string, NodeCircleStatus>(StringComparer.OrdinalIgnoreCase);

		public NodeCircleStatus Current { get; private set; } = NodeCircleStatus.Healthy;

		public void Reset()
		{
			statusesByLabel.Clear();
			Current = NodeCircleStatus.Healthy;
		}

		public NodeCircleStatus RecordStat(string label, string value, string stateHint)
		{
			if (string.IsNullOrWhiteSpace(label))
			{
				return Current;
			}

			statusesByLabel[label.Trim()] = Evaluate(label, value, stateHint);
			Current = GetWorstStatus();
			return Current;
		}

		private static NodeCircleStatus Evaluate(string label, string value, string stateHint)
		{
			string normalizedLabel = Normalize(label);
			string normalizedValue = Normalize(value);
			string normalizedHint = Normalize(stateHint);

			NodeCircleStatus hintedStatus = EvaluateText(normalizedHint);

			if (hintedStatus != NodeCircleStatus.Healthy)
			{
				return hintedStatus;
			}

			NodeCircleStatus textStatus = EvaluateText(normalizedValue);

			if (textStatus != NodeCircleStatus.Healthy)
			{
				return textStatus;
			}

			if (!TryReadNumber(value, out float number))
			{
				return NodeCircleStatus.Healthy;
			}

			if (IsPositiveMetric(normalizedLabel))
			{
				return EvaluatePositiveMetric(number);
			}

			if (IsNegativeMetric(normalizedLabel))
			{
				return EvaluateNegativeMetric(number);
			}

			if (normalizedLabel.Contains("delay") || normalizedLabel.Contains("response"))
			{
				return EvaluateTimeMetric(number);
			}

			if (normalizedLabel.Contains("critical") ||
				normalizedLabel.Contains("vulnerab") ||
				normalizedLabel.Contains("falseblock"))
			{
				return EvaluateCountMetric(number);
			}

			return NodeCircleStatus.Healthy;
		}

		private static NodeCircleStatus EvaluateText(string normalizedText)
		{
			if (string.IsNullOrWhiteSpace(normalizedText))
			{
				return NodeCircleStatus.Healthy;
			}

			if (normalizedText.Contains("missing") ||
				normalizedText.Contains("offline") ||
				normalizedText.Contains("noservice") ||
				normalizedText.Contains("failed") ||
				normalizedText.Contains("error") ||
				normalizedText.Contains("obsolete"))
			{
				return NodeCircleStatus.Error;
			}

			if (normalizedText.Contains("aging") ||
				normalizedText.Contains("warning") ||
				normalizedText.Contains("degraded") ||
				normalizedText.Contains("unstable") ||
				normalizedText.Contains("slow") ||
				normalizedText.Contains("highload"))
			{
				return NodeCircleStatus.Limit;
			}

			return NodeCircleStatus.Healthy;
		}

		private static NodeCircleStatus EvaluatePositiveMetric(float value)
		{
			if (value <= 0f || value < 35f)
			{
				return NodeCircleStatus.Error;
			}

			return value < 60f ? NodeCircleStatus.Limit : NodeCircleStatus.Healthy;
		}

		private static NodeCircleStatus EvaluateNegativeMetric(float value)
		{
			if (value >= 90f)
			{
				return NodeCircleStatus.Error;
			}

			return value >= 70f ? NodeCircleStatus.Limit : NodeCircleStatus.Healthy;
		}

		private static NodeCircleStatus EvaluateTimeMetric(float value)
		{
			if (value >= 350f)
			{
				return NodeCircleStatus.Error;
			}

			return value >= 120f ? NodeCircleStatus.Limit : NodeCircleStatus.Healthy;
		}

		private static NodeCircleStatus EvaluateCountMetric(float value)
		{
			if (value >= 5f)
			{
				return NodeCircleStatus.Error;
			}

			return value > 0f ? NodeCircleStatus.Limit : NodeCircleStatus.Healthy;
		}

		private static bool IsPositiveMetric(string normalizedLabel)
		{
			return normalizedLabel.Contains("stability") ||
				   normalizedLabel.Contains("safety") ||
				   normalizedLabel.Contains("support") ||
				   normalizedLabel.Contains("usability") ||
				   normalizedLabel.Contains("encryption");
		}

		private static bool IsNegativeMetric(string normalizedLabel)
		{
			return normalizedLabel.Contains("load") ||
				   normalizedLabel.Contains("usage") ||
				   normalizedLabel.Contains("risk") ||
				   normalizedLabel.Contains("threat") ||
				   normalizedLabel.Contains("fragmentation") ||
				   normalizedLabel.Contains("gpu");
		}

		private static bool TryReadNumber(string value, out float number)
		{
			number = 0f;

			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			Match match = NumberRegex.Match(value);

			if (!match.Success)
			{
				return false;
			}

			string normalizedNumber = match.Value.Replace(',', '.');
			return float.TryParse(
				normalizedNumber,
				NumberStyles.Float,
				CultureInfo.InvariantCulture,
				out number);
		}

		private NodeCircleStatus GetWorstStatus()
		{
			NodeCircleStatus result = NodeCircleStatus.Healthy;

			foreach (NodeCircleStatus status in statusesByLabel.Values)
			{
				if (status > result)
				{
					result = status;
				}
			}

			return result;
		}

		private static string Normalize(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			return value
				.Trim()
				.Replace(" ", string.Empty)
				.Replace("_", string.Empty)
				.Replace("-", string.Empty)
				.ToLowerInvariant();
		}
	}
}
