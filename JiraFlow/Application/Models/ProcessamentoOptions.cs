namespace JiraFlow.Application.Models;

/// <summary>
/// Parâmetros do processamento: o que buscar e o que fazer com cada chamado encontrado.
/// </summary>
public sealed record ProcessamentoOptions(
    string Jql,
    string StatusDestino,
    bool AtribuirParaMim,
    string? Comentario,
    bool ComentarioInterno);
