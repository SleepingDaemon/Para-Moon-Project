using UnityEngine;
using UnityEditor;

public class OutlineTextureGenerator : EditorWindow
{
    [MenuItem("Tools/Create Outline Texture")]
    public static void CreateOutlineTexture()
    {
        // Create a 3x3 texture
        Texture2D outlineTex = new Texture2D(3, 3, TextureFormat.RGBA32, false);

        // Set all pixels to white
        Color white = Color.white;
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                outlineTex.SetPixel(x, y, white);
            }
        }

        // Set center pixel to transparent
        outlineTex.SetPixel(1, 1, new Color(0, 0, 0, 0));

        // Apply changes
        outlineTex.Apply();

        // Save the texture
        string path = EditorUtility.SaveFilePanelInProject("Save Outline Texture", "OutlineTexture", "png", "Save outline texture");
        if (path.Length > 0)
        {
            byte[] pngData = outlineTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            AssetDatabase.Refresh();

            // Set texture import settings
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.spritePixelsPerUnit = 100;

                // Set sprite borders for 9-slice
                importer.spriteBorder = new Vector4(1, 1, 1, 1);

                // Apply import settings
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                Debug.Log("Outline texture created at: " + path);
            }
        }
    }
}