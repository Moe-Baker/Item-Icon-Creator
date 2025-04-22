using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject, IPreviewItem
{
    [field: SerializeField]
    public GameObject Prefab { get; private set; }

    [field: SerializeField]
    public Sprite Icon { get; set; }

    string IPreviewItem.GetTitle() => name;
}