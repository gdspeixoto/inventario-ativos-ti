namespace ControleAtivos.Domain.Common;

/// <summary>
/// Evento de dominio. A camada de persistencia converte os eventos acumulados
/// em <c>TimelineEvent</c> ao salvar, garantindo que nenhuma mudanca relevante
/// dependa de o caso de uso lembrar de registrar historico.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
