using UnityEditor;
using UnityEngine;

namespace Game.Nodes.Definitions
{
	[CustomEditor(typeof(NodeJsonDefinitionAsset))]
	public sealed class NodeJsonDefinitionAssetEditor : Editor
	{
		private SerializedProperty jsonProperty;

		private void OnEnable()
		{
			jsonProperty = serializedObject.FindProperty("json");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if (jsonProperty == null)
			{
				EditorGUILayout.HelpBox("Serialized field 'json' not found.", MessageType.Error);
				serializedObject.ApplyModifiedProperties();
				return;
			}

			EditorGUILayout.PropertyField(jsonProperty, new GUIContent("Json"), true);

			EditorGUILayout.Space(6f);

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Copy"))
				{
					GUIUtility.systemCopyBuffer = jsonProperty.stringValue ?? string.Empty;
				}

				if (GUILayout.Button("Paste"))
				{
					Undo.RecordObject(target, "Paste Node JSON");

					jsonProperty.stringValue = GUIUtility.systemCopyBuffer ?? string.Empty;

					serializedObject.ApplyModifiedProperties();
					EditorUtility.SetDirty(target);
					GUI.FocusControl(null);
					return;
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}