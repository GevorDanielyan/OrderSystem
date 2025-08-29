using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Features.DTO;
using OrderService.Application.Features.Orders;
using OrderService.Api.Infrastructure.Contracts.Dto;

namespace OrderService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;


    [HttpPost]
    [ProducesResponseType(typeof(CreateEntityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] IReadOnlyList<CreateOrderRequest> requests, CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(requests);
        var orderIds = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = orderIds }, orderIds);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetOrderQuery(id);
        var order = await _mediator.Send(query, cancellationToken);
        return Ok(order);
    }
}
