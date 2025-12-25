using IMS.Api.Domain.Common;
using IMS.Api.Domain.Enums;

namespace IMS.Api.Domain.Entities;

public class ProductAttribute : SoftDeletableEntity<Guid>
{
    public string Name { get; private set; }
    public string Value { get; private set; }
    public AttributeDataType DataType { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ProductAttribute(Guid id, string name, string value, AttributeDataType dataType) : base(id)
    {
        Name = name;
        Value = value;
        DataType = dataType;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static ProductAttribute Create(string name, string value, AttributeDataType dataType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Attribute name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Attribute value cannot be null or empty", nameof(value));

        ValidateValueForDataType(value, dataType);

        return new ProductAttribute(Guid.NewGuid(), name.Trim(), value.Trim(), dataType);
    }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Attribute value cannot be null or empty", nameof(value));

        ValidateValueForDataType(value, DataType);
        Value = value.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateValueForDataType(string value, AttributeDataType dataType)
    {
        switch (dataType)
        {
            case AttributeDataType.Integer:
                if (!int.TryParse(value, out _))
                    throw new ArgumentException($"Value '{value}' is not a valid integer");
                break;
            case AttributeDataType.Decimal:
                if (!decimal.TryParse(value, out _))
                    throw new ArgumentException($"Value '{value}' is not a valid decimal");
                break;
            case AttributeDataType.Boolean:
                if (!bool.TryParse(value, out _))
                    throw new ArgumentException($"Value '{value}' is not a valid boolean");
                break;
            case AttributeDataType.DateTime:
                if (!DateTime.TryParse(value, out _))
                    throw new ArgumentException($"Value '{value}' is not a valid datetime");
                break;
            case AttributeDataType.Text:
                // Text values are always valid
                break;
            default:
                throw new ArgumentException($"Unknown data type: {dataType}");
        }
    }
}