using Moq;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Tests.Services;

public class ItemServiceTests
{
    private const int ProductId = 10;

    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IItemRepository> _items = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ItemService _service;

    public ItemServiceTests()
    {
        _service = new ItemService(_products.Object, _items.Object, _unitOfWork.Object);
    }

    private void ProductExists() =>
        _products.Setup(repository => repository.ExistsAsync(ProductId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

    [Fact]
    public async Task Create_adds_an_item_to_an_existing_product_and_saves()
    {
        ProductExists();

        var dto = await _service.CreateAsync(ProductId, new CreateItemRequest(4), CancellationToken.None);

        _items.Verify(
            repository => repository.Add(It.Is<Item>(item => item.ProductId == ProductId && item.Quantity == 4)),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(ProductId, dto.ProductId);
        Assert.Equal(4, dto.Quantity);
    }

    [Fact]
    public async Task Create_throws_NotFound_and_saves_nothing_when_the_product_does_not_exist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(ProductId, new CreateItemRequest(1), CancellationToken.None));

        _items.Verify(repository => repository.Add(It.IsAny<Item>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPaged_throws_NotFound_when_the_product_does_not_exist()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetPagedAsync(ProductId, new PagedQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task GetPaged_maps_the_items_of_the_product()
    {
        ProductExists();
        var page = new PagedResult<Item>(new[] { Item.Create(ProductId, 1), Item.Create(ProductId, 2) }, 1, 20, 2);
        _items.Setup(repository => repository.GetPagedByProductAsync(ProductId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _service.GetPagedAsync(ProductId, new PagedQuery(), CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, result.Items.Select(item => item.Quantity).ToArray());
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetById_throws_NotFound_when_the_item_is_not_part_of_the_product()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(ProductId, 1, CancellationToken.None));
    }

    [Fact]
    public async Task Update_changes_the_quantity_and_saves()
    {
        var item = Item.Create(ProductId, 1);
        _items.Setup(repository => repository.GetForUpdateAsync(ProductId, 3, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var dto = await _service.UpdateAsync(ProductId, 3, new UpdateItemRequest(9), CancellationToken.None);

        Assert.Equal(9, item.Quantity);
        Assert.Equal(9, dto.Quantity);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_throws_NotFound_for_an_unknown_item()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateAsync(ProductId, 3, new UpdateItemRequest(9), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_removes_the_item_and_saves()
    {
        var item = Item.Create(ProductId, 1);
        _items.Setup(repository => repository.GetForUpdateAsync(ProductId, 3, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        await _service.DeleteAsync(ProductId, 3, CancellationToken.None);

        _items.Verify(repository => repository.Remove(item), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_throws_NotFound_for_an_unknown_item()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(ProductId, 3, CancellationToken.None));
    }
}
