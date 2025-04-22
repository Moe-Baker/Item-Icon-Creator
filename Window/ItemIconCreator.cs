#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEditorInternal;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

public class ItemIconCreator : EditorWindow
{
    [MenuItem("Window/" + ToolTitle)]
    public static void ShowExample()
    {
        var window = GetWindow<ItemIconCreator>();
        window.titleContent = new GUIContent(ToolTitle);
    }

    [SerializeField]
    VisualTreeAsset VisualTreeAsset = default;

    const string ToolTitle = "Item Icon Creator";
    const string PreviewScenePath = "Misc/Item Icon Preview Scene.unity";
    const string PreviewSceneOverrideLabel = "Item-Icon-Preview-Scene";

    Vector2Int ImageDimensions = new Vector2Int(512, 512);

    public UIElements UI;
    public struct UIElements
    {
        public ListView Entries { get; }
        public VisualElement Preview { get; }

        public FloatField CameraDistance { get; }

        public Vector3Field PivotOffset { get; }
        public Vector3Field PivotRotation { get; }

        public Toggle ShotTransparent { get; }
        public Button TakeShot { get; }

        public UIElements(VisualElement tree)
        {
            Entries = tree.Q<ListView>("Entries");

            Preview = tree.Q<VisualElement>("Preview");

            CameraDistance = tree.Q<FloatField>("Camera-Distance");

            PivotOffset = tree.Q<Vector3Field>("Pivot-Offset");
            PivotRotation = tree.Q<Vector3Field>("Pivot-Rotation");

            ShotTransparent = tree.Q<Toggle>("Shot-Transparent");
            TakeShot = tree.Q<Button>("Take-Shot");
        }
    }

    List<IPreviewItem> EntriesList;
    IPreviewItem SelectedEntry
    {
        get
        {
            var index = UI.Entries.selectedIndex;

            if (index < 0)
                return null;

            return EntriesList[index];
        }
    }

    RenderTexture PreviewTexture;

    Scene PreviewScene;
    Scene OpenPreviewScene()
    {
        //Find Override Scene
        {
            var guids = AssetDatabase.FindAssets($"l:{PreviewSceneOverrideLabel}");

            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);

                return EditorSceneManager.OpenPreviewScene(path);
            }
        }

        //Load Default Scene
        {
            var path = Path.Combine(GetToolDirectoryPath(), PreviewScenePath);

            path = path.Replace('\\', '/');

            return EditorSceneManager.OpenPreviewScene(path);
        }
    }
    void ClosePreviewScene()
    {
        if (PreviewScene.IsValid() is false)
            return;

        EditorSceneManager.ClosePreviewScene(PreviewScene);
    }

    Camera PreviewCamera;
    float CameraDistance
    {
        set
        {
            var position = PreviewCamera.transform.localPosition;

            position.z = -value;

            PreviewCamera.transform.localPosition = position;
        }
    }
    void SetCameraTransparency(bool transparent, bool preview)
    {
        PreviewCamera.clearFlags = transparent ? CameraClearFlags.Color : CameraClearFlags.Skybox;

        PreviewCamera.backgroundColor = transparent ? new Color(0, 0, 0, preview ? 1 : 0) : Color.white;
    }

    Transform PreviewPivot => PreviewCamera.transform.parent;
    Vector3 PivotOffset
    {
        set => PreviewPivot.localPosition = value;
    }
    Vector3 PivotRotation
    {
        set => PreviewPivot.localEulerAngles = value;
    }

    GameObject PreviewInstance;

    static class Preferences
    {
        static Dictionary<string, PreviewData> EntryPreviews;
        static bool IsDirty = false;

        [Serializable]
        public struct PreviewData
        {
            public OptionalValue<float> CameraDistance;
            public float GetCameraDistance() => CameraDistance.Evaluate(Defaults.CameraDistance);

            public OptionalValue<Vector3> PivotOffset;
            public Vector3 GetPivotOffset() => PivotOffset.Evaluate(Defaults.PivotOffset);

            public OptionalValue<Vector3> PivotRotation;
            public Vector3 GetPivotRotation() => PivotRotation.Evaluate(Defaults.PivotRotation);

            public OptionalValue<bool> Transparent;
        }

        [Serializable]
        public struct OptionalValue<T>
        {
            public bool Assigned;
            public T Value;

            public T Evaluate(T fallback) => Assigned ? Value : fallback;

            public OptionalValue(T Value)
            {
                Assigned = true;
                this.Value = Value;
            }
        }

        public static class IO
        {
            public const string FilePath = "ProjectSettings/Item-Icon-Creator.json";

            [Serializable]
            public struct SaveData
            {
                public EntryData[] EntryPreviews;
                [Serializable]
                public struct EntryData
                {
                    public string ID;
                    public PreviewData Preview;

                    public EntryData(string ID, PreviewData Preview)
                    {
                        this.ID = ID;
                        this.Preview = Preview;
                    }
                }
            }

            public static void Save()
            {
                if (IsDirty is false)
                    return;

                var data = new SaveData()
                {
                    EntryPreviews = EntryPreviews
                        .Select(item => new SaveData.EntryData(item.Key, item.Value))
                        .ToArray(),
                };

                var json = JsonUtility.ToJson(data, true);

                File.WriteAllText(FilePath, json);
            }
            public static void Load()
            {
                if (File.Exists(FilePath) is false)
                    return;

                var json = File.ReadAllText(FilePath);

                var data = JsonUtility.FromJson<SaveData>(json);

                EntryPreviews.Clear();

                foreach (var entry in data.EntryPreviews)
                    EntryPreviews[entry.ID] = entry.Preview;
            }
        }

        public static class Defaults
        {
            public static float CameraDistance { get; } = 10f;
            public static Vector3 PivotOffset { get; } = new Vector3(0f, 0f, 0f);
            public static Vector3 PivotRotation { get; } = new Vector3(0f, 0f, 0f);
        }

        public static PreviewData GetData(IPreviewItem target)
        {
            TryGetData(target, out var data);
            return data;
        }
        public static bool TryGetData(IPreviewItem target, out PreviewData data)
        {
            if (TryGetGUID(target, out var guid) is false)
            {
                data = default;
                return false;
            }

            return EntryPreviews.TryGetValue(guid, out data);
        }

        public static bool TrySetCameraDistance(IPreviewItem target, float value)
        {
            if (TryGetGUID(target, out var guid) is false)
                return false;

            IsDirty = true;

            var data = EntryPreviews.GetValueOrDefault(guid);
            data.CameraDistance = new(value);
            EntryPreviews[guid] = data;

            return true;
        }
        public static bool TrySetPivotOffset(IPreviewItem target, Vector3 value)
        {
            if (TryGetGUID(target, out var guid) is false)
                return false;

            IsDirty = true;

            var data = EntryPreviews.GetValueOrDefault(guid);
            data.PivotOffset = new(value);
            EntryPreviews[guid] = data;

            return true;
        }
        public static bool TrySetPivotRotation(IPreviewItem target, Vector3 value)
        {
            if (TryGetGUID(target, out var guid) is false)
                return false;

            IsDirty = true;

            var data = EntryPreviews.GetValueOrDefault(guid);
            data.PivotRotation = new(value);
            EntryPreviews[guid] = data;

            return true;
        }
        public static bool TrySetTransparent(IPreviewItem target, bool value)
        {
            if (TryGetGUID(target, out var guid) is false)
                return false;

            IsDirty = true;

            var data = EntryPreviews.GetValueOrDefault(guid);
            data.Transparent = new(value);
            EntryPreviews[guid] = data;

            return true;
        }

        public static bool TryGetGUID(IPreviewItem target, out string id)
        {
            if (target is not Object asset || EditorUtility.IsPersistent(asset) is false)
            {
                id = default;
                return false;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            id = AssetDatabase.AssetPathToGUID(path);
            return true;
        }

        static Preferences()
        {
            EntryPreviews = new();
        }
    }

    void CreateGUI()
    {
        EntriesList = FindAllPreviewItems();

        if (EntriesList.Count is 0)
        {
            rootVisualElement.Add(new HelpBox("NO Preview Items Found in Project, Try Importing the Sample Items to Start", HelpBoxMessageType.Warning));
            return;
        }

        Preferences.IO.Load();

        AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReloadEvent;

        VisualTreeAsset.CloneTree(rootVisualElement);

        UI = new UIElements(rootVisualElement);

        UI.Entries.itemsSource = EntriesList;
        UI.Entries.selectedIndicesChanged += EntrySelectionChanged;
        if (EntriesList.Count > 0) UI.Entries.SetSelection(0);

        EditorSceneManager.NewPreviewScene();

        //Preview Adjustments
        {
            UI.CameraDistance.RegisterValueChangedCallback(CameraDistanceChanged);
            UI.PivotOffset.RegisterValueChangedCallback(PivotOffsetChanged);
            UI.PivotRotation.RegisterValueChangedCallback(PivotRotationChanged);
        }

        UI.ShotTransparent.RegisterValueChangedCallback(ShotTransparentChange);
        SetCameraTransparency(UI.ShotTransparent.value, preview: true);

        UI.TakeShot.clicked += TakeShotAction;
    }

    void OnDestroy() => Cleanup();
    void BeforeAssemblyReloadEvent() => Cleanup();

    void Cleanup()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReloadEvent;

        ClosePreviewScene();

        PreviewCamera = default;
        PreviewInstance = default;

        if (PreviewTexture) DestroyImmediate(PreviewTexture);
        PreviewTexture = default;

        Preferences.IO.Save();
    }

    void EntrySelectionChanged(IEnumerable<int> indexes)
    {
        Selection.activeObject = SelectedEntry as Object;

        //Open Preview Scene
        if (PreviewScene.IsValid() is false)
            PreviewScene = OpenPreviewScene();

        //Create Preview Texture
        if (PreviewTexture == null)
        {
            PreviewTexture = new RenderTexture(ImageDimensions.x, ImageDimensions.y, 32, RenderTextureFormat.ARGBFloat);
            UI.Preview.style.backgroundImage = Background.FromRenderTexture(PreviewTexture);
        }

        //Find Camera
        if (PreviewCamera == null)
        {
            PreviewCamera = FindComponentInScene<Camera>(PreviewScene);
            PreviewCamera.scene = PreviewScene;
            PreviewCamera.targetTexture = PreviewTexture;
        }

        //Spawn Preview Instance
        {
            if (PreviewInstance) DestroyImmediate(PreviewInstance);

            PreviewInstance = Instantiate(SelectedEntry.Prefab, PreviewScene) as GameObject;
            PreviewInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        //Apply Preferences
        {
            var data = Preferences.GetData(SelectedEntry);

            CameraDistance = UI.CameraDistance.value = data.GetCameraDistance();
            PivotOffset = UI.PivotOffset.value = data.GetPivotOffset();
            PivotRotation = UI.PivotRotation.value = data.GetPivotRotation();

            if (data.Transparent.Assigned)
                UI.ShotTransparent.value = data.Transparent.Value;
        }
    }

    #region Controls
    void PivotOffsetChanged(ChangeEvent<Vector3> evt)
    {
        Preferences.TrySetPivotOffset(SelectedEntry, evt.newValue);
        PivotOffset = evt.newValue;
    }
    void PivotRotationChanged(ChangeEvent<Vector3> evt)
    {
        Preferences.TrySetPivotRotation(SelectedEntry, evt.newValue);
        PivotRotation = evt.newValue;
    }
    void CameraDistanceChanged(ChangeEvent<float> evt)
    {
        Preferences.TrySetCameraDistance(SelectedEntry, evt.newValue);
        CameraDistance = evt.newValue;
    }

    void ShotTransparentChange(ChangeEvent<bool> evt)
    {
        Preferences.TrySetTransparent(SelectedEntry, evt.newValue);
        SetCameraTransparency(evt.newValue, preview: true);
    }

    void TakeShotAction()
    {
        var path = GetIconFilePath(SelectedEntry, "png");

        EnsureDirectoryExists(path);

        SetCameraTransparency(UI.ShotTransparent.value, preview: false);
        PreviewCamera.Render();

        RenderTexture.active = PreviewTexture;

        var mediator = new Texture2D(PreviewTexture.width, PreviewTexture.height, TextureFormat.RGBAFloat, false);
        mediator.ReadPixels(new Rect(0, 0, mediator.width, mediator.height), 0, 0);

        try
        {
            var bytes = ImageConversion.EncodeToPNG(mediator);

            File.WriteAllBytes(path, bytes);

            AssetDatabase.ImportAsset(path);

            //Change Import Settings
            {
                var importer = TextureImporter.GetAtPath(path) as TextureImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }

            //Assign Sprite Reference
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                SelectedEntry.Icon = sprite;

                if (SelectedEntry is Object uObject)
                    EditorUtility.SetDirty(uObject);
            }
        }
        finally
        {
            DestroyImmediate(mediator);

            SetCameraTransparency(UI.ShotTransparent.value, preview: true);
            PreviewCamera.Render();
        }
    }
    #endregion

    #region Asset Database Query
    List<IPreviewItem> FindAllPreviewItems()
    {
        var list = new List<IPreviewItem>();
        FindAllPreviewItems(list);
        return list;
    }
    void FindAllPreviewItems(List<IPreviewItem> targets)
    {
        foreach (var type in TypeCache.GetTypesDerivedFrom<IPreviewItem>())
        {
            var guids = AssetDatabase.FindAssets($"t:{type.Name}");

            targets.Capacity += guids.Length;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset is not IPreviewItem contract)
                    continue;

                targets.Add(contract);
            }
        }
    }
    #endregion

    #region IO
    string GetIconFilePath(IPreviewItem target, string format)
    {
        //From Assigned Icon
        if (target.Icon && EditorUtility.IsPersistent(target.Icon))
        {
            var path = AssetDatabase.GetAssetPath(target.Icon);

            var extension = Path.GetExtension(path.AsSpan()).Trim('.');

            if (extension.Equals(format, StringComparison.OrdinalIgnoreCase))
                return path;

            //Remove current icon file and fallback to next conditions
            AssetDatabase.DeleteAsset(path);
        }

        //From Asset Path
        if (target is Object asset && EditorUtility.IsPersistent(asset))
        {
            var path = AssetDatabase.GetAssetPath(asset);

            path = Path.GetDirectoryName(path);

            return Path.Combine(path, $"{target.GetTitle()}.{format}");
        }

        //Fallback
        {
            return $"Item Icons/{target.GetTitle()}.{format}";
        }
    }
    string GetToolDirectoryPath()
    {
        var guids = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionAsset)} Item-Icon-Creator");

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);

        path = Path.GetDirectoryName(path);

        return path;
    }
    #endregion

    #region Static Utility
    static T FindComponentInScene<T>(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var instance = root.GetComponentInChildren<T>();

            if (instance == null)
                continue;

            return instance;
        }

        return default;
    }

    static void EnsureDirectoryExists(string path)
    {
        if (Path.HasExtension(path))
            path = Path.GetDirectoryName(path);

        Directory.CreateDirectory(path);
    }
    #endregion
}
#endif