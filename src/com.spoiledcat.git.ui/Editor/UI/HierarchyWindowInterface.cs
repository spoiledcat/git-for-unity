using UnityEditor;
using UnityEngine;

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
            GameObject hierarchyGO = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (!hierarchyGO || hierarchyGO != PrefabUtility.GetNearestPrefabInstanceRoot(hierarchyGO))
                return;

            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(hierarchyGO);
            if (!prefab)
                return;

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long localId))
                return;

            Texture2D texture = ProjectWindowInterface.GetStatusIconForAssetGUID(guid);
            if (texture == null)
                return;

            // place the icon to the right of the list:
            Rect r = new Rect(selectionRect);
            r.x = r.width + 10;
            r.width = 18;

            GUI.Label(r, texture);
        }
    }
}
