using UnityEngine;

[CreateAssetMenu(menuName = "AAH/Inventory Item", fileName = "Item_")]
public class AAH_ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _itemId = "item";
    [SerializeField] private string _displayName = "Item";
    [SerializeField] private Sprite _icon;

    [Header("Equipped view")]
    [Tooltip("Existing model shown in front of the camera when this item is selected. Optional.")]
    [SerializeField] private GameObject _equippedModel;
    [Tooltip("Largest dimension of the equipped model in metres.")]
    [SerializeField, Min(0.01f)] private float _equippedSize = 0.3f;
    [SerializeField] private Vector3 _equippedEuler;

    [Header("Consumable")]
    [SerializeField] private bool _consumable;
    [Tooltip("Sanity restored when consumed.")]
    [SerializeField, Min(0)] private int _sanityRestore;

    // Per-dose identity lets the room remove only the item issued for this order.
    public static AAH_ItemDefinition CreateMedicine(string label, Texture2D image, GameObject bottle)
    {
        var item = CreateInstance<AAH_ItemDefinition>();
        item._itemId = "medicine_" + image.name;
        item._displayName = label;
        item._equippedModel = bottle;
        item._equippedEuler = bottle.transform.localEulerAngles;
        item._equippedSize = 0.45f;
        item._icon = Sprite.Create(image, new Rect(0f, 0f, image.width, image.height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        item._ownsIcon = true;
        return item;
    }

    private bool _ownsIcon;
    private void OnDestroy()
    {
        if (_ownsIcon && _icon != null) Destroy(_icon);
    }

    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public GameObject EquippedModel => _equippedModel;
    public float EquippedSize => _equippedSize;
    public Quaternion EquippedRotation => Quaternion.Euler(_equippedEuler);
    public bool Consumable => _consumable;
    public int SanityRestore => _sanityRestore;
}
