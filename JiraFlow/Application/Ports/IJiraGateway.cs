using JiraFlow.Application.Models;

namespace JiraFlow.Application.Ports;

/// <summary>
/// Porta de saída: tudo que a aplicação precisa pedir ao Jira.
/// A aplicação não sabe se isso é HTTP, outro cliente ou um objeto falso de teste.
/// </summary>
public interface IJiraGateway
{
    /// <summary>Valida a credencial e devolve o accountId do dono do token.</summary>
    Task<Resultado<string>> AutenticarAsync();

    /// <summary>Busca as chaves dos chamados da JQL (todas as páginas). Lança exceção se a busca falhar.</summary>
    Task<IReadOnlyList<string>> BuscarChavesAsync(string jql);

    /// <summary>Procura a transição que leva ao status informado. Valor = null quando não existe.</summary>
    Task<Resultado<string>> BuscarTransicaoAsync(string chave, string statusDestino);

    Task<Resultado> MoverAsync(string chave, string transicaoId);

    Task<Resultado> AtribuirAsync(string chave, string accountId);

    Task<Resultado> ComentarAsync(string chave, string texto, bool interno);
}
