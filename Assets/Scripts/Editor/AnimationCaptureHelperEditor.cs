using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

// Custom editor for the AnimationCaptureHelper.
[CustomEditor(typeof(AnimationCaptureHelper))]
public class AnimationCaptureHelperEditor : Editor
{
    // The current capture routine in progress.
    private IEnumerator _currentCaptureRoutine;

    // Draws the custom inspector for the capture helper.
    public override void OnInspectorGUI(){

        using (new EditorGUI.DisabledScope()){

            var helper = (AnimationCaptureHelper)target;
            var targetProp = serializedObject.FindProperty("_target");
            var sourceClipProp = serializedObject.FindProperty("_sourceClip");
            var cantPixelsClean = serializedObject.FindProperty("_cantPixelsClean");

            EditorGUILayout.PropertyField(targetProp);
            EditorGUILayout.PropertyField(sourceClipProp);
            EditorGUILayout.PropertyField(cantPixelsClean);

            var sourceClip = (AnimationClip)sourceClipProp.objectReferenceValue;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)){

                EditorGUILayout.LabelField("Frames Options", EditorStyles.boldLabel);

                var fpsProp = serializedObject.FindProperty("_framesPerSecond");
                EditorGUILayout.PropertyField(fpsProp);

            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Capture Options", EditorStyles.boldLabel);

                var captureCameraProp = serializedObject.FindProperty("_captureCamera");
                EditorGUILayout.ObjectField(captureCameraProp, typeof(Camera));

                var resolutionProp = serializedObject.FindProperty("_cellSize");
                EditorGUILayout.PropertyField(resolutionProp);

                if (GUILayout.Button("Capture"))
                {
                    RunRoutine(helper.CaptureAnimation(SaveCapture));
                }
                                    
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    // Starts running the editor routine.
    private void RunRoutine(IEnumerator routine)
    {
        _currentCaptureRoutine = routine;
        EditorApplication.update += UpdateRoutine;
        
    }

    // Calls MoveNext on the routine each editor frame until the iterator terminates.
    private void UpdateRoutine(){
        
        if (!_currentCaptureRoutine.MoveNext())
        {
            EditorApplication.update -= UpdateRoutine;
        }
    }

    // Saves the captured animation sprite atlases to disk.
    private void SaveCapture(Texture2D diffuseMap, Texture2D normalMap)
    {
        
        var diffusePath = EditorUtility.SaveFilePanel("Save Capture", "", "NewCapture", "png");

        if (string.IsNullOrEmpty(diffusePath))
        {
            return;
        }

        var fileName = Path.GetFileNameWithoutExtension(diffusePath);
        var directory = Path.GetDirectoryName(diffusePath);
        var normalPath = string.Format("{0}/{1}{2}.{3}", directory, fileName, "NormalMap", "png");

        File.WriteAllBytes(diffusePath, diffuseMap.EncodeToPNG());
        File.WriteAllBytes(normalPath, normalMap.EncodeToPNG());

        AssetDatabase.Refresh();
    }
}
