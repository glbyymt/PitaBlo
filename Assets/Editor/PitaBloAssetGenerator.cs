#if UNITY_EDITOR
using Pitablock.Data;
using Pitablock.Core;
using UnityEditor;
using UnityEngine;

namespace Pitablock.EditorTools
{
    public static class PitaBloAssetGenerator
    {
        [MenuItem("Pitablock/Generate Master Data Assets")]
        public static void GenerateAssets()
        {
            EnsureFolder("Assets/ScriptableObjects/Blocks");
            EnsureFolder("Assets/ScriptableObjects/Animals");
            EnsureFolder("Assets/Resources/Animals");

            var sprite = SpriteFactory.CreateRoundedSquareSprite(64, Color.white);
            AssetDatabase.CreateAsset(sprite.texture, "Assets/Sprites/Blocks/BlockCell.png");

            CreateBlockAsset("Block_I", 1, new Color(0.45f, 0.85f, 1f), new[]
            {
                new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)
            });
            CreateBlockAsset("Block_O", 2, new Color(1f, 0.92f, 0.45f), new[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
            });
            CreateBlockAsset("Block_T", 3, new Color(0.85f, 0.55f, 1f), new[]
            {
                new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1)
            });
            CreateBlockAsset("Block_S", 4, new Color(0.55f, 1f, 0.65f), new[]
            {
                new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0)
            });
            CreateBlockAsset("Block_Z", 5, new Color(1f, 0.55f, 0.55f), new[]
            {
                new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
            });
            CreateBlockAsset("Block_J", 6, new Color(0.55f, 0.65f, 1f), new[]
            {
                new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1)
            });
            CreateBlockAsset("Block_L", 7, new Color(1f, 0.75f, 0.45f), new[]
            {
                new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1)
            });

            CreateAnimalAsset("Animal_Rabbit", 1, "うさぎ先生", new Color(1f, 0.75f, 0.8f), 0);
            CreateAnimalAsset("Animal_Elephant", 2, "ぞう先生", new Color(0.75f, 0.8f, 1f), 3);
            CreateAnimalAsset("Animal_Cat", 3, "ねこ先生", new Color(0.9f, 0.85f, 0.6f), 6);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ピタブロ！マスタデータを生成しました。");
        }

        private static void CreateBlockAsset(string fileName, int id, Color color, Vector2Int[] shape)
        {
            var data = ScriptableObject.CreateInstance<BlockData>();
            data.blockId = id;
            data.blockName = fileName;
            data.blockColor = color;
            data.localPositions = shape;
            AssetDatabase.CreateAsset(data, $"Assets/ScriptableObjects/Blocks/{fileName}.asset");
        }

        private static void CreateAnimalAsset(string fileName, int id, string name, Color color, int unlockLevel)
        {
            var data = ScriptableObject.CreateInstance<AnimalData>();
            data.animalId = id;
            data.animalName = name;
            data.animalSprite = SpriteFactory.CreateCircleSprite(96, color);
            data.unlockRequiredLevel = unlockLevel;
            AssetDatabase.CreateAsset(data, $"Assets/ScriptableObjects/Animals/{fileName}.asset");

            var resourcesCopy = ScriptableObject.CreateInstance<AnimalData>();
            resourcesCopy.animalId = id;
            resourcesCopy.animalName = name;
            resourcesCopy.animalSprite = SpriteFactory.CreateCircleSprite(96, color);
            resourcesCopy.unlockRequiredLevel = unlockLevel;
            AssetDatabase.CreateAsset(resourcesCopy, $"Assets/Resources/Animals/{fileName}.asset");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                var folder = System.IO.Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folder))
                {
                    AssetDatabase.CreateFolder(parent, folder);
                }
            }
        }
    }
}
#endif
