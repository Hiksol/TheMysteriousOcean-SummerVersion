using Mirror;

public class ItemSpaItemSpawnOnStartwnZone : NetworkBehaviour
{
    public ItemInstance itemPrefab;
    public ItemData itemData;

    public override void OnStartServer() {
        ItemInstance item = Instantiate(itemPrefab, transform.position, transform.rotation);
        NetworkServer.Spawn(item.gameObject);
        item.SetItemData(itemData);
    }
}
