using Moq;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Application.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _service = new ProductService(_products.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Create_adds_the_product_and_saves_once()
    {
        var dto = await _service.CreateAsync(new CreateProductRequest("  Widget  "), CancellationToken.None);

        _products.Verify(repository => repository.Add(It.Is<Product>(product => product.ProductName == "Widget")), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Widget", dto.ProductName);
    }

    [Fact]
    public async Task GetById_returns_the_product()
    {
        _products.Setup(repository => repository.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Product.Create("Widget"));

        var dto = await _service.GetByIdAsync(3, CancellationToken.None);

        Assert.Equal("Widget", dto.ProductName);
    }

    [Fact]
    public async Task GetById_throws_NotFound_for_an_unknown_product()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(99, CancellationToken.None));
    }

    [Fact]
    public async Task GetPaged_maps_every_product_and_keeps_the_paging_information()
    {
        var page = new PagedResult<Product>(new[] { Product.Create("A"), Product.Create("B") }, 2, 2, 5);
        _products.Setup(repository => repository.GetPagedAsync(2, 2, It.IsAny<CancellationToken>())).ReturnsAsync(page);

        var result = await _service.GetPagedAsync(new PagedQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        Assert.Equal(new[] { "A", "B" }, result.Items.Select(item => item.ProductName).ToArray());
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task Update_renames_the_tracked_product_and_saves()
    {
        var product = Product.Create("Old");
        _products.Setup(repository => repository.GetForUpdateAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var dto = await _service.UpdateAsync(4, new UpdateProductRequest("New"), CancellationToken.None);

        Assert.Equal("New", product.ProductName);
        Assert.Equal("New", dto.ProductName);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_throws_NotFound_and_saves_nothing_for_an_unknown_product()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateAsync(99, new UpdateProductRequest("New"), CancellationToken.None));

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_removes_a_product_that_has_no_items()
    {
        var product = Product.Create("Widget");
        _products.Setup(repository => repository.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _products.Setup(repository => repository.HasItemsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _service.DeleteAsync(5, CancellationToken.None);

        _products.Verify(repository => repository.Remove(product), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_throws_Conflict_and_removes_nothing_when_the_product_has_items()
    {
        _products.Setup(repository => repository.GetForUpdateAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Product.Create("Widget"));
        _products.Setup(repository => repository.HasItemsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(5, CancellationToken.None));

        _products.Verify(repository => repository.Remove(It.IsAny<Product>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_throws_NotFound_for_an_unknown_product()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99, CancellationToken.None));
    }
}
