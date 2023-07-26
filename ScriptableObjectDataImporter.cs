using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System;
using System.Linq;

public class ScriptableObjectDataImporter : EditorWindow
{
	private TextAsset csvFile;
	private ScriptableObject[] data;

	private Vector2 scrollPosition = Vector2.zero;

	[MenuItem("Data/Import Scriptable Data")]
	public static void ShowWindow()
	{
		GetWindow<ScriptableObjectDataImporter>();
	}

	private void OnGUI()
	{
		GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
		labelStyle.fontSize = 20;
		labelStyle.alignment = TextAnchor.MiddleCenter;
		EditorGUILayout.LabelField("Import Scriptable Data", labelStyle);
		EditorGUILayout.LabelField("1.ScriptableObject�� ���� �����͸� ���� csv ������ �巡�׾� ���");
		EditorGUILayout.LabelField("2.ScriptableObject�� ����ִ� ������ ǥ�õ� ���� �巡�׾ص��");
		EditorGUILayout.LabelField("3.ImportButton�� �����ÿ�");
		EditorGUILayout.LabelField("���ǻ��� : Column ��� ���� �̸��� ���� ���ƾ���");
		EditorGUILayout.Space(10);

		csvFile = EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false) as TextAsset;

		if (data == null)
		{
			data = new ScriptableObject[0];
		}

		int arraySize = EditorGUILayout.IntField("Scriptable Data Array Size", data.Length);

		if (arraySize != data.Length)
		{
			Array.Resize(ref data, arraySize);
		}

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		for (int i = 0; i < arraySize; i++)
		{
			data[i] = EditorGUILayout.ObjectField($"Scriptable Data {i}", data[i], typeof(ScriptableObject), false) as ScriptableObject;
		}

		GUILayout.Space(10);
		EditorGUILayout.EndScrollView();
		//Editor GUI BOX
		Event evt = Event.current;
		Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
		GUIStyle style = new GUIStyle(EditorStyles.helpBox);
		style.alignment = TextAnchor.MiddleCenter;
		GUI.Box(dropArea, "�١ڡ١ں������١ڡ١� \n ���ٰ� ������ �巡���Ͻʼ�", style);

		switch (evt.type)
		{
			case EventType.DragUpdated:
			case EventType.DragPerform:
				if (!dropArea.Contains(evt.mousePosition))
				{
					break;
				}

				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				if (evt.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();

					foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
					{
						if (obj is DefaultAsset)
						{
							string path = AssetDatabase.GetAssetPath(obj);
							// t: <- AssetDatabase.FindAssets���� ���� ���͸� ����.. �̰� �Ⱦ��� ScriptableObject�� �ҷ��� ���� ���� �ٸ��͵� �� �ҷ���..
							string[] scriptable = AssetDatabase.FindAssets("t:ScriptableObject", new[] { path });

							for (int i = 0; i < scriptable.Length; i++)
							{
								string assetPath = AssetDatabase.GUIDToAssetPath(scriptable[i]);
								ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

								if (!data.Contains(asset))
								{
									Array.Resize(ref data, data.Length + 1);
									data[data.Length - 1] = asset;
								}
							}
						}
					}
				}

				break;
		}

		GUILayout.FlexibleSpace();

		GUILayout.BeginHorizontal();

		GUILayout.FlexibleSpace();

		if (GUILayout.Button("Import"))
		{
			ImportData();
		}

		GUILayout.FlexibleSpace();

		GUILayout.EndHorizontal();
	}

	private void ImportData()
	{
		string[] lines = csvFile.text.Split('\n');
		string[] header = lines[0].Split(',');

		for (int k = 0; k < data.Length; k++)
		{
			Type dataType = data[k].GetType();
			FieldInfo[] fields = dataType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			SerializedObject serializedData = new SerializedObject(data[k]);

			string[] values = lines[k + 1].Split(',');

			for (int j = 0; j < header.Length; j++)
			{
				string fieldName = header[j].Trim();
				string value = values[j].Trim();

				foreach (FieldInfo field in fields)
				{
					if (!field.FieldType.IsEnum)
					{
						if (field.Name.Equals(fieldName))
						{
							object convertedValue = Convert.ChangeType(value, field.FieldType);
							field.SetValue(data[k], convertedValue);

							SerializedProperty property = serializedData.FindProperty(field.Name);

							SetPropertyValue(property, convertedValue);

							break;
						}
					}
					else
					{
						if (field.Name.Equals(fieldName))
						{
							object convertedValue = Enum.Parse(field.FieldType, (string)value);
							field.SetValue(data[k], convertedValue);

							SerializedProperty property = serializedData.FindProperty(field.Name);

							SetPropertyValue(property, convertedValue);

							break;
						}
					}
					
				}
			}

			// ��������� ��ũ�� �����ϴ� �Լ�
			// �̰��� ������ ItemData�� ���������� ������ ų�� �ʱ�ȭ ��
			//serializedData.ApplyModifiedProperties();
		}
	}

	private void SetPropertyValue(SerializedProperty property, object value)
	{
		switch (property.propertyType)
		{
			case SerializedPropertyType.Integer:
				property.intValue = (int)value;
				break;
			case SerializedPropertyType.Float:
				property.floatValue = (float)value;
				break;
			case SerializedPropertyType.Boolean:
				property.boolValue = (bool)value;
				break;
			case SerializedPropertyType.String:
				property.stringValue = (string)value;
				break;
			case SerializedPropertyType.Enum:
				property.enumValueIndex = (int)value;
				//property.enumValueIndex = (int)Enum.Parse(property.enumType, (string)value);
				break;
			case SerializedPropertyType.Vector2:
				property.vector2Value = (Vector2)value;
				break;
			case SerializedPropertyType.Vector3:
				property.vector3Value = (Vector3)value;
				break;
			case SerializedPropertyType.Vector4:
				property.vector4Value = (Vector4)value;
				break;
			case SerializedPropertyType.Color:
				property.colorValue = (Color)value;
				break;
			case SerializedPropertyType.ObjectReference:
				property.objectReferenceValue = (UnityEngine.Object)value;
				break;
			default:
				Debug.LogError("Unsupported property type: " + property.propertyType);
				break;
		}
	}

}
