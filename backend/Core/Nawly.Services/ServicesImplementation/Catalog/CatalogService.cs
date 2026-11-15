using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Catalog;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Catalog;
using Nawly.Services.Mapping.CatalogMapping;
using Nawly.Services.Specifications.CatalogSpec;
using Nawly.Services.Specifications.PromotionSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Catalog;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Catalog;

namespace Nawly.Services.ServicesImplementation.Catalog;

public sealed class CatalogService(
    IUnitOfWork unitOfWork,
    IClock clock) : ICatalogService
{
    public async Task<Result<StoreMenuDto>> GetStoreMenuAsync(
        Guid storeId,
        bool includeUnavailable = false,
        CancellationToken ct = default)
    {
        var categories = await unitOfWork
            .GetRepository<MenuCategory, Guid>()
            .GetAllWithSpecAsync(new StoreMenuCategoriesSpecification(storeId), ct);

        var products = await unitOfWork
            .GetRepository<Product, Guid>()
            .GetAllWithSpecAsync(new StoreProductsSpecification(storeId, includeUnavailable), ct);

        var grouped = categories.Select(cat => new MenuCategoryWithProductsDto(
            cat.Id,
            cat.Name,
            cat.NameAr,
            cat.SortOrder,
            products.Where(p => p.MenuCategoryId == cat.Id).Select(p => p.ToDto()).ToList())).ToList();

        return Result.Success(new StoreMenuDto(storeId, grouped));
    }

    public async Task<Result<PaginationResponse<ProductDto>>> SearchProductsAsync(
        ProductQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Product, Guid>();
        var items = await repo.GetAllWithSpecAsync(new ProductsFilterSpecification(queryParams), ct);
        var count = await repo.GetCountAsync(new ProductsCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<ProductDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            count,
            items.Select(p => p.ToDto()).ToList()));
    }

    public async Task<Result<MenuCategoryDto>> CreateMenuCategoryAsync(
        Guid storeId,
        UpsertMenuCategoryDto request,
        CancellationToken ct = default)
    {
        var category = new MenuCategory
        {
            StoreId = storeId,
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            SortOrder = request.SortOrder,
            CreatedAt = clock.UtcNow
        };

        await unitOfWork.GetRepository<MenuCategory, Guid>().AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(category.ToDto());
    }

    public async Task<Result<MenuCategoryDto>> UpdateMenuCategoryAsync(
        Guid storeId,
        Guid categoryId,
        UpsertMenuCategoryDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<MenuCategory, Guid>();
        var category = await repo.GetByIdWithSpecAsync(new MenuCategoryByIdSpecification(storeId, categoryId), ct);
        if (category is null)
            return Result.Failure<MenuCategoryDto>(Error.NotFound("catalog.category_not_found", "Menu category not found."));

        category.Name = request.Name.Trim();
        category.NameAr = request.NameAr.Trim();
        category.SortOrder = request.SortOrder;
        category.UpdatedAt = clock.UtcNow;

        repo.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(category.ToDto());
    }

    public async Task<Result> DeleteMenuCategoryAsync(Guid storeId, Guid categoryId, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<MenuCategory, Guid>();
        var category = await repo.GetByIdWithSpecAsync(new MenuCategoryByIdSpecification(storeId, categoryId), ct);
        if (category is null)
            return Result.Failure(Error.NotFound("catalog.category_not_found", "Menu category not found."));

        category.IsDeleted = true;
        category.DeletedAt = clock.UtcNow;
        repo.Update(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ProductDto>> CreateProductAsync(Guid storeId, UpsertProductDto request, CancellationToken ct = default)
    {
        var category = await unitOfWork
            .GetRepository<MenuCategory, Guid>()
            .GetByIdWithSpecAsync(new MenuCategoryByIdSpecification(storeId, request.MenuCategoryId), ct);

        if (category is null)
            return Result.Failure<ProductDto>(Error.NotFound("catalog.category_not_found", "Menu category does not belong to this store."));

        var product = new Product
        {
            StoreId = storeId,
            MenuCategoryId = request.MenuCategoryId,
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            Description = request.Description?.Trim(),
            DescriptionAr = request.DescriptionAr?.Trim(),
            ImageRef = request.ImageRef,
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            IsAvailable = request.IsAvailable,
            StockQuantity = request.StockQuantity,
            Sku = request.Sku?.Trim(),
            Barcode = request.Barcode?.Trim(),
            PrepMinutes = request.PrepMinutes,
            SortOrder = request.SortOrder,
            CreatedAt = clock.UtcNow
        };
        product.RefreshNormalizedSearchText();

        await unitOfWork.GetRepository<Product, Guid>().AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(product.ToDto());
    }

    public async Task<Result<ProductDto>> UpdateProductAsync(
        Guid storeId,
        Guid productId,
        UpsertProductDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Product, Guid>();
        var product = await repo.GetByIdWithSpecAsync(new ProductWithOptionsSpecification(productId, storeId), ct);
        if (product is null)
            return Result.Failure<ProductDto>(Error.NotFound("catalog.product_not_found", "Product not found."));

        var category = await unitOfWork
            .GetRepository<MenuCategory, Guid>()
            .GetByIdWithSpecAsync(new MenuCategoryByIdSpecification(storeId, request.MenuCategoryId), ct);

        if (category is null)
            return Result.Failure<ProductDto>(Error.NotFound("catalog.category_not_found", "Menu category does not belong to this store."));

        product.MenuCategoryId = request.MenuCategoryId;
        product.Name = request.Name.Trim();
        product.NameAr = request.NameAr.Trim();
        product.Description = request.Description?.Trim();
        product.DescriptionAr = request.DescriptionAr?.Trim();
        product.ImageRef = request.ImageRef;
        product.Price = request.Price;
        product.CompareAtPrice = request.CompareAtPrice;
        product.IsAvailable = request.IsAvailable;
        product.StockQuantity = request.StockQuantity;
        product.Sku = request.Sku?.Trim();
        product.Barcode = request.Barcode?.Trim();
        product.PrepMinutes = request.PrepMinutes;
        product.SortOrder = request.SortOrder;
        product.UpdatedAt = clock.UtcNow;
        product.RefreshNormalizedSearchText();

        repo.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(product.ToDto());
    }

    public async Task<Result> DeleteProductAsync(Guid storeId, Guid productId, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Product, Guid>();
        var product = await repo.GetByIdWithSpecAsync(new ProductWithOptionsSpecification(productId, storeId), ct);
        if (product is null)
            return Result.Failure(Error.NotFound("catalog.product_not_found", "Product not found."));

        product.IsDeleted = true;
        product.DeletedAt = clock.UtcNow;
        repo.Update(product);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ProductDto>> ToggleProductAvailabilityAsync(
        Guid storeId,
        Guid productId,
        ToggleProductAvailabilityDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Product, Guid>();
        var product = await repo.GetByIdWithSpecAsync(new ProductWithOptionsSpecification(productId, storeId), ct);
        if (product is null)
            return Result.Failure<ProductDto>(Error.NotFound("catalog.product_not_found", "Product not found."));

        product.IsAvailable = request.IsAvailable;
        product.SoldOutUntilNextOpening = !request.IsAvailable && request.SoldOutUntilNextOpening;
        product.UpdatedAt = clock.UtcNow;

        repo.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(product.ToDto());
    }

    public async Task<Result<ProductOptionGroupDto>> UpsertOptionGroupAsync(
        Guid storeId,
        Guid productId,
        Guid? groupId,
        UpsertProductOptionGroupDto request,
        CancellationToken ct = default)
    {
        var product = await unitOfWork
            .GetRepository<Product, Guid>()
            .GetByIdWithSpecAsync(new ProductWithOptionsSpecification(productId, storeId), ct);

        if (product is null)
            return Result.Failure<ProductOptionGroupDto>(Error.NotFound("catalog.product_not_found", "Product not found."));

        var groupRepo = unitOfWork.GetRepository<ProductOptionGroup, Guid>();
        ProductOptionGroup group;

        if (!groupId.HasValue)
        {
            group = new ProductOptionGroup
            {
                ProductId = productId,
                Name = request.Name.Trim(),
                NameAr = request.NameAr.Trim(),
                IsRequired = request.IsRequired,
                MinSelect = request.MinSelect,
                MaxSelect = request.MaxSelect,
                SortOrder = request.SortOrder,
                CreatedAt = clock.UtcNow
            };

            foreach (var opt in request.Options)
            {
                group.Options.Add(new ProductOption
                {
                    GroupId = group.Id,
                    Name = opt.Name.Trim(),
                    NameAr = opt.NameAr.Trim(),
                    PriceDelta = opt.PriceDelta,
                    IsAvailable = opt.IsAvailable,
                    SortOrder = opt.SortOrder,
                    CreatedAt = clock.UtcNow
                });
            }

            await groupRepo.AddAsync(group, ct);
        }
        else
        {
            var existing = await groupRepo.GetByIdWithSpecAsync(new ProductOptionGroupByIdSpecification(productId, groupId.Value), ct);
            if (existing is null)
                return Result.Failure<ProductOptionGroupDto>(Error.NotFound("catalog.option_group_not_found", "Option group not found."));

            existing.Name = request.Name.Trim();
            existing.NameAr = request.NameAr.Trim();
            existing.IsRequired = request.IsRequired;
            existing.MinSelect = request.MinSelect;
            existing.MaxSelect = request.MaxSelect;
            existing.SortOrder = request.SortOrder;
            existing.UpdatedAt = clock.UtcNow;

            var optionRepo = unitOfWork.GetRepository<ProductOption, Guid>();
            foreach (var old in existing.Options.Where(o => !o.IsDeleted))
            {
                old.IsDeleted = true;
                old.DeletedAt = clock.UtcNow;
                optionRepo.Update(old);
            }

            foreach (var opt in request.Options)
            {
                await optionRepo.AddAsync(new ProductOption
                {
                    GroupId = existing.Id,
                    Name = opt.Name.Trim(),
                    NameAr = opt.NameAr.Trim(),
                    PriceDelta = opt.PriceDelta,
                    IsAvailable = opt.IsAvailable,
                    SortOrder = opt.SortOrder,
                    CreatedAt = clock.UtcNow
                }, ct);
            }

            groupRepo.Update(existing);
            group = existing;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(group.ToDto());
    }

    public async Task<Result> DeleteOptionGroupAsync(Guid storeId, Guid productId, Guid groupId, CancellationToken ct = default)
    {
        var product = await unitOfWork
            .GetRepository<Product, Guid>()
            .GetByIdWithSpecAsync(new ProductWithOptionsSpecification(productId, storeId), ct);

        if (product is null)
            return Result.Failure(Error.NotFound("catalog.product_not_found", "Product not found."));

        var groupRepo = unitOfWork.GetRepository<ProductOptionGroup, Guid>();
        var group = await groupRepo.GetByIdWithSpecAsync(new ProductOptionGroupByIdSpecification(productId, groupId), ct);
        if (group is null)
            return Result.Failure(Error.NotFound("catalog.option_group_not_found", "Option group not found."));

        group.IsDeleted = true;
        group.DeletedAt = clock.UtcNow;
        groupRepo.Update(group);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
