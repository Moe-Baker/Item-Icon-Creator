using UnityEngine;

public interface IPreviewItem
{
    GameObject Prefab { get; }

    Sprite Icon { get; set; }

    public string GetTitle() => ToString();
}