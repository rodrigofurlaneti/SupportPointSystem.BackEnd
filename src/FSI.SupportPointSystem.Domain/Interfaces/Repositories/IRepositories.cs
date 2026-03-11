using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.ValueObjects;

namespace FSI.SupportPointSystem.Domain.Interfaces.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default);
}

public interface ISellerRepository : IRepository<Seller>
{
    Task<Seller?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Seller>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}

public interface IVisitRepository : IRepository<Visit>
{
    /// <summary>Retorna a visita aberta de um vendedor, ou null se não existir.</summary>
    Task<Visit?> GetActiveVisitBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

    /// <summary>Retorna o histórico de visitas de um vendedor paginado.</summary>
    Task<IReadOnlyList<Visit>> GetVisitHistoryBySellerIdAsync(
        Guid sellerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveVisitAsync(Guid sellerId, CancellationToken cancellationToken = default);
}
