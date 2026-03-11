using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FSI.SupportPointSystem.Infrastructure.Persistence.Repositories;

public abstract class Repository<T>(AppDbContext context) : IRepository<T>
    where T : class
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);
}

public sealed class UserRepository(AppDbContext context)
    : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.Cpf.Value == cpf.Value, cancellationToken);

    public async Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(u => u.Cpf.Value == cpf.Value, cancellationToken);
}

public sealed class SellerRepository(AppDbContext context)
    : Repository<Seller>(context), ISellerRepository
{
    public async Task<Seller?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Seller>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await DbSet.Where(s => s.IsActive).AsNoTracking().ToListAsync(cancellationToken);
}

public sealed class CustomerRepository(AppDbContext context)
    : Repository<Customer>(context), ICustomerRepository
{
    public async Task<Customer?> GetByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(c => c.Cnpj.Value == cnpj.Value, cancellationToken);

    public async Task<bool> ExistsByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(c => c.Cnpj.Value == cnpj.Value, cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await DbSet.Where(c => c.IsActive).AsNoTracking().ToListAsync(cancellationToken);
}

public sealed class VisitRepository(AppDbContext context)
    : Repository<Visit>(context), IVisitRepository
{
    public async Task<Visit?> GetActiveVisitBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(
            v => v.SellerId == sellerId && v.CheckoutTimestamp == null,
            cancellationToken);

    public async Task<bool> HasActiveVisitAsync(Guid sellerId, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(
            v => v.SellerId == sellerId && v.CheckoutTimestamp == null,
            cancellationToken);

    public async Task<IReadOnlyList<Visit>> GetVisitHistoryBySellerIdAsync(
        Guid sellerId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(v => v.SellerId == sellerId)
            .OrderByDescending(v => v.CheckinTimestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
