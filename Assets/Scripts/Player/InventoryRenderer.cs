using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryRenderer : NetworkBehaviour
{
    public InventoryRendererCell inventoryRendererCellPrefab;
    public float inventoryRendererCellDelta = 50f;
    public Transform inventoryRendererRoot;
    public TMP_Text interactTargetText;
    public Slider useItemSlider;

    Inventory inventory;
    readonly List<InventoryRendererCell> handsCells = new();
    readonly List<InventoryRendererCell> inventoryCells = new();

    void Awake()
    {
        inventory = transform.parent.GetComponent<Inventory>();
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            gameObject.SetActive(false);
            return;
        }

        // Руки
        Utils.Repeat(Inventory.HANDS_COUNT, i =>
        {
            InventoryRendererCell cell = Instantiate(inventoryRendererCellPrefab, inventoryRendererRoot);
            cell.transform.localPosition = Vector3.right * (i * inventoryRendererCellDelta);
            handsCells.Add(cell);
        });

        // Инициализируем ячейки инвентаря по текущей вместимости
        OnInventoryCapacityChange(inventory.GetInventoryCapacity());
    }

    void Update()
    {
        // Текст взаимодействия
        if (inventory.raycastInteractableTarget)
        {
            if (!interactTargetText.gameObject.activeSelf)
                interactTargetText.gameObject.SetActive(true);

            interactTargetText.text = $"Press E to {(inventory.raycastInteractableTarget is ItemInstance ? "pickup" : "interact")}";
        }
        else if (interactTargetText.gameObject.activeSelf)
        {
            interactTargetText.gameObject.SetActive(false);
        }

        // Слайдер использования предмета
        if (inventory.IsUsingItem)
        {
            if (!useItemSlider.gameObject.activeSelf)
                useItemSlider.gameObject.SetActive(true);

            ItemInstance rightHandItem = inventory.GetItemInRightHand();
            useItemSlider.maxValue = rightHandItem != null ? rightHandItem.itemData.holdTimeToUse : 1f;
            useItemSlider.value = inventory.useHolding;
        }
        else
        {
            if (useItemSlider.gameObject.activeSelf)
                useItemSlider.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        inventory.onHandsChange.AddListener(OnHandsChange);
        inventory.onInventoryChange.AddListener(OnInventoryChange);
        inventory.onInventoryCapacityChange.AddListener(OnInventoryCapacityChange);
    }

    void OnDisable()
    {
        inventory.onHandsChange.RemoveListener(OnHandsChange);
        inventory.onInventoryChange.RemoveListener(OnInventoryChange);
        inventory.onInventoryCapacityChange.RemoveListener(OnInventoryCapacityChange);
    }

    void OnHandsChange(ItemContainer hands)
    {
        OnBaseInventoryChange(hands.containerSlots.ToList(), handsCells);
    }

    void OnInventoryChange(List<ItemContainer> itemContainers)
    {
        List<ItemSlotInfo> itemContainersFlatten = itemContainers
            .SelectMany(ic => ic.containerSlots)
            .ToList();

        OnBaseInventoryChange(itemContainersFlatten, inventoryCells);
    }

    void OnBaseInventoryChange(List<ItemSlotInfo> itemSlots, List<InventoryRendererCell> cells)
    {
        // Берём минимальное количество, чтобы не вылезти за границы
        int count = Mathf.Min(itemSlots.Count, cells.Count);

        for (int i = 0; i < count; i++)
        {
            ItemSlotInfo itemSlotInfo = itemSlots[i];

            string text =
                itemSlotInfo.IsOccupiedByItself
                    ? itemSlotInfo.item.itemData.itemName
                    : itemSlotInfo.IsOccupiedByAnotherSlot
                        ? "---"
                        : string.Empty;

            cells[i].SetCellText(text);
        }

        // Очищаем лишние ячейки, если их больше, чем слотов
        for (int i = count; i < cells.Count; i++)
        {
            cells[i].SetCellText(string.Empty);
        }
    }

    void OnInventoryCapacityChange(int capacity)
    {
        // Добавляем недостающие ячейки
        while (inventoryCells.Count < capacity)
        {
            InventoryRendererCell cell = Instantiate(inventoryRendererCellPrefab, inventoryRendererRoot);
            cell.transform.localPosition =
                Vector3.right * ((Inventory.HANDS_COUNT + inventoryCells.Count) * inventoryRendererCellDelta);
            inventoryCells.Add(cell);
        }

        // Удаляем лишние ячейки
        if (inventoryCells.Count > capacity)
        {
            for (int i = capacity; i < inventoryCells.Count; i++)
            {
                if (inventoryCells[i] != null && inventoryCells[i].gameObject != null)
                    Destroy(inventoryCells[i].gameObject);
            }

            inventoryCells.RemoveRange(capacity, inventoryCells.Count - capacity);
        }
    }
}
