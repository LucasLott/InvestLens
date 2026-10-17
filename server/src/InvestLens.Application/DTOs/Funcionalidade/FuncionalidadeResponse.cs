namespace InvestLens.Application.DTOs.Funcionalidade;

public sealed record FuncionalidadeResponse(int IdFuncionalidade, string Nome, string Descricao, bool ExclusivaPro, bool? Disponivel = null);
