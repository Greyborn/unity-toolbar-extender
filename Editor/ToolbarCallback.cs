using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace UnityToolbarExtender
{
	public static class ToolbarCallback
	{
		static readonly Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
		static ScriptableObject m_currentToolbar;

#if UNITY_2021_1_OR_NEWER
		/// <summary>
		/// Callback for left toolbar OnGUI method.
		/// </summary>
		public static Action OnToolbarGUILeft;

		/// <summary>
		/// Callback for right toolbar OnGUI method.
		/// </summary>
		public static Action OnToolbarGUIRight;
#else
		private const BindingFlags InstanceBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		static readonly Type m_guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");
		static readonly FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler", InstanceBindingFlags);

		/// <summary>
		/// Callback for toolbar OnGUI method.
		/// </summary>
		public static Action OnToolbarGUI;

	#if UNITY_2020_1_OR_NEWER
		static readonly Type m_iWindowBackendType = typeof(Editor).Assembly.GetType("UnityEditor.IWindowBackend");
		static readonly PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend", InstanceBindingFlags);
		static readonly PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree", InstanceBindingFlags);
	#else
		static readonly PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree", InstanceBindingFlags);
	#endif
#endif

		static ToolbarCallback()
		{
			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;
		}

		static void OnUpdate()
		{
			// Relying on the fact that toolbar is ScriptableObject and gets deleted when layout changes
			if (m_currentToolbar != null)
			{
				// Toolbar reference already exists, so it must already be configured.
				return;
			}

			// Find toolbar
			var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
			m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
			if (m_currentToolbar == null)
			{
				// Toolbar was not found, so just abort.
				return;
			}

#if UNITY_2021_1_OR_NEWER
			var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
			var rawRoot = root.GetValue(m_currentToolbar);
			var mRoot = rawRoot as VisualElement;
			RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
			RegisterCallback("ToolbarZoneRightAlign", OnToolbarGUIRight);

			void RegisterCallback(string root, Action cb)
			{
				var toolbarZone = mRoot.Q(root);
				var parent = new VisualElement
					{
						style =
							{
								flexGrow = 1,
								flexDirection = FlexDirection.Row,
							},
					};
				var container = new IMGUIContainer();
				container.style.flexGrow = 1;
				container.onGUIHandler += () => { cb?.Invoke(); };
				parent.Add(container);
				toolbarZone.Add(parent);
			}
#else
	#if UNITY_2020_1_OR_NEWER
			var windowBackend = m_windowBackend.GetValue(m_currentToolbar);
			var visualTree = (VisualElement) m_viewVisualTree.GetValue(windowBackend, null);
	#else
			var visualTree = (VisualElement) m_viewVisualTree.GetValue(m_currentToolbar, null);
	#endif

			// Get first child which 'happens' to be toolbar IMGUIContainer
			var container = (IMGUIContainer) visualTree[0];

			// (Re)attach handler
			var handler = (Action) m_imguiContainerOnGui.GetValue(container);
			handler -= OnGUI;
			handler += OnGUI;
			m_imguiContainerOnGui.SetValue(container, handler);

			void OnGUI()
			{
				OnToolbarGUI?.Invoke();
			}
#endif
		}
	}
}
