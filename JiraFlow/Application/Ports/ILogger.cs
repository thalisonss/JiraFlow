namespace JiraFlow.Application.Ports;

/// <summary>
/// Porta de saída: o que a aplicação precisa para registrar eventos.
/// A aplicação conhece só esta interface, nunca o destino real do log (arquivo, console, etc.).
/// </summary>
public interface ILogger
{
    void Info(string mensagem);
    void Warn(string mensagem);
    void Erro(string mensagem);
}
