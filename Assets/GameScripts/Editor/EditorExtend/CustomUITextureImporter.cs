using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.U2D.Sprites;

public class CustomUITextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        // if (AssetDatabase.Contains(assetImporter.GetInstanceID()))
        // {
        //     Debug.Log("既存の画像の変更ため　何もしない");
        //     return;
        // }
        // Debug.Log($"OnPreprocessTexture {assetImporter.GetType()}");
        if (assetImporter is not TextureImporter)
        {
            return;
        }
        
        string tmp = AssetDatabase.GetAssetPath(assetImporter.GetInstanceID());
        if (tmp.Split('/').Length > 3)
        {//分類しましたから　何もしない
            return;
        }
        
        string fileName = Path.GetFileName(assetPath);
        if (!fileName.StartsWith("UI"))
        {
            return;
        }
        TextureImporter importer = (TextureImporter)assetImporter;
        // UI_Single_Gacha_1001.png
        // UI_Atlas_Gacha_A.png
        string[] info = fileName.Split("_");
        if (info.Length < 4)
        {
            Debug.LogError($"file {fileName} format is invalid");
            return;
        }

        var importerSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(importerSettings);
        
        string usage = info[0];
        string shape = info[1];
        string moduleName = info[2];
        string concreteName = info[3];
        switch (usage)
        {
            case "UI":
                importerSettings.spriteGenerateFallbackPhysicsShape = false;
                importerSettings.textureType = TextureImporterType.Sprite;
                importerSettings.filterMode = FilterMode.Bilinear;
                importerSettings.mipmapEnabled = false;
                importer.SetTextureSettings(importerSettings);
                break;
        }

        switch (shape)
        {
            case "Single":
                importer.spriteImportMode = SpriteImportMode.Single;
                break;
            case "Atlas":
                importer.spriteImportMode = SpriteImportMode.Multiple;
                break;
        }
        
        // Assets/Res/UI/Gacha/10001.png
        string targetDir = $"Assets/Res/{usage}/{moduleName}/";
        string[] folderPaths = targetDir.Split('/');
        string curFolderPath = "Assets/Res";
        for (int i = 2; i < folderPaths.Length; i++)
        {
            bool isValidFolder = AssetDatabase.IsValidFolder($"{curFolderPath}/{folderPaths[i]}");
            if (!isValidFolder)
            {
                AssetDatabase.CreateFolder(curFolderPath, folderPaths[i]);    
            }
            curFolderPath = $"{curFolderPath}/{folderPaths[i]}";
        }

        string completedName = targetDir + concreteName;
        
        EditorApplication.delayCall += () => { MoveTexture(completedName); };
      
    }

    private void MoveTexture(string targetPath)
    { 
        bool isExisted = AssetDatabase.AssetPathExists(targetPath);
        if (isExisted)
        {
            AssetDatabase.DeleteAsset(targetPath);
        }
        string error = AssetDatabase.MoveAsset(assetPath, targetPath);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"ファイル移動が失敗しました: {error}");
            
        }
        else
        {
            Debug.Log($"移動成功: {assetPath} -> {targetPath}");
        }
        
    }

    private void OnPostprocessTexture(Texture2D texture)
    {
        //UnityEditor.U2D.Sprites.ISpriteEditorDataProvider
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(assetImporter);
        dataProvider.InitSpriteEditorDataProvider();
        var physicsOutlineDataProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        var spriteRects = dataProvider.GetSpriteRects();
        foreach (var spriteRect in spriteRects)
        {
            physicsOutlineDataProvider.SetOutlines(spriteRect.spriteID,new List<Vector2[]>());
            physicsOutlineDataProvider.SetTessellationDetail(spriteRect.spriteID,0);
        }
        
        // 应用修改
        dataProvider.Apply();
        assetImporter.SaveAndReimport();
        // SetPlatformSettings(importer, "Android", TextureImporterFormat.ASTC_6x6);
        // SetPlatformSettings(importer, "iPhone", TextureImporterFormat.ASTC_6x6);
        // SetPlatformSettings(importer, "Standalone", TextureImporterFormat.DXT5);
    }

    private void OnPostprocessTexture2DArray(Texture2DArray texture)
    {
        throw new NotImplementedException();
    }

    void SetPlatformSettings(TextureImporter importer, string platform, TextureImporterFormat format)
    {
        var settings = importer.GetPlatformTextureSettings(platform);
        settings.overridden = true;
        settings.format = format;
        importer.SetPlatformTextureSettings(settings);
    }
}

