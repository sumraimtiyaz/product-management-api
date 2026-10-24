namespace ProductManagement.Domain.Entities;

/// <summary>
/// An entity whose audit columns are stamped centrally when it is saved,
/// so no service has to remember to set them.
/// </summary>
public interface IAuditable
{
    string CreatedBy { get; }
    DateTime CreatedOn { get; }
    string? ModifiedBy { get; }
    DateTime? ModifiedOn { get; }
}
