namespace JiraFlow.Application.Models;

/// <summary>
/// Resultado de uma operação que também devolve um valor (ex.: accountId, id da transição).
/// </summary>
public sealed record Resultado<T>(bool Sucesso, int StatusHttp, T? Valor, string Detalhe)
{
    public static Resultado<T> Ok(int statusHttp, T? valor) => new(true, statusHttp, valor, string.Empty);
    public static Resultado<T> Falha(int statusHttp, string detalhe) => new(false, statusHttp, default, detalhe);
}
