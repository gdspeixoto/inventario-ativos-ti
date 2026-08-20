namespace ControleAtivos.Application.Abstractions;

/// <summary>
/// Fronteira transacional. A conversao de eventos de dominio em linhas de
/// timeline acontece dentro do mesmo <c>SaveChanges</c>, de modo que a mudanca
/// e o seu registro historico sejam atomicos.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
