using IMS.Api.Domain.Common;
using IMS.Api.Domain.ValueObjects;

namespace IMS.Api.Domain.Entities;

/// <summary>
/// Category entity representing a hierarchical classification system for products
/// </summary>
public sealed class Category : SoftDeletableEntity<CategoryId>
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public CategoryId? ParentCategoryId { get; private set; }
    public int Level { get; private set; }
    public string Path { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public TenantId TenantId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public UserId CreatedBy { get; private set; }
    public UserId? UpdatedBy { get; private set; }

    // Navigation properties for hierarchical relationships
    private readonly List<Category> _childCategories = new();
    public IReadOnlyCollection<Category> ChildCategories => _childCategories.AsReadOnly();

    private Category(
        CategoryId id,
        string name,
        string code,
        string description,
        CategoryId? parentCategoryId,
        int level,
        string path,
        int sortOrder,
        bool isActive,
        TenantId tenantId,
        UserId createdBy) : base(id)
    {
        Name = name;
        Code = code;
        Description = description;
        ParentCategoryId = parentCategoryId;
        Level = level;
        Path = path;
        SortOrder = sortOrder;
        IsActive = isActive;
        TenantId = tenantId;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static Category CreateRootCategory(
        string name,
        string code,
        string description,
        int sortOrder,
        bool isActive,
        TenantId tenantId,
        UserId createdBy)
    {
        ValidateCategoryData(name, code, description);

        var categoryId = CategoryId.CreateNew();
        var path = $"/{categoryId}";

        return new Category(
            categoryId,
            name,
            code,
            description,
            null, // No parent for root category
            0, // Root level
            path,
            sortOrder,
            isActive,
            tenantId,
            createdBy);
    }

    public static Category CreateChildCategory(
        string name,
        string code,
        string description,
        Category parentCategory,
        int sortOrder,
        bool isActive,
        TenantId tenantId,
        UserId createdBy)
    {
        ValidateCategoryData(name, code, description);

        if (parentCategory == null)
            throw new ArgumentNullException(nameof(parentCategory));

        if (parentCategory.TenantId != tenantId)
            throw new ArgumentException("Parent category must belong to the same tenant", nameof(parentCategory));

        if (!parentCategory.IsActive)
            throw new ArgumentException("Parent category must be active", nameof(parentCategory));

        var categoryId = CategoryId.CreateNew();
        var level = parentCategory.Level + 1;
        var path = $"{parentCategory.Path}/{categoryId}";

        // Validate hierarchy depth (max 5 levels)
        if (level > 4) // 0-based, so 4 means 5 levels
            throw new InvalidOperationException("Category hierarchy cannot exceed 5 levels");

        return new Category(
            categoryId,
            name,
            code,
            description,
            parentCategory.Id,
            level,
            path,
            sortOrder,
            isActive,
            tenantId,
            createdBy);
    }

    public void UpdateDetails(
        string name,
        string description,
        int sortOrder,
        UserId updatedBy)
    {
        ValidateCategoryData(name, Code, description);

        Name = name;
        Description = description;
        SortOrder = sortOrder;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate(UserId updatedBy)
    {
        if (IsActive)
            throw new InvalidOperationException("Category is already active");

        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate(UserId updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Category is already inactive");

        // Check if category has active child categories
        if (_childCategories.Any(c => c.IsActive && !c.IsDeleted))
            throw new InvalidOperationException("Cannot deactivate category with active child categories");

        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddChildCategory(Category childCategory)
    {
        if (childCategory == null)
            throw new ArgumentNullException(nameof(childCategory));

        if (childCategory.ParentCategoryId != Id)
            throw new ArgumentException("Child category must have this category as parent", nameof(childCategory));

        if (_childCategories.Any(c => c.Id == childCategory.Id))
            throw new InvalidOperationException("Child category already exists");

        _childCategories.Add(childCategory);
    }

    public void RemoveChildCategory(CategoryId childCategoryId)
    {
        var childCategory = _childCategories.FirstOrDefault(c => c.Id == childCategoryId);
        if (childCategory != null)
        {
            _childCategories.Remove(childCategory);
        }
    }

    public bool IsRootCategory()
    {
        return ParentCategoryId == null && Level == 0;
    }

    public bool IsChildOf(CategoryId parentCategoryId)
    {
        return ParentCategoryId == parentCategoryId;
    }

    public bool IsDescendantOf(CategoryId ancestorCategoryId)
    {
        return Path.Contains($"/{ancestorCategoryId}/") || Path.EndsWith($"/{ancestorCategoryId}");
    }

    public bool HasChildren()
    {
        return _childCategories.Any(c => !c.IsDeleted);
    }

    public bool HasActiveChildren()
    {
        return _childCategories.Any(c => c.IsActive && !c.IsDeleted);
    }

    public IEnumerable<CategoryId> GetAncestorIds()
    {
        var pathParts = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return pathParts.Take(pathParts.Length - 1) // Exclude self
                       .Select(part => CategoryId.Create(Guid.Parse(part)));
    }

    public override void SoftDelete(UserId deletedBy)
    {
        // Check if category has active child categories
        if (_childCategories.Any(c => c.IsActive && !c.IsDeleted))
            throw new InvalidOperationException("Cannot delete category with active child categories");

        base.SoftDelete(deletedBy);
    }

    private static void ValidateCategoryData(string name, string code, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Category code is required", nameof(code));

        if (name.Length > 255)
            throw new ArgumentException("Category name cannot exceed 255 characters", nameof(name));

        if (code.Length > 50)
            throw new ArgumentException("Category code cannot exceed 50 characters", nameof(code));

        if (!string.IsNullOrEmpty(description) && description.Length > 1000)
            throw new ArgumentException("Category description cannot exceed 1000 characters", nameof(description));
    }
}