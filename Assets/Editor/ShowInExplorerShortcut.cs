using UnityEditor;
using UnityEngine;
using System.IO;

public class ShowInExplorerShortcut
{
    [MenuItem("Assets/Show In Explorer %#d")]  
    private static void ShowInExplorer()
    {
        var obj = Selection.activeObject;
        if (obj == null) return;

        var path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return;

        var fullPath = Path.GetFullPath(path);
        EditorUtility.RevealInFinder(fullPath);
    }
}
