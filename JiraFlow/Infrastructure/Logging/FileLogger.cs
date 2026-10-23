using System.Text;
using JiraFlow.Application.Ports;

namespace JiraFlow.Infrastructure.Logging;

/// <summary>
/// Logger simples em arquivo, com nível, data/hora com milissegundos e rotação por dia e por tamanho.
/// Arquivo ativo: jiraflow.log. Arquivos rotacionados: jiraflow-yyyyMMdd-HHmmss-fff.log.
/// </summary>
public sealed class FileLogger : ILogger
{
    private const string NomeAtivo = "jiraflow.log";
    private const string PadraoRotacionados = "jiraflow-*.log";

    private readonly string _pasta;
    private readonly string _arquivoAtivo;
    private readonly long _tamanhoMaximoBytes;
    private readonly int _arquivosRetidos;
    private readonly NivelLog _nivelMinimo;
    private readonly object _trava = new();

    public FileLogger(string pasta, long tamanhoMaximoBytes, int arquivosRetidos, NivelLog nivelMinimo)
    {
        _pasta = pasta;
        _arquivoAtivo = Path.Combine(pasta, NomeAtivo);
        _tamanhoMaximoBytes = tamanhoMaximoBytes;
        _arquivosRetidos = arquivosRetidos;
        _nivelMinimo = nivelMinimo;
    }

    public void Info(string mensagem) => Escrever(NivelLog.INFO, mensagem);
    public void Warn(string mensagem) => Escrever(NivelLog.WARN, mensagem);
    public void Erro(string mensagem) => Escrever(NivelLog.ERRO, mensagem);

    private void Escrever(NivelLog nivel, string mensagem)
    {
        if (nivel < _nivelMinimo) return;

        lock (_trava)
        {
            try
            {
                Directory.CreateDirectory(_pasta);
                Rotacionar();

                var linha = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{nivel}] {mensagem}{Environment.NewLine}";
                File.AppendAllText(_arquivoAtivo, linha, Encoding.UTF8);
            }
            catch
            {
                // Falha de log (disco cheio, arquivo travado) nunca deve derrubar o app.
            }
        }
    }

    private void Rotacionar()
    {
        var arquivo = new FileInfo(_arquivoAtivo);
        if (!arquivo.Exists) return;

        var mudouODia = arquivo.LastWriteTime.Date != DateTime.Today;
        if (!mudouODia && arquivo.Length < _tamanhoMaximoBytes) return;

        var destino = Path.Combine(_pasta, $"jiraflow-{arquivo.LastWriteTime:yyyyMMdd-HHmmss-fff}.log");
        File.Move(_arquivoAtivo, destino);

        RemoverAntigos();
    }

    private void RemoverAntigos()
    {
        var excedentes = Directory.GetFiles(_pasta, PadraoRotacionados)
            .OrderByDescending(File.GetLastWriteTime)
            .Skip(_arquivosRetidos);

        foreach (var arquivo in excedentes)
            File.Delete(arquivo);
    }
}
