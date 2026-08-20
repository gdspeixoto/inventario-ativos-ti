using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Domain.Events;

/// <summary>
/// Sinaliza que uma mudanca relevante ocorreu e precisa virar linha na timeline.
///
/// As entidades emitem este evento dentro dos proprios metodos de mutacao; o
/// <c>DbContext</c> os converte em <c>TimelineEvent</c> ao salvar. Assim o
/// historico nao depende de o caso de uso lembrar de registra-lo, que e o modo
/// classico de a auditoria ficar incompleta com o tempo.
/// </summary>
public sealed record TimelineEntryRequested(
    TimelineEntityType EntityType,
    Guid EntityId,
    TimelineEventType EventType,
    string Title,
    string Description,
    Guid? OrganizationalUnitId = null,
    decimal? PreviousAmount = null,
    decimal? NewAmount = null) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
