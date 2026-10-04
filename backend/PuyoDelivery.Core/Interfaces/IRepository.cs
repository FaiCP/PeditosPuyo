using System.Linq.Expressions;
using PuyoDelivery.Core.Common;
using PuyoDelivery.Core.Entities;

namespace PuyoDelivery.Core.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
