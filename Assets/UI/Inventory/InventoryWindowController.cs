using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class InventoryWindowController : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset slotTemplate;

    [Header("Generator")]
    public float fuelTransferPerSecond = 5f;

    private const string RowsRootName = "rows-root";
    private const string ClothesFrameName = "ClothesFrame";
    private const string SlotsContainerName = "SlotsContainer";
    private const string SlotButtonName = "SlotButton";
    private const string SlotImageName = "ItemImage";
    private const string FuelGeneratorName = "FuelGenerator";
    private const string FuelLevelName = "FuelLevel";
    private const string FuelTankNeckTriggerName = "FuelTankNeckTrigger";

    private static readonly EquipableContainerType[] RowTypes =
    {
        EquipableContainerType.Shirt,
        EquipableContainerType.Pants,
        EquipableContainerType.Backpack
    };

    private Player player;
    private Inventory inventory;
    private VisualElement root;
    private VisualElement rowsRoot;
    VisualElement fuelGenerator;
    VisualElement fuelLevel;
    VisualElement fuelTankNeckTrigger;

    private readonly List<RowBinding> rowBindings = new();

    private bool isOpen;
    private bool isDragging;

    private TemplateContainer dragGhost;
    private Image dragGhostImage;
    private Vector2 lastPointerPos;
    private Vector2 dragStartPos;

    private DragSource dragSource = DragSource.Invalid;
    private DropTarget hoverTarget = DropTarget.Invalid;
    private VisualElement hoveredVisual;
    private VisualElement dragSourceVisual;
    private Coroutine dropRoutine;

    Generator generator;

    private sealed class RowBinding
    {
        public EquipableContainerType Type;
        public VisualElement RowRoot;
        public VisualElement ClothesFrame;
        public Button ClothesButton;
        public Image ClothesImage;
        public VisualElement SlotsContainer;
    }

    private enum DragKind
    {
        None,
        InventorySlot,
        ClothesFrame
    }

    private struct DragSource
    {
        public DragKind Kind;
        public EquipableContainerType Type;
        public int SlotIndex;
        public ItemInstance Item;
        public VisualElement Visual;

        public bool IsValid => Kind != DragKind.None && Item != null && Visual != null;

        public static DragSource Invalid => new DragSource
        {
            Kind = DragKind.None,
            Type = EquipableContainerType.Custom,
            SlotIndex = -1,
            Item = null,
            Visual = null
        };
    }

    private struct DropTarget
    {
        public DragKind Kind;
        public EquipableContainerType Type;
        public int SlotIndex;
        public VisualElement Visual;

        public bool IsValid => Kind != DragKind.None && Visual != null;

        public static DropTarget Invalid => new DropTarget
        {
            Kind = DragKind.None,
            Type = EquipableContainerType.Custom,
            SlotIndex = -1,
            Visual = null
        };
    }

    private void Reset()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private IEnumerator Start()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("InventoryWindowController: UIDocument not found.");
            yield break;
        }

        root = uiDocument.rootVisualElement;
        rowsRoot = root.Q<VisualElement>(RowsRootName);

        if (rowsRoot == null)
        {
            Debug.LogError($"InventoryWindowController: '{RowsRootName}' not found in inventory.uxml.");
            yield break;
        }

        fuelGenerator = root.Q<VisualElement>(FuelGeneratorName);
        fuelLevel = root.Q<VisualElement>(FuelLevelName);
        fuelTankNeckTrigger = root.Q<VisualElement>(FuelTankNeckTriggerName);

        CacheRows();
        CreateDragGhost();
        SetOpen(false);

        while (NetworkClient.localPlayer == null)
            yield return null;

        player = NetworkClient.localPlayer.GetComponent<Player>();
        inventory = player.Inventory;
        if (inventory == null)
        {
            Debug.LogError("InventoryWindowController: local player has no Inventory.");
            yield break;
        }

        BindInventoryEvents();
        Rebuild();
    }

    private void OnDisable()
    {
        UnbindInventoryEvents();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            ToggleInventory();

        if (fuelGenerator.style.display != DisplayStyle.None) fuelLevel.style.maxHeight = new(new Length(generator.currentFuel / generator.maxFuel * 100, LengthUnit.Percent));

        if (isDragging && dragSource.Item == null) EndDrag();
        if (!isOpen || !isDragging || dragGhost == null || dragGhost.style.display == DisplayStyle.None)
            return;

        bool resetRotation = true;
        if (isDragging && dragSource.Item.TryGetProperty(out ItemPropertyFuelCanister fuelCanister)) {
            float dragX = dragGhost.worldBound.center.x;
            if (fuelTankNeckTrigger.worldBound.xMin <= dragX && dragX <= fuelTankNeckTrigger.worldBound.xMax) {
                CmdTryTransferFuelToGenerator(fuelCanister, dragSource.Item, generator, fuelTransferPerSecond * Time.deltaTime);
                dragGhost.style.rotate = new(new Rotate(
                    Mathf.Lerp(dragGhost.style.rotate.value.angle.value, -45, 10 * Time.deltaTime)
                ));
                resetRotation = false;
            }
        }
        if (resetRotation && dragGhost.style.rotate.value.angle.value != 0) {
            dragGhost.style.rotate = new(new Rotate(
                Mathf.Lerp(dragGhost.style.rotate.value.angle.value, 0, 10 * Time.deltaTime)
            ));
        }

        dragGhost.style.left = lastPointerPos.x + 12f;
        dragGhost.style.top = lastPointerPos.y + 12f;
    }

    [Command]
    public void CmdTryTransferFuelToGenerator(ItemPropertyFuelCanister fuelCanister, ItemInstance item, Generator generator, float fuelAmount) {
        fuelCanister.TryTransferFuelToGenerator(item, generator, fuelAmount);
    }

    [Client]
    public void ToggleInventory()
    {
        SetOpen(!isOpen);
    }

    [Client]
    public void SetOpen(bool open, Generator generator = null)
    {
        isOpen = open;
        this.generator = generator;

        if (root != null)
            root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        fuelGenerator.style.display = generator ? DisplayStyle.Flex : DisplayStyle.None;

        UnityEngine.Cursor.visible = open;
        UnityEngine.Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;

        if (player != null)
            player.SetPlayerState(open ? PlayerState.Interacting : PlayerState.Default);
    }

    private void CacheRows()
    {
        rowBindings.Clear();

        List<VisualElement> rows = rowsRoot.Children().ToList();
        int count = Mathf.Min(rows.Count, RowTypes.Length);

        for (int i = 0; i < count; i++)
        {
            VisualElement row = rows[i];
            VisualElement clothesFrame = row.Q<VisualElement>(ClothesFrameName);
            VisualElement slotsContainer = row.Q<VisualElement>(SlotsContainerName);

            if (clothesFrame == null || slotsContainer == null)
                continue;

            Button clothesButton = clothesFrame.Q<Button>(SlotButtonName);
            Image clothesImage = clothesFrame.Q<Image>(SlotImageName);
            if (clothesImage == null)
                clothesImage = clothesFrame.Q<Image>();

            if (clothesButton != null)
                clothesButton.text = string.Empty;

            var binding = new RowBinding
            {
                Type = RowTypes[i],
                RowRoot = row,
                ClothesFrame = clothesFrame,
                ClothesButton = clothesButton,
                ClothesImage = clothesImage,
                SlotsContainer = slotsContainer
            };

            rowBindings.Add(binding);

            int rowIndex = i;

            if (clothesFrame != null)
            {
                clothesFrame.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0) return;
                    if (rowIndex < 0 || rowIndex >= rowBindings.Count) return;

                    ItemInstance item = GetEquippedItem(rowBindings[rowIndex].Type);
                    if (item == null) return;

                    BeginDrag(DragKind.ClothesFrame, rowBindings[rowIndex].Type, -1, item, rowBindings[rowIndex].ClothesFrame);
                    evt.StopPropagation();
                }, TrickleDown.TrickleDown);

                clothesFrame.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    if (!isDragging) return;

                    SetHoverTarget(new DropTarget
                    {
                        Kind = DragKind.ClothesFrame,
                        Type = RowTypes[rowIndex],
                        SlotIndex = -1,
                        Visual = binding.ClothesFrame
                    });
                }, TrickleDown.TrickleDown);

                clothesFrame.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    if (!isDragging) return;
                    ClearHoverTargetIfMatches(DragKind.ClothesFrame, RowTypes[rowIndex], -1);
                }, TrickleDown.TrickleDown);
            }
        }

        root.RegisterCallback<PointerMoveEvent>(OnRootPointerMove);
        root.RegisterCallback<PointerUpEvent>(OnRootPointerUp);
    }

    private void CreateDragGhost()
    {
        if (slotTemplate == null)
            return;

        dragGhost = slotTemplate.CloneTree();
        dragGhost.pickingMode = PickingMode.Ignore;
        dragGhost.style.position = Position.Absolute;
        dragGhost.style.display = DisplayStyle.None;
        dragGhost.style.opacity = 0.9f;
        root.Add(dragGhost);

        dragGhostImage = dragGhost.Q<Image>(SlotImageName);
        if (dragGhostImage == null)
            dragGhostImage = dragGhost.Q<Image>();
    }

    private void BindInventoryEvents()
    {
        inventory.onHandsChange.AddListener(OnAnyInventoryChanged);
        inventory.onInventoryChange.AddListener(OnAnyInventoryChanged);
        inventory.onInventoryCapacityChange.AddListener(OnAnyInventoryChanged);
        inventory.onSlotChanged.AddListener(OnAnySlotChanged);
    }

    private void UnbindInventoryEvents()
    {
        if (inventory == null) return;

        inventory.onHandsChange.RemoveListener(OnAnyInventoryChanged);
        inventory.onInventoryChange.RemoveListener(OnAnyInventoryChanged);
        inventory.onInventoryCapacityChange.RemoveListener(OnAnyInventoryChanged);
        inventory.onSlotChanged.RemoveListener(OnAnySlotChanged);
    }

    private void OnAnyInventoryChanged(ItemContainer _) => Rebuild();
    private void OnAnyInventoryChanged(List<ItemContainer> _) => Rebuild();
    private void OnAnyInventoryChanged(int _) => Rebuild();
    private void OnAnySlotChanged(ItemContainer _, int __) => Rebuild();

    private void OnRootPointerMove(PointerMoveEvent evt)
    {
        lastPointerPos = evt.position;
    }

    private void OnRootPointerUp(PointerUpEvent evt)
    {
        if (!isDragging || !dragSource.IsValid)
            return;

        if (TryBuildDropAction(out Action action, out VisualElement targetVisual))
        {
            StopDropRoutineIfAny();
            dropRoutine = StartCoroutine(AnimateGhostAndCommit(targetVisual, action));
        }
        else
        {
            StopDropRoutineIfAny();
            dropRoutine = StartCoroutine(AnimateGhostAndReturn());
        }
    }

    private void StopDropRoutineIfAny()
    {
        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }
    }

    private void Rebuild()
    {
        if (inventory == null || rowsRoot == null || slotTemplate == null)
            return;

        foreach (RowBinding row in rowBindings)
        {
            UpdateRow(row);
        }
    }

    private void UpdateRow(RowBinding row)
    {
        if (row == null || row.SlotsContainer == null || row.ClothesImage == null)
            return;

        if (!TryGetContainer(row.Type, out ItemContainer container, out ItemData headerItem))
        {
            SetImage(row.ClothesImage, null);
            row.SlotsContainer.Clear();
            return;
        }

        SetImage(row.ClothesImage, headerItem != null && headerItem.itemIcon != null ? headerItem.itemIcon.texture : null);

        row.SlotsContainer.Clear();

        for (int slotIndex = 0; slotIndex < container.capacity; slotIndex++)
        {
            ItemInstance item = container.GetItem(slotIndex);
            row.SlotsContainer.Add(CreateSlotVisual(row.Type, slotIndex, item));
        }
    }

    private TemplateContainer CreateSlotVisual(EquipableContainerType rowType, int slotIndex, ItemInstance item)
    {
        TemplateContainer slot = slotTemplate.CloneTree();

        Button button = slot.Q<Button>(SlotButtonName);
        Image image = slot.Q<Image>(SlotImageName);

        if (button != null)
            button.text = string.Empty;

        if (image == null)
            image = slot.Q<Image>();

        SetImage(image, item != null && item.itemData != null && item.itemData.itemIcon != null
            ? item.itemData.itemIcon.texture
            : null);

        VisualElement rootVisual = slot;
        int capturedSlotIndex = slotIndex;
        EquipableContainerType capturedType = rowType;

        rootVisual.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            if (item == null) return;

            BeginDrag(DragKind.InventorySlot, capturedType, capturedSlotIndex, item, rootVisual);
            evt.StopPropagation();
        }, TrickleDown.TrickleDown);

        rootVisual.RegisterCallback<PointerEnterEvent>(evt =>
        {
            if (!isDragging) return;

            SetHoverTarget(new DropTarget
            {
                Kind = DragKind.InventorySlot,
                Type = capturedType,
                SlotIndex = capturedSlotIndex,
                Visual = rootVisual
            });
        }, TrickleDown.TrickleDown);

        rootVisual.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            if (!isDragging) return;
            ClearHoverTargetIfMatches(DragKind.InventorySlot, capturedType, capturedSlotIndex);
        }, TrickleDown.TrickleDown);

        return slot;
    }

    private void BeginDrag(DragKind kind, EquipableContainerType type, int slotIndex, ItemInstance item, VisualElement sourceVisual)
    {
        if (item == null || sourceVisual == null)
            return;

        isDragging = true;
        dragSource = new DragSource
        {
            Kind = kind,
            Type = type,
            SlotIndex = slotIndex,
            Item = item,
            Visual = sourceVisual
        };

        dragSourceVisual = sourceVisual;
        dragStartPos = GetVisualTopLeft(sourceVisual);

        dragGhostImage.image = item.itemData != null && item.itemData.itemIcon != null
            ? item.itemData.itemIcon.texture
            : null;

        dragGhost.style.left = dragStartPos.x;
        dragGhost.style.top = dragStartPos.y;
        dragGhost.style.display = DisplayStyle.Flex;

        if (dragSourceVisual != null)
        {
            dragSourceVisual.AddToClassList("dragging");
            dragSourceVisual.style.opacity = 0.45f;
        }
    }

    private void SetHoverTarget(DropTarget target)
    {
        if (!isDragging)
            return;

        if (hoveredVisual != null)
            hoveredVisual.RemoveFromClassList("hovered");

        hoverTarget = target;
        hoveredVisual = target.Visual;

        if (hoveredVisual != null)
            hoveredVisual.AddToClassList("hovered");
    }

    private void ClearHoverTargetIfMatches(DragKind kind, EquipableContainerType type, int slotIndex)
    {
        if (!hoverTarget.IsValid)
            return;

        if (hoverTarget.Kind != kind || hoverTarget.Type != type || hoverTarget.SlotIndex != slotIndex)
            return;

        if (hoveredVisual != null)
            hoveredVisual.RemoveFromClassList("hovered");

        hoveredVisual = null;
        hoverTarget = DropTarget.Invalid;
    }

    private bool TryBuildDropAction(out Action action, out VisualElement targetVisual)
    {
        action = null;
        targetVisual = null;

        if (!dragSource.IsValid || !hoverTarget.IsValid)
            return false;

        if (dragSource.Kind == DragKind.InventorySlot && hoverTarget.Kind == DragKind.InventorySlot)
        {
            if (!CanMoveSlotToSlot(dragSource.Type, dragSource.SlotIndex, hoverTarget.Type, hoverTarget.SlotIndex))
                return false;

            EquipableContainerType fromType = dragSource.Type;
            int fromSlot = dragSource.SlotIndex;
            EquipableContainerType toType = hoverTarget.Type;
            int toSlot = hoverTarget.SlotIndex;

            action = () => inventory.CmdMoveItemBetweenContainers(fromType, fromSlot, toType, toSlot);
            targetVisual = hoverTarget.Visual;
            return true;
        }

        if (dragSource.Kind == DragKind.InventorySlot && hoverTarget.Kind == DragKind.ClothesFrame)
        {
            if (!CanEquipFromSlotToFrame(dragSource.Type, dragSource.SlotIndex, hoverTarget.Type))
                return false;

            EquipableContainerType fromType = dragSource.Type;
            int fromSlot = dragSource.SlotIndex;

            action = () => inventory.CmdEquipContainerFromSlot(fromType, fromSlot);
            targetVisual = hoverTarget.Visual;
            return true;
        }

        if (dragSource.Kind == DragKind.ClothesFrame && hoverTarget.Kind == DragKind.InventorySlot)
        {
            if (!CanUnequipFrameToSlot(dragSource.Type, hoverTarget.Type, hoverTarget.SlotIndex))
                return false;

            EquipableContainerType fromType = dragSource.Type;
            EquipableContainerType toType = hoverTarget.Type;
            int toSlot = hoverTarget.SlotIndex;

            action = () => inventory.CmdUnequipContainerToSlot(fromType, toType, toSlot);
            targetVisual = hoverTarget.Visual;
            return true;
        }

        return false;
    }

    private bool CanMoveSlotToSlot(EquipableContainerType fromType, int fromSlot, EquipableContainerType toType, int toSlot)
    {
        ItemContainer source = GetContainer(fromType);
        ItemContainer target = GetContainer(toType);
        if (source == null || target == null) return false;

        ItemInstance item = source.GetItem(fromSlot);
        if (item == null) return false;

        return target.IsSlotFreeForPotentialItem(toSlot, item);
    }

    private bool CanEquipFromSlotToFrame(EquipableContainerType fromType, int fromSlot, EquipableContainerType targetType)
    {
        ItemContainer source = GetContainer(fromType);
        if (source == null) return false;

        ItemInstance item = source.GetItem(fromSlot);
        if (item == null) return false;
        if (!TryGetEquipableData(item, out ItemPropertyEquipableContainer equipable)) return false;
        if (equipable.containerType != targetType) return false;

        return GetContainerIndex(targetType) == -1;
    }

    private bool CanUnequipFrameToSlot(EquipableContainerType fromType, EquipableContainerType toType, int toSlot)
    {
        if (fromType == toType) return false;

        ItemContainer source = GetContainer(fromType);
        ItemContainer target = GetContainer(toType);

        if (source == null || target == null) return false;
        if (!source.IsEmpty()) return false;

        ItemInstance item = source.containerItem;
        if (item == null) return false;

        return target.IsSlotFreeForPotentialItem(toSlot, item);
    }

    private IEnumerator AnimateGhostAndCommit(VisualElement targetVisual, Action action)
    {
        isDragging = false;

        if (dragSourceVisual != null)
        {
            dragSourceVisual.RemoveFromClassList("dragging");
            dragSourceVisual.style.opacity = 1f;
        }

        Vector2 endPos = targetVisual != null ? GetVisualTopLeft(targetVisual) : dragStartPos;
        yield return AnimateGhostTo(endPos, 0.14f);

        action?.Invoke();
        EndDrag();
    }

    private IEnumerator AnimateGhostAndReturn()
    {
        isDragging = false;

        if (dragSourceVisual != null)
        {
            dragSourceVisual.RemoveFromClassList("dragging");
            dragSourceVisual.style.opacity = 1f;
        }

        yield return AnimateGhostTo(dragStartPos, 0.14f);
        EndDrag();
    }

    private IEnumerator AnimateGhostTo(Vector2 targetPos, float duration)
    {
        if (dragGhost == null)
            yield break;

        Vector2 startPos = new Vector2(dragGhost.resolvedStyle.left, dragGhost.resolvedStyle.top);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);

            dragGhost.style.left = Mathf.Lerp(startPos.x, targetPos.x, t);
            dragGhost.style.top = Mathf.Lerp(startPos.y, targetPos.y, t);

            yield return null;
        }

        dragGhost.style.left = targetPos.x;
        dragGhost.style.top = targetPos.y;
    }

    private void EndDrag()
    {
        isDragging = false;
        dragSource = DragSource.Invalid;
        hoverTarget = DropTarget.Invalid;

        if (hoveredVisual != null)
            hoveredVisual.RemoveFromClassList("hovered");

        hoveredVisual = null;

        if (dragSourceVisual != null)
        {
            dragSourceVisual.RemoveFromClassList("dragging");
            dragSourceVisual.style.opacity = 1f;
        }

        dragSourceVisual = null;

        if (dragGhost != null)
        {
            dragGhost.style.display = DisplayStyle.None;
            if (dragGhostImage != null)
                dragGhostImage.image = null;
        }

        dropRoutine = null;
    }

    private Vector2 GetVisualTopLeft(VisualElement element)
    {
        Rect r = element.worldBound;
        return new Vector2(r.xMin, r.yMin);
    }

    private void SetImage(Image image, Texture texture)
    {
        if (image == null)
            return;

        image.image = texture;
        image.visible = texture != null;
    }

    private bool TryGetContainer(EquipableContainerType type, out ItemContainer container, out ItemData headerItem)
    {
        int index = GetContainerIndex(type);
        if (index < 0 || index >= inventory.inventoryContainers.Count)
        {
            container = null;
            headerItem = null;
            return false;
        }

        container = inventory.inventoryContainers[index];
        headerItem = index < inventory.inventoryContainerHeaderItems.Count
            ? inventory.inventoryContainerHeaderItems[index]
            : null;

        return container != null;
    }

    private ItemContainer GetContainer(EquipableContainerType type)
    {
        int index = GetContainerIndex(type);
        if (index < 0 || index >= inventory.inventoryContainers.Count) return null;
        return inventory.inventoryContainers[index];
    }

    private int GetContainerIndex(EquipableContainerType type)
    {
        for (int i = 0; i < inventory.inventoryContainerTypes.Count; i++)
        {
            if ((EquipableContainerType)inventory.inventoryContainerTypes[i] == type)
                return i;
        }
        return -1;
    }

    private ItemInstance GetEquippedItem(EquipableContainerType type)
    {
        ItemContainer container = GetContainer(type);
        return container != null ? container.containerItem : null;
    }

    private bool TryGetEquipableData(ItemInstance item, out ItemPropertyEquipableContainer equipable)
    {
        equipable = null;
        if (item == null || item.itemData == null || item.itemData.itemProperties == null) return false;

        equipable = item.itemData.itemProperties.OfType<ItemPropertyEquipableContainer>().FirstOrDefault();
        return equipable != null;
    }
}