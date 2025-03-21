using UnityEngine;
using System.IO;

namespace Arcweave.Project
{
    //...
    [System.Serializable]
    public class Cover
    {
        public enum Type
        {
            Undefined,
            Image,
            Youtube,
        }

        [field: SerializeField]
        public Type type { get; private set; }
        [field: SerializeField]
        public string filePath { get; private set; }

        [System.NonSerialized]
        private Texture2D _cachedImage;

        public Cover(Type type, string filePath) {
            this.type = type;
            this.filePath = filePath;
        }

        ///<summary>Resolves the image from Resources or build folder.</summary>
        public Texture2D ResolveImage() {
            var imageName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            
            // Return cached image if already loaded
            if (_cachedImage != null && _cachedImage.name == imageName) {
                return _cachedImage;
            }
            
            // Try to load from Resources first (original behavior)
            _cachedImage = Resources.Load<Texture2D>(imageName);
            if (_cachedImage != null) {
                return _cachedImage;
            }
            
            // Try to load from build folder
            string buildFolderPath = Application.isEditor ? 
                Application.dataPath.Replace("/Assets", "") : 
                System.IO.Path.GetDirectoryName(Application.dataPath);
            
            string buildImagePath = System.IO.Path.Combine(buildFolderPath, "arcweave/images", System.IO.Path.GetFileName(filePath));
            if (File.Exists(buildImagePath)) {
                return LoadImageFromFile(buildImagePath, imageName);
            }
            
            // If we get here, the image wasn't found
            Debug.LogWarning($"Image not found: {imageName}. Tried Resources and build folder.");
            return null;
        }
        
        private Texture2D LoadImageFromFile(string fullPath, string imageName) {
            try {
                byte[] imageData = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.name = imageName;
                
                if (texture.LoadImage(imageData)) {
                    _cachedImage = texture;
                    return _cachedImage;
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"Error loading image from {fullPath}: {e.Message}");
            }
            
            return null;
        }
    }
}