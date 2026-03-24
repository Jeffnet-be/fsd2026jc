using FullStackDevelopment_Ticketverkoop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FullStackDevelopment_Ticketverkoop.Data.Repositories;

/// <summary>
/// Handles creation and retrieval of purchase orders.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context) => _context = context;

    public async Task<Order> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
    {
        return await _context.Orders
            .Include(o => o.OrderLines).ThenInclude(ol => ol.Ticket)
                .ThenInclude(t => t!.Match)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}