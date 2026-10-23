namespace JiraFlow.Application.Models;

/// <summary>
/// Resultado de uma operação sem valor de retorno (mover, atribuir, comentar).
/// Em vez de lançar exceção para falhas "esperadas" (HTTP 400, 403...), o gateway devolve este objeto.
/// </summary>
public sealed record Resultado(bool Sucesso, int StatusHttp, string Detalhe)
{
    public static Resultado Ok(int statusHttp) => new(true, statusHttp, string.Empty);
    public static Resultado Falha(int statusHttp, string detalhe) => new(false, statusHttp, detalhe);
}
