using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.VersionControl.Git
{
    [InitializeOnLoad]
    public class HierarchyWindowInterface
    {
        static HierarchyWindowInterface()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemTryToDrawStatusIcon;
        }

        private static void OnHierarchyItemTryToDrawStatusIcon(int instanceID, Rect selectionRect)
        {
            if (!ApplicationConfiguration.AreHierarchyIconsTurnedOn)
                return;

            string guid;
            GameObject hierarchyGO = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if(!hierarchyGO)
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
                    return;

                guid = AssetDatabase.AssetPathToGUID(scenePath);
            }
            else
            {
                if (hierarchyGO != PrefabUtility.GetNearestPrefabInstanceRoot(hierarchyGO))
                    return;

                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(hierarchyGO);
                if (!prefab)
                    return;

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out guid, out long localId))
                    return;
            }


            Texture2D texture = ProjectWindowInterface.GetStatusIconForAssetGUID(guid);
            if (texture == null)
                return;

            // place the icon to the right of the list:
            Rect r = new Rect(selectionRect);
            r.x = ApplicationConfiguration.AreHierarchyIconsIndented ? r.width + 10 : r.x = r.xMax - 40;
            r.width = 18;

            GUI.Label(r, texture);
        }
    }
}
