using FoodHub.API.Extensions;
using FoodHub.Application.Dtos;
using FoodHub.Application.Services;
using FoodHub.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        Ok(await _auth.RegisterAsync(request, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(request, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct) =>
        Ok(await _auth.RefreshAsync(request, ct));
}

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly RestaurantService _restaurants;
    private readonly OrderService _orders;

    public RestaurantsController(RestaurantService restaurants, OrderService orders)
    {
        _restaurants = restaurants;
        _orders = orders;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<RestaurantDto>>> List(CancellationToken ct) =>
        Ok(await _restaurants.ListAsync(ct));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<RestaurantDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _restaurants.GetAsync(id, ct));

    [HttpGet("mine")]
    [Authorize(Roles = nameof(Role.Restaurant))]
    public async Task<ActionResult<IReadOnlyList<RestaurantDto>>> ListMine(CancellationToken ct) =>
        Ok(await _restaurants.ListMineAsync(User.GetUserId(), ct));

    [HttpPost]
    [Authorize(Roles = nameof(Role.Restaurant))]
    public async Task<ActionResult<RestaurantDto>> Create(CreateRestaurantRequest request, CancellationToken ct)
    {
        var created = await _restaurants.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/menu-items")]
    [Authorize(Roles = nameof(Role.Restaurant))]
    public async Task<ActionResult<MenuItemDto>> AddMenuItem(Guid id, AddMenuItemRequest request, CancellationToken ct) =>
        Ok(await _restaurants.AddMenuItemAsync(User.GetUserId(), id, request, ct));

    [HttpGet("{id:guid}/orders")]
    [Authorize(Roles = nameof(Role.Restaurant))]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> ListOrders(Guid id, CancellationToken ct) =>
        Ok(await _orders.ListForRestaurantAsync(User.GetUserId(), id, ct));
}

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orders;
    private readonly DeliveryService _deliveries;

    public OrdersController(OrderService orders, DeliveryService deliveries)
    {
        _orders = orders;
        _deliveries = deliveries;
    }

    [HttpPost]
    [Authorize(Roles = nameof(Role.Customer))]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await _orders.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet("mine")]
    [Authorize(Roles = nameof(Role.Customer))]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> ListMine(CancellationToken ct) =>
        Ok(await _orders.ListMineAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _orders.GetAsync(User.GetUserId(), User.GetRole(), id, ct));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Customer,Restaurant,Admin")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, CancellationToken ct) =>
        Ok(await _orders.CancelAsync(User.GetUserId(), User.GetRole(), id, ct));

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = nameof(Role.Restaurant))]
    public async Task<ActionResult<OrderDto>> Confirm(Guid id, CancellationToken ct) =>
        Ok(await _orders.ConfirmAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:guid}/prepare")]
    [Authorize(Roles = nameof(Role.Restaurant))]
    public async Task<ActionResult<OrderDto>> Prepare(Guid id, CancellationToken ct) =>
        Ok(await _orders.StartPreparingAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:guid}/ready")]
    [Authorize(Roles = nameof(Role.Restaurant))]
    public async Task<ActionResult<OrderDto>> Ready(Guid id, CancellationToken ct) =>
        Ok(await _orders.MarkReadyAsync(User.GetUserId(), id, ct));

    [HttpGet("{id:guid}/delivery-code")]
    [Authorize(Roles = nameof(Role.Customer))]
    public async Task<ActionResult<DeliveryCodeDto>> GetDeliveryCode(Guid id, CancellationToken ct) =>
        Ok(await _deliveries.GetCodeForCustomerAsync(User.GetUserId(), id, ct));
}

[ApiController]
[Route("api/deliveries")]
[Authorize(Roles = nameof(Role.Shipper))]
public class DeliveriesController : ControllerBase
{
    private readonly DeliveryService _deliveries;

    public DeliveriesController(DeliveryService deliveries) => _deliveries = deliveries;

    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> Available(CancellationToken ct) =>
        Ok(await _deliveries.ListAvailableAsync(ct));

    [HttpPost("{orderId:guid}/accept")]
    public async Task<ActionResult<DeliveryDto>> Accept(Guid orderId, CancellationToken ct) =>
        Ok(await _deliveries.AcceptAsync(User.GetUserId(), orderId, ct));

    [HttpPost("{orderId:guid}/start")]
    public async Task<ActionResult<DeliveryDto>> Start(Guid orderId, CancellationToken ct) =>
        Ok(await _deliveries.StartDeliveringAsync(User.GetUserId(), orderId, ct));

    [HttpPost("{orderId:guid}/confirm-receipt")]
    public async Task<ActionResult<DeliveryDto>> ConfirmReceipt(Guid orderId, ConfirmReceiptRequest request, CancellationToken ct) =>
        Ok(await _deliveries.ConfirmReceiptAsync(User.GetUserId(), orderId, request, ct));
}
