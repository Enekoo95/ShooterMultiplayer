// Assets/Editor/FixCanvasScaler.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class FixCanvasScaler
{
    [MenuItem("Tools/Fix All Canvas Scalers")]
    static void FixAll()
    {
        // Busca TODOS los CanvasScaler en la escena, incluso los inactivos
        CanvasScaler[] scalers = Resources.FindObjectsOfTypeAll<CanvasScaler>();
        int count = 0;

        foreach (CanvasScaler scaler in scalers)
        {
            // Solo los que estén en la escena (no prefabs en memoria)
            if (scaler.gameObject.scene.name == null) continue;

            Undo.RecordObject(scaler, "Fix Canvas Scaler");

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            EditorUtility.SetDirty(scaler);
            count++;
        }

        Debug.Log($"[FixCanvasScaler] {count} Canvas Scalers corregidos.");
    }
}