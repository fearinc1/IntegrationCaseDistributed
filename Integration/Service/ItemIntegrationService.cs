using Integration.Common;
using Integration.Backend;
using StackExchange.Redis;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.

    public Result SaveItem(string itemContent)
    {
        var db = RedisConnectionManager.GetDatabase();
        var lockKey = $"item-lock:{itemContent}";
        var lockToken = Guid.NewGuid().ToString();
        var expiry = TimeSpan.FromSeconds(30); // Lock expiry time

        // Try to acquire the distributed lock
        if (db.StringSet(lockKey, lockToken, expiry, When.NotExists))
        {
            try
            {
                // Check if the content already exists
                if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
                {
                    return new Result(false, $"Duplicate item received with content {itemContent}.");
                }

                // Save the item
                var item = ItemIntegrationBackend.SaveItem(itemContent);
                return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
            }
            finally
            {
                // Release the lock only if the token matches
                var existingToken = db.StringGet(lockKey);
                if (existingToken == lockToken)
                {
                    db.KeyDelete(lockKey);
                }
            }
        }
        else
        {
            // Lock could not be acquired, meaning another process is working on this content
            return new Result(false, $"Item with content {itemContent} is being processed by another node.");
        }
    }
    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}