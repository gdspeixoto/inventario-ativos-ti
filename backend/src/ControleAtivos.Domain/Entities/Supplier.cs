using ControleAtivos.Domain.Common;
using ControleAtivos.Domain.Enums;

namespace ControleAtivos.Domain.Entities;

/// <summary>Fornecedor de licencas, infraestrutura ou servicos.</summary>
public sealed class Supplier : TenantEntity
{
    private readonly List<Contract> _contracts = [];

    private Supplier()
    {
    }

    public Supplier(Guid id, Guid tenantId, string name, string? documentNumber = null)
        : base(id, tenantId)
    {
        Name = Guard.MaxLength(Guard.NotEmpty(name, nameof(name)), 200, nameof(name));
        DocumentNumber = NormalizeDocument(documentNumber);
        Status = SupplierStatus.Ativo;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>CNPJ ou identificador fiscal, apenas digitos.</summary>
    public string? DocumentNumber { get; private set; }

    public ContractCategory? Category { get; private set; }

    public string? MainContactName { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? Website { get; private set; }

    public string? SlaDescription { get; private set; }

    public SupplierStatus Status { get; private set; }

    public IReadOnlyCollection<Contract> Contracts => _contracts.AsReadOnly();

    /// <summary>
    /// Corrige a razao social.
    ///
    /// O cadastro comum nao permite alterar nome nem documento, para nao
    /// transformar um fornecedor em outro por descuido. Esta operacao existe
    /// para o caso legitimo de erro de digitacao, e por isso e separada.
    /// </summary>
    public void Rename(string name, Guid? userId = null)
    {
        Name = Guard.MaxLength(Guard.NotEmpty(name, nameof(name)), 200, nameof(name));
        Touch(userId);
    }

    public void UpdateContactInfo(
        string? contactName,
        string? email,
        string? phone,
        string? website,
        Guid? userId = null)
    {
        MainContactName = Guard.Optional(contactName);
        Email = Guard.Optional(email)?.ToLowerInvariant();
        Phone = Guard.Optional(phone);
        Website = Guard.Optional(website);
        Touch(userId);
    }

    public void Classify(ContractCategory category, string? slaDescription = null, Guid? userId = null)
    {
        Category = category;
        SlaDescription = Guard.Optional(slaDescription);
        Touch(userId);
    }

    public void ChangeStatus(SupplierStatus status, Guid? userId = null)
    {
        Status = status;
        Touch(userId);
    }

    private static string? NormalizeDocument(string? documentNumber)
    {
        var cleaned = Guard.Optional(documentNumber);
        if (cleaned is null)
        {
            return null;
        }

        var digits = new string(cleaned.Where(char.IsDigit).ToArray());
        DomainException.ThrowIf(digits.Length is not (11 or 14), "CPF/CNPJ invalido.");
        return digits;
    }
}
