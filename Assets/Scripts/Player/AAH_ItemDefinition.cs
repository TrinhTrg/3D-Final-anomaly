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

    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public GameObject EquippedModel => _equippedModel;
    public float EquippedSize => _equippedSize;
    public Quaternion EquippedRotation => Quaternion.Euler(_equippedEuler);
    public bool Consumable => _consumable;
    public int SanityRestore => _sanityRestore;
}
