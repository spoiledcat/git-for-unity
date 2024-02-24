using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.VersionControl.Git.UI
{
    public static class HierarchyWindowInterface
    {
        private static Dictionary<int, Texture2D> iconCache;
        private static float rightEdge;

        public static void Initialize()
        {
            iconCache = new Dictionary<int, Texture2D>();
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemTryToDrawStatusIcon;
        }

        private static void OnHierarchyItemTryToDrawStatusIcon(int instanceID, Rect selectionRect)
        {
            if (!ApplicationConfiguration.HierarchyIconsEnabled)
                return;

            if (!iconCache.TryGetValue(instanceID, out Texture2D texture))
            {
                string guid = null;
                GameObject hierarchyGO = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (!hierarchyGO)
                {
                    // if no Object has been returned by the InstanceIDToObject() method, then it is possible, that it is a Scene
                    string scenePath = "";
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (scene.GetHashCode() == instanceID)
                        {
                            scenePath = scene.path;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(scenePath))
                    {
                        iconCache.Add(instanceID, null);
                        return;
                    }

                    guid = AssetDatabase.AssetPathToGUID(scenePath);
                    rightEdge = selectionRect.x;
                }
                else
                {
                    if (PrefabUtility.GetNearestPrefabInstanceRoot(hierarchyGO) == hierarchyGO)
                    {
                        GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(hierarchyGO);
                        if (prefab)
                        {
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out guid, out long _);
                        }
                    }
                }

                if (guid != null)
                {
                    texture = ProjectWindowInterface.GetStatusIconForAssetGUID(guid);
                }

                iconCache.Add(instanceID, texture);
            }

            if (texture == null)
                return;

            // place the icon to the right of the list:
            Rect r = new Rect(selectionRect);
            r.width = 18;

            if (ApplicationConfiguration.HierarchyIconsAlignment == ApplicationConfiguration.HierarchyIconAlignment.Right)
            {
                r.x = ApplicationConfiguration.HierarchyIconsIndented ? selectionRect.width + 6 : selectionRect.xMax - 40;
                r.x -= ApplicationConfiguration.HierarchyIconsOffsetRight - 22f;
            }
            else
            {
                r.x = rightEdge - r.width - 22f;
                r.x += ApplicationConfiguration.HierarchyIconsOffsetLeft;
            }

            GUI.Label(r, texture);
        }
    }
}
