using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureApi.Data;
using SecureApi.Models;
using SecureApi.Models.DTOs;
using Stripe;
using Stripe.Checkout;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PaymentController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int MAX_CART_ITEMS = 50;
        private const decimal MAX_ITEM_PRICE = 1000000;
        private const int MAX_QUANTITY = 999;

        public PaymentController(
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            ILogger<PaymentController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
            _userManager = userManager;
        }

        [Authorize]
        [HttpPost("create-checkout-session")]
        public async Task<ActionResult> CreateCheckoutSession([FromBody] List<CartItemDto> items)
        {
            try
            {
                // 1. Validate cart
                if (items == null || !items.Any())
                    return BadRequest(new { error = "Cart is empty" });

                if (items.Count > MAX_CART_ITEMS)
                    return BadRequest(new { error = $"Cart cannot exceed {MAX_CART_ITEMS} items" });

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.ProductName))
                        return BadRequest(new { error = "Product name is required" });

                    if (item.ProductName.Length > 255)
                        return BadRequest(new { error = "Product name too long" });

                    if (item.Price <= 0 || item.Price > MAX_ITEM_PRICE)
                        return BadRequest(new { error = $"Invalid price for {item.ProductName}" });

                    if (item.Quantity <= 0 || item.Quantity > MAX_QUANTITY)
                        return BadRequest(new { error = $"Invalid quantity for {item.ProductName}" });
                }

                // 2. ✅ Get authenticated user using Identity
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // ✅ Check if user account is locked
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return Unauthorized(new { error = "Account is locked" });
                }

                // ✅ Check email confirmation (optional)
                // if (!await _userManager.IsEmailConfirmedAsync(user))
                // {
                //     return Unauthorized(new { error = "Email not confirmed" });
                // }

                var userId = user.Id;
                var userEmail = user.Email;

                // 3. Get user language
                var userLang = HttpContext.Features.Get<IRequestCultureFeature>()
                    ?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "en";

                var stripeLocale = userLang switch
                {
                    "ar" => "en",
                    "fr" => "fr",
                    "de" => "de",
                    "es" => "es",
                    _ => "auto"
                };

                // 4. Calculate total
                decimal totalAmount = items.Sum(i => i.Price * i.Quantity);

                if (totalAmount <= 0)
                    return BadRequest(new { error = "Order total must be greater than zero" });

                if (totalAmount > 10000000)
                    return BadRequest(new { error = "Order total exceeds maximum" });

                _logger.LogInformation(
                    "User {Email} creating checkout for {Amount:C} SAR",
                    userEmail, totalAmount);

                // 5. Create order
                string orderReference = Guid.NewGuid().ToString("N")[..12].ToUpper();
                Order order;

                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        order = new Order
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = userId,
                            OrderReference = orderReference,
                            TotalAmount = totalAmount,
                            Status = OrderStatus.Pending,
                            CreatedAt = DateTime.UtcNow,
                            SessionExpiresAt = DateTime.UtcNow.AddMinutes(30)
                        };

                        _dbContext.Order.Add(order);
                        await _dbContext.SaveChangesAsync();

                        var orderItems = items.Select(i => new OrderItems
                        {
                            Id = Guid.NewGuid().ToString(),
                            OrderId = order.Id,
                            ProductId = i.ProductId,
                            Name = i.ProductName.Trim(),
                            ImageUrl = i.Image,
                            Price = i.Price,
                            Quantity = i.Quantity
                        }).ToList();

                        _dbContext.OrderItems.AddRange(orderItems);
                        await _dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "Order {OrderRef} created for user {Email}",
                            orderReference, userEmail);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Failed to create order for {Email}", userEmail);
                        return StatusCode(500, new { error = "Failed to create order" });
                    }
                }

                // 6. Configure Stripe
                StripeConfiguration.ApiKey = _configuration["Stripe:StripeKey"];
                var domain = _configuration["Stripe:FrontendUrl"];

                if (string.IsNullOrWhiteSpace(domain))
                {
                    _logger.LogError("Stripe:FrontendUrl not configured");
                    return StatusCode(500, new { error = "Payment configuration error" });
                }

                // 7. Create Stripe session
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = items.Select(i => new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "sar",
                            UnitAmount = (long)(i.Price * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = i.ProductName,
                                Images = !string.IsNullOrWhiteSpace(i.Image)
                                    ? new List<string> { i.Image }
                                    : null
                            }
                        },
                        Quantity = i.Quantity,
                    }).ToList(),
                    Locale = stripeLocale,
                    Mode = "payment",
                    SuccessUrl = $"{domain}/success?order_ref={orderReference}&session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{domain}/cancel?order_ref={orderReference}",
                    ClientReferenceId = orderReference,
                    CustomerEmail = userEmail,
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_id", order.Id },
                        { "order_reference", orderReference },
                        { "user_id", userId },
                        { "user_email", userEmail ?? "" }
                    },
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                    BillingAddressCollection = "required"
                };

                var service = new SessionService();
                Session session = await service.CreateAsync(options);

                // 8. Update order with session ID
                order.StripeSessionId = session.Id;
                order.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Stripe session {SessionId} created for order {OrderRef}",
                    session.Id, orderReference);

                return Ok(new
                {
                    id = session.Id,
                    url = session.Url,
                    orderReference = orderReference,
                    orderId = order.Id,
                    expiresAt = session.ExpiresAt
                });
            }
            catch (StripeException stripeEx)
            {
                _logger.LogError(stripeEx, "Stripe error: {Message}", stripeEx.Message);
                return StatusCode(500, new { error = "Payment service error" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in checkout");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }

        [HttpPost("webhook")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrWhiteSpace(stripeSignature))
            {
                _logger.LogWarning("Webhook without signature");
                return BadRequest("Missing signature");
            }

            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];

                if (string.IsNullOrWhiteSpace(webhookSecret))
                {
                    _logger.LogError("Webhook secret not configured");
                    return StatusCode(500);
                }

                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    webhookSecret,
                    throwOnApiVersionMismatch: false
                );

                _logger.LogInformation(
                    "Webhook: {EventType} - {EventId}",
                    stripeEvent.Type,
                    stripeEvent.Id);

                // Idempotency check
                var exists = await _dbContext.StripeWebhookEvents
                    .AnyAsync(e => e.EventId == stripeEvent.Id);

                if (exists)
                {
                    _logger.LogInformation("Event {EventId} already processed", stripeEvent.Id);
                    return Ok();
                }

                // Handle events
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        var session = stripeEvent.Data.Object as Session;
                        await HandleSuccessfulPayment(session);
                        break;

                    case "checkout.session.expired":
                        var expiredSession = stripeEvent.Data.Object as Session;
                        await HandleExpiredSession(expiredSession);
                        break;

                    case "payment_intent.succeeded":
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        _logger.LogInformation("PaymentIntent succeeded: {Id}", paymentIntent?.Id);
                        break;

                    case "payment_intent.payment_failed":
                        var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                        await HandleFailedPayment(failedIntent);
                        break;

                    default:
                        _logger.LogInformation("Unhandled: {EventType}", stripeEvent.Type);
                        break;
                }

                // Record event
                _dbContext.StripeWebhookEvents.Add(new StripeWebhookEvent
                {
                    EventId = stripeEvent.Id,
                    EventType = stripeEvent.Type,
                    ProcessedAt = DateTime.UtcNow,
                    Payload = json
                });
                await _dbContext.SaveChangesAsync();

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Invalid webhook signature");
                return BadRequest("Invalid signature");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook processing error");
                return StatusCode(500);
            }
        }

        private async Task HandleSuccessfulPayment(Session? session)
        {
            if (session?.ClientReferenceId == null) return;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var order = await _dbContext.Order
                    .FirstOrDefaultAsync(o => o.OrderReference == session.ClientReferenceId);

                if (order == null)
                {
                    _logger.LogError("Order not found: {Ref}", session.ClientReferenceId);
                    return;
                }

                if (order.Status == OrderStatus.Paid)
                {
                    _logger.LogWarning("Order {Ref} already paid", session.ClientReferenceId);
                    return;
                }

                // Verify amount
                var paidAmount = session.AmountTotal / 100m ??0;
                if (Math.Abs(paidAmount - order.TotalAmount) > 0.01m)
                {
                    _logger.LogError(
                        "Amount mismatch for {Ref}. Expected: {Expected}, Paid: {Paid}",
                        session.ClientReferenceId, order.TotalAmount, paidAmount);

                    order.Status = "PaymentMismatch";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return;
                }

                order.Status = OrderStatus.Paid;
                order.StripePaymentIntentId = session.PaymentIntentId;
                order.PaymentMethod = session.PaymentMethodTypes?.FirstOrDefault();
                order.PaidAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Payment confirmed: {Ref}", session.ClientReferenceId);

                // TODO: Send email notification
                // await _emailService.SendOrderConfirmation(order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error handling payment for {Ref}", session?.ClientReferenceId);
            }
        }

        private async Task HandleExpiredSession(Session? session)
        {
            if (session?.ClientReferenceId == null) return;

            var order = await _dbContext.Order
                .FirstOrDefaultAsync(o => o.OrderReference == session.ClientReferenceId);

            if (order?.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Expired;
                order.CancelledAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Order expired: {Ref}", session.ClientReferenceId);
            }
        }

        private async Task HandleFailedPayment(PaymentIntent? paymentIntent)
        {
            if (paymentIntent == null) return;

            var order = await _dbContext.Order
                .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntent.Id);

            if (order?.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.PaymentFailed;
                order.FailureReason = paymentIntent.LastPaymentError?.Message;
                order.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogWarning(
                    "Payment failed: {Ref} - {Reason}",
                    order.OrderReference,
                    paymentIntent.LastPaymentError?.Message);
            }
        }

        [Authorize]
        [HttpGet("order/{orderReference}")]
        public async Task<ActionResult> GetOrderStatus(string orderReference)
        {
            if (string.IsNullOrWhiteSpace(orderReference))
                return BadRequest(new { error = "Order reference required" });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _dbContext.Order
                .Include(o => o.OrderItems)
               .AsNoTracking()
                .FirstOrDefaultAsync(o =>
                    o.OrderReference == orderReference &&
                    o.UserId == user.Id);

            if (order == null)
                return NotFound(new { error = "Order not found" });

            return Ok(new
            {
                orderId = order.Id,
                orderReference = order.OrderReference,
                status = order.Status,
                totalAmount = order.TotalAmount,
                createdAt = order.CreatedAt,
                paidAt = order.PaidAt,
                expiresAt = order.SessionExpiresAt,
                isExpired = order.IsExpired,
                canBeCancelled = order.CanBeCancelled,
                items = order.OrderItems.Select(i => new
                {
                    productId = i.ProductId,
                    name = i.Name,
                    imageUrl = i.ImageUrl,
                    price = i.Price,
                    quantity = i.Quantity,
                    subtotal = i.Subtotal
                })
            });
        }

        [Authorize]
        [HttpPost("order/{orderReference}/cancel")]
        public async Task<ActionResult> CancelOrder(string orderReference)
        {
            if (string.IsNullOrWhiteSpace(orderReference))
                return BadRequest(new { error = "Order reference required" });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _dbContext.Order
                .FirstOrDefaultAsync(o =>
                    o.OrderReference == orderReference &&
                    o.UserId == user.Id);

            if (order == null)
                return NotFound(new { error = "Order not found" });

            if (!order.CanBeCancelled)
                return BadRequest(new { error = $"Cannot cancel order with status: {order.Status}" });

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Order {Ref} cancelled by {Email}", orderReference, user.Email);

            return Ok(new { message = "Order cancelled successfully" });
        }

        [Authorize]
        [HttpGet("orders")]
        public async Task<ActionResult> GetUserOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var query = _dbContext.Order
                .Where(o => o.UserId == user.Id);

            if (!string.IsNullOrWhiteSpace(status) && OrderStatus.IsValid(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var totalOrders = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .Select(o => new
                {
                    orderId = o.Id,
                    orderReference = o.OrderReference,
                    status = o.Status,
                    totalAmount = o.TotalAmount,
                    createdAt = o.CreatedAt,
                    paidAt = o.PaidAt
                })
                .ToListAsync();

            return Ok(new
            {
                orders,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalOrders,
                    totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize)
                }
            });
        }
    }

    
}