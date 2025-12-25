using IMS.Api.Domain.Common;
using System.Text.Json;

namespace IMS.Api.Domain.ValueObjects;

public sealed class MovementMetadata : ValueObject
{
    public Dictionary<string, object> Data { get; }

    // Parameterless constructor for EF Core
    private MovementMetadata()
    {
        Data = new Dictionary<string, object>();
    }

    private MovementMetadata(Dictionary<string, object> data)
    {
        Data = new Dictionary<string, object>(data);
    }

    public static MovementMetadata Empty()
    {
        return new MovementMetadata(new Dictionary<string, object>());
    }

    public static MovementMetadata Create(Dictionary<string, object> data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        return new MovementMetadata(data);
    }

    public static MovementMetadata FromDictionary(Dictionary<string, object> data)
    {
        return Create(data);
    }

    public static MovementMetadata FromTransfer(WarehouseId sourceWarehouseId, WarehouseId destinationWarehouseId)
    {
        return Create(new Dictionary<string, object>
        {
            ["sourceWarehouseId"] = sourceWarehouseId.Value,
            ["destinationWarehouseId"] = destinationWarehouseId.Value,
            ["transferType"] = "warehouse_transfer"
        });
    }

    public static MovementMetadata FromSale(string orderNumber, string customerReference = null)
    {
        var data = new Dictionary<string, object>
        {
            ["orderNumber"] = orderNumber,
            ["saleType"] = "customer_sale"
        };

        if (!string.IsNullOrWhiteSpace(customerReference))
            data["customerReference"] = customerReference;

        return Create(data);
    }

    public static MovementMetadata FromRefund(string originalSaleReference, string refundReason = null)
    {
        var data = new Dictionary<string, object>
        {
            ["originalSaleReference"] = originalSaleReference,
            ["refundType"] = "customer_refund"
        };

        if (!string.IsNullOrWhiteSpace(refundReason))
            data["refundReason"] = refundReason;

        return Create(data);
    }

    public T GetValue<T>(string key)
    {
        if (!Data.ContainsKey(key))
            throw new KeyNotFoundException($"Metadata key '{key}' not found");

        var value = Data[key];
        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
        }

        return (T)value;
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        value = default(T);
        
        if (!Data.ContainsKey(key))
            return false;

        try
        {
            var rawValue = Data[key];
            if (rawValue is JsonElement jsonElement)
            {
                value = JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            else
            {
                value = (T)rawValue;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool HasKey(string key)
    {
        return Data.ContainsKey(key);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var kvp in Data.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(Data);
    }
}