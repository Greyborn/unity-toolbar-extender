using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;

using UnityEngine;
using UnityEngine.Assertions;

namespace UnityToolbarExtender
{
	[InitializeOnLoad]
	public static class ToolbarExtender
	{
		public static readonly List<Action> LeftToolbarGUI = new List<Action>();

		public static readonly List<Action> RightToolbarGUI = new List<Action>();

		static ToolbarExtender()
		{
#if UNITY_2021_1_OR_NEWER
			ToolbarCallback.OnToolbarGUILeft = GUILeft;
			ToolbarCallback.OnToolbarGUIRight = GUIRight;
#else
			ToolbarCallback.OnToolbarGUI = OnGUI;
#endif
		}

		private static void DrawToolbar(List<Action> toolbar)
		{
			GUILayout.BeginHorizontal();
			foreach (var handler in toolbar)
			{
				handler();
			}

			GUILayout.EndHorizontal();
		}

#if UNITY_2021_1_OR_NEWER
		private static void GUILeft()
		{
			DrawToolbar(LeftToolbarGUI);
		}

		private static void GUIRight()
		{
			DrawToolbar(RightToolbarGUI);
		}
#else
		private static void DrawToolbar(Rect rect, List<Action> toolbar)
		{
			if (rect.width <= 0)
			{
				return;
			}

			GUILayout.BeginArea(rect);
			DrawToolbar(toolbar);
			GUILayout.EndArea();
		}

		private static void OnGUI()
		{
			if (LeftToolbarGUI.Count > 0)
			{
				DrawToolbar(Content.LeftRect(), LeftToolbarGUI);
			}

			if (RightToolbarGUI.Count > 0)
			{
				DrawToolbar(Content.RightRect(), RightToolbarGUI);
			}
		}

		private static class Content
		{
			// Controls
			private const int SmButton = 32;
			private const int LgButton = 64;
			private const int SmDropdown = 80;
			private const int LgDropdown = 110;

			// Spaces
	#if UNITY_2019_3_OR_NEWER
			private const int Space = 8;
	#else
			private const int Space = 10;
	#endif

	#if UNITY_2019_1_OR_NEWER
			private const float PlayPauseStopWidth = 140;
	#else
			private const float PlayPauseStopWidth = 100;
	#endif

			private static readonly GUIStyle commandStyle = new GUIStyle("CommandLeft");

			private static readonly float leftControlWidth = CalcLeftControlWidth();

			private static readonly float rightFixedWidth = CalcRightFixedWidth();

			private static int ToolCount
			{
				get
				{
	#if UNITY_2020_1_OR_NEWER
					return 7;
	#else
					const BindingFlags StaticBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

					var type = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
					Assert.IsNotNull(type);

		#if UNITY_2019_1_OR_NEWER
					var field = type.GetField("k_ToolCount", StaticBindingFlags);
					return (int)field.GetValue(null);
		#else
					var field = type.GetField("s_ShownToolIcons", StaticBindingFlags);
					return field != null ? ((Array)field.GetValue(null)).Length : 5;
		#endif
	#endif
				}
			}

			public static Rect LeftRect()
			{
				var screenWidth = EditorGUIUtility.currentViewWidth;

				// Reserve space at the left for internal controls, and then clamp where the play controls begin.
				var rect = new Rect(0, 0, screenWidth, Screen.height);
				rect.xMin += leftControlWidth;
				rect.xMax = CalcCenterControlWidth(screenWidth);
				return Padding(rect);
			}

			public static Rect RightRect()
			{
				var screenWidth = EditorGUIUtility.currentViewWidth;

				// The area begins where the play controls end, and space is reserved at the right for internal controls.
				var rect = new Rect(0, 0, screenWidth, Screen.height);
				rect.xMin = CalcCenterControlWidth(screenWidth);
				rect.xMin += commandStyle.fixedWidth * 3; // Play controls
				rect.xMax -= rightFixedWidth + CalcRightVariableWidth();
				return Padding(rect);
			}

			private static float CalcCenterControlWidth(float screenWidth)
			{
				return Mathf.RoundToInt((screenWidth - PlayPauseStopWidth) / 2);
			}

			private static float CalcLeftControlWidth()
			{
	#if UNITY_2020_1_OR_NEWER
				// Transform, Pivot, and Snap controls
				return Space + (SmButton * ToolCount) + Space + (LgButton * 2) + Space + SmButton;
	#elif UNITY_2019_3_OR_NEWER
				// Transform, Pivot, and Snap controls
				return Space + (SmButton * ToolCount) + Space + (LgButton * 2) + SmButton;
	#else
				// Transform and Pivot controls
				return Space + (SmButton * ToolCount) + (Space + Space) + (LgButton * 2);
	#endif
			}

			private static float CalcRightFixedWidth()
			{
	#if UNITY_2020_1_OR_NEWER
				// Cloud, Account, Layers, Layout
				return SmButton + Space + SmDropdown + Space + SmDropdown + Space + LgDropdown + Space;
	#elif UNITY_2019_2_OR_NEWER
				// Cloud, Account, Layers, Layout
				return SmButton + Space + SmDropdown + Space + SmDropdown + Space + SmDropdown + Space;
	#else
				// Cloud, Account, Layers, Layout
				return SmButton + Space + SmDropdown + (Space + Space) + SmDropdown + Space + SmDropdown + Space;
	#endif
			}

			private static float CalcRightVariableWidth()
			{
				var result = 0f;

	#if UNITY_2018_3_OR_NEWER
				foreach (var subToolbar in Reflect.SubToolbars)
				{
					result += Space + Reflect.SubToolbarWidth(subToolbar);
				}
	#else
				// Colab
				result += 78 + Space;
	#endif

	#if UNITY_2020_1_OR_NEWER
				float width = Reflect.PreviewPackageControlWidth(EditorGUIUtility.currentViewWidth);
				if (width > 0)
				{
					result += Space + width;
				}
	#endif
				return result;
			}

			private static Rect Padding(Rect rect)
			{
	#if UNITY_2019_3_OR_NEWER
				return new Rect(rect.xMin + Space, 4, rect.width - Space - Space, 22);
	#else
				return new Rect(rect.xMin + Space, 5, rect.width - Space - Space, 24);
	#endif
			}
		}

#if UNITY_2018_3_OR_NEWER
		private static class Reflect
		{
			static Reflect()
			{
				const BindingFlags InstanceBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
				const BindingFlags StaticBindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

#if UNITY_2020_1_OR_NEWER
				var typePackageManagerPrefs = Type.GetType(
					"UnityEditor.PackageManager.UI.PackageManagerPrefs, UnityEditor.PackageManagerUIModule");
				fiDismissPreviewPackagesInUse = typePackageManagerPrefs?.GetField(
					"m_DismissPreviewPackagesInUse",
					InstanceBindingFlags);

				var typeUnityMainToolbar =
					Type.GetType("UnityEditor.UnityMainToolbar, UnityEditor.UIServiceModule");
				Assert.IsNotNull(typeUnityMainToolbar);

				fiIsPreviewPackagesInUse = typeUnityMainToolbar.GetField(
					"m_IsPreviewPackagesInUse",
					InstanceBindingFlags);
				fiPackageManagerPrefs = typeUnityMainToolbar.GetField(
					"m_PackageManagerPrefs",
					InstanceBindingFlags);

				fiSubToolbars = typeUnityMainToolbar.GetField("s_SubToolbars", StaticBindingFlags);
#else
				var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
				fiSubToolbars = toolbarType.GetField("s_SubToolbars", StaticBindingFlags);
#endif

				var typeSubToolbar = typeof(Editor).Assembly.GetType("UnityEditor.SubToolbar");
				piSubToolbarWidth = typeSubToolbar.GetProperty("Width");
			}

			private static readonly FieldInfo fiSubToolbars;

			private static readonly PropertyInfo piSubToolbarWidth;

			/// <summary>
			/// Get the <see cref="IList"/> interface to a <see cref="List{T}"/> where T is a
			/// <see cref="UnityEditor.SubToolbar"/> (reflected type).
			/// </summary>
			public static IList SubToolbars => (IList)fiSubToolbars.GetValue(null);

			public static float SubToolbarWidth(object subToolbar)
			{
				return (float)piSubToolbarWidth.GetValue(subToolbar);
			}

		#if UNITY_2020_1_OR_NEWER
			private static readonly FieldInfo fiIsPreviewPackagesInUse;

			private static readonly FieldInfo fiPackageManagerPrefs;

			private static readonly FieldInfo fiDismissPreviewPackagesInUse;

			private static ScriptableObject unityMainToolbar;

			public static int PreviewPackageControlWidth(float toolbarWidth)
			{
				return !IsPreviewPackagesInUse || DismissPreviewPackageInUse ? 0 : toolbarWidth < 1100 ? 45 : 173;
			}

			private static bool IsPreviewPackagesInUse =>
				(bool)fiIsPreviewPackagesInUse.GetValue(UnityMainToolbar);

			private static bool DismissPreviewPackageInUse =>
				(bool)fiDismissPreviewPackagesInUse.GetValue(fiPackageManagerPrefs.GetValue(UnityMainToolbar));

			private static ScriptableObject UnityMainToolbar
			{
				get
				{
					if (unityMainToolbar == null)
					{
						unityMainToolbar = Resources.FindObjectsOfTypeAll<ScriptableObject>().FirstOrDefault(
							scriptableObject => scriptableObject.GetType().Name == "UnityMainToolbar");
					}

					return unityMainToolbar;
				}
			}
		#endif
		}
	#endif
#endif
	}
}
