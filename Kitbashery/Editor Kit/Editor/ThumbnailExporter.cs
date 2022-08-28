using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
 MIT License

Copyright (c) 2022 Kitbashery

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.



Need support or additional features? Please visit https://kitbashery.com/
*/

public class ThumbnailExporter : EditorWindow
{
    GameObject previewObject;
    GameObject objectPreviewed;
    Texture previewTexture;
    Editor previewEditor;
    int resolution = 512;
    Color grey = new Color(0.082f, 0.082f, 0.082f, 1.000f);

    [MenuItem("Tools/Kitbashery/Thumbnail Exporter")]
    static void ShowBrushMakerWindow()
    {
        ThumbnailExporter window = (ThumbnailExporter)GetWindow(typeof(ThumbnailExporter));
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Select an object:");
        previewObject = (GameObject)EditorGUILayout.ObjectField(previewObject, typeof(GameObject), true);
        EditorGUILayout.BeginVertical();
        GUILayout.Box("", GUIStyle.none, GUILayout.Width(resolution), GUILayout.Height(resolution));
        Rect r = GUILayoutUtility.GetLastRect();
        DrawEditorPreview(previewObject, r);
        EditorGUILayout.EndVertical();
    }

    public void DrawEditorPreview(GameObject preview, Rect r)
    {
        if(previewObject != null && previewObject != objectPreviewed)
        {
            previewEditor = null;
            //TODO: Does the editor need to be destroyed or closed somehow?
        }

        if (previewEditor == null)
        {
            previewEditor = Editor.CreateEditor(preview);
            objectPreviewed = previewObject;
        }
        else
        {
            previewEditor.OnInteractivePreviewGUI(r, GUI.skin.box);
        }

        if (GUILayout.Button("Save Thumbnail", GUILayout.Width(resolution)))
        {
            Dictionary<int, Texture> previewCache = typeof(Editor).Assembly.GetType("UnityEditor.GameObjectInspector").GetField("m_PreviewCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(previewEditor) as Dictionary<int, Texture>;
            previewCache.TryGetValue(0, out previewTexture);
            SaveRTToFile(previewTexture);
        }
    }

    private void SaveRTToFile(Texture t)
    {
        RenderTexture rt = t as RenderTexture;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, false);

        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

        RenderTexture.active = null;

        //CONVERT TO GAMMA SPACE MANUALLY DUE TO UNITY BUG IN LINEAR SPACE:
        if (PlayerSettings.colorSpace == ColorSpace.Linear)
        {
            Color[] colors = tex.GetPixels();
            for (int i = 0; i < colors.Length - 1; i++)
            {
                colors[i] = new Color(MathF.Pow(colors[i].r, 0.4545f), MathF.Pow(colors[i].g, 0.4545f), MathF.Pow(colors[i].b, 0.4545f), MathF.Pow(colors[i].a, 0.4545f));
            }
            tex.SetPixels(colors);
            tex.Apply();
        }

        string path = EditorUtility.SaveFilePanelInProject("Save png", previewObject.name + "_thumbnail", "png","Please enter a file name to save the texture to");
        if (path.Length != 0)
        {
            byte[] bytes;
            bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
            Debug.Log("Saved to " + path);
            AssetDatabase.Refresh();
        }
    }
}