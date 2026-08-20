namespace ControleAtivos.Domain.Enums;

/// <summary>
/// Niveis da arvore institucional:
/// Matriz/Filial &gt; Negocio &gt; Area Tecnologica &gt; Unidade Operacional &gt; Nucleo de Suporte.
/// O valor numerico representa a profundidade esperada e e usado para validar
/// a hierarquia no momento do cadastro.
/// </summary>
public enum OrganizationalUnitType
{
    Matriz = 0,
    Filial = 1,
    Negocio = 2,
    AreaTecnologica = 3,
    UnidadeOperacional = 4,
    NucleoSuporte = 5,
}
