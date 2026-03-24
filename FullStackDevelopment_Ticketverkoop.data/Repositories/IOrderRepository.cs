using FullStackDevelopment_Ticketverkoop.Domain.Entities;

namespace FullStackDevelopment_Ticketverkoop.Data.Repositories;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
}