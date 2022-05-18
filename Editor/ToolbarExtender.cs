using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityToolbarExtender
{
	[InitializeOnLoad]
	public static class ToolbarExtender
	{
		static int m_toolCount;
		static GUIStyle m_commandStyle = null;

		public static readonly List<Action> LeftToolbarGUI = new List<Action>();
		public static readonly List<Action> RightToolbarGUI = new List<Action>();

		static ToolbarExtender()
		{
			const BindingFlags BindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

			Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");

#if UNITY_2019_1_OR_NEWER
			string fieldName = "k_ToolCount";
#else
			string fieldName = "s_ShownToolIcons";
#endif

			FieldInfo toolIcons = toolbarType.GetField(fieldName, BindingFlags);

#if UNITY_2019_3_OR_NEWER
			m_toolCount = toolIcons != null ? ((int) toolIcons.GetValue(null)) : 8;
#elif UNITY_2019_1_OR_NEWER
			m_toolCount = toolIcons != null ? ((int) toolIcons.GetValue(null)) : 7;
#elif UNITY_2018_1_OR_NEWER
			m_toolCount = toolIcons != null ? ((Array) toolIcons.GetValue(null)).Length : 6;
#else
			m_toolCount = toolIcons != null ? ((Array) toolIcons.GetValue(null)).Length : 5;
#endif

#if UNITY_2018_3_OR_NEWER
			fiSubToolbars = toolbarType.GetField("s_SubToolbars", BindingFlags);
			Assert.IsNotNull(fiSubToolbars);

			var typeSubToolbar = typeof(Editor).Assembly.GetType("UnityEditor.SubToolbar");
			piSubToolbarWidth = typeSubToolbar.GetProperty("Width");
#endif

			ToolbarCallback.OnToolbarGUI = OnGUI;
			ToolbarCallback.OnToolbarGUILeft = GUILeft;
			ToolbarCallback.OnToolbarGUIRight = GUIRight;
		}

#if UNITY_2019_3_OR_NEWER
		public const float space = 8;
#else
		public const float space = 10;
#endif
		public const float largeSpace = 20;
		public const float buttonWidth = 32;
		public const float dropdownWidth = 80;
#if UNITY_2019_1_OR_NEWER
		public const float playPauseStopWidth = 140;
#else
		public const float playPauseStopWidth = 100;
#endif

#if UNITY_2018_3_OR_NEWER
		/// <summary>
		/// <see cref="List{T}"/> where T is <see cref="UnityEditor.SubToolbar"/>
		/// </summary>
		private static readonly FieldInfo fiSubToolbars;

		/// <summary>
		/// float
		/// </summary>
		private static readonly PropertyInfo piSubToolbarWidth;

		/// <summary>
		/// Get the <see cref="IList"/> interface to a <see cref="List{T}"/> where T is a
		/// <see cref="UnityEditor.SubToolbar"/> (reflected type).
		/// </summary>
		private static IList SubToolbars => (IList)fiSubToolbars.GetValue(null);

		private static float SubToolbarWidth(object subToolbar)
		{
			return (float)piSubToolbarWidth.GetValue(subToolbar);
		}
#endif

		public static void GUILeft()
		{
			DrawToolbar(LeftToolbarGUI);
		}

		public static void GUIRight()
		{
			DrawToolbar(RightToolbarGUI);
		}

		static void OnGUI()
		{
			// Create two containers, left and right
			// Screen is whole toolbar

			if (m_commandStyle == null)
			{
				m_commandStyle = new GUIStyle("CommandLeft");
			}

			var screenWidth = EditorGUIUtility.currentViewWidth;

			// Following calculations match code reflected from Toolbar.OldOnGUI()
			float playButtonsPosition = Mathf.RoundToInt ((screenWidth - playPauseStopWidth) / 2);

			// Reserve space at the left for internal controls,
			// and then clamp where the play controls begin.
			Rect leftRect = new Rect(0, 0, screenWidth, Screen.height);
			leftRect.xMin += space;
			leftRect.xMin += buttonWidth * m_toolCount; // Transform controls
#if UNITY_2019_3_OR_NEWER
			leftRect.xMin += space;
#else
			leftRect.xMin += largeSpace;
#endif
			leftRect.xMin += 64 * 2; // Pivot controls
#if UNITY_2019_3_OR_NEWER
			leftRect.xMin += buttonWidth; // Snap control
#endif
			leftRect.xMax = playButtonsPosition;

			// The area begins where the play controls end, and space
			// is reserved at the right for internal controls.
			Rect rightRect = new Rect(0, 0, screenWidth, Screen.height);
			rightRect.xMin = playButtonsPosition;
			rightRect.xMin += m_commandStyle.fixedWidth * 3; // Play controls
			rightRect.xMax = screenWidth;
			rightRect.xMax -= space;
			rightRect.xMax -= dropdownWidth; // Layout
			rightRect.xMax -= space;
			rightRect.xMax -= dropdownWidth; // Layers
#if UNITY_2019_2_OR_NEWER
			rightRect.xMax -= space;
#else
			rightRect.xMax -= largeSpace;
#endif
			rightRect.xMax -= dropdownWidth; // Account
			rightRect.xMax -= space;
			rightRect.xMax -= buttonWidth; // Cloud
#if UNITY_2018_3_OR_NEWER
			foreach (var subToolbar in SubToolbars)
			{
				rightRect.xMax -= space;
				rightRect.xMax -= SubToolbarWidth(subToolbar);
			}
#else
			rightRect.xMax -= space;
			rightRect.xMax -= 78; // Colab
#endif

			// Add spacing around existing controls
			leftRect.xMin += space;
			leftRect.xMax -= space;
			rightRect.xMin += space;
			rightRect.xMax -= space;

			// Add top and bottom margins
#if UNITY_2019_3_OR_NEWER
			leftRect.y = 4;
			leftRect.height = 22;
			rightRect.y = 4;
			rightRect.height = 22;
#else
			leftRect.y = 5;
			leftRect.height = 24;
			rightRect.y = 5;
			rightRect.height = 24;
#endif

			DrawToolbar(leftRect, LeftToolbarGUI);
			DrawToolbar(rightRect, RightToolbarGUI);
		}

		static void DrawToolbar(List<Action> toolbar)
		{
			GUILayout.BeginHorizontal();
			foreach (var handler in toolbar)
			{
				handler();
			}

			GUILayout.EndHorizontal();
		}

		static void DrawToolbar(Rect rect, List<Action> toolbar)
		{
			if (rect.width <= 0)
			{
				return;
			}

			GUILayout.BeginArea(rect);
			DrawToolbar(toolbar);
			GUILayout.EndArea();
		}
	}
}
