namespace OrderService.Application.Features.DTO;

public record CreateEntityResponse(IReadOnlyList<Guid> CreatedIds);
