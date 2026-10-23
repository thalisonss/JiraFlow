using JiraFlow.Application.Models;
using JiraFlow.Application.Ports;

namespace JiraFlow.Application.UseCases;

/// <summary>
/// Caso de uso: busca os chamados da JQL, move cada um para o status alvo,
/// atribui a quem está executando e comenta (cada etapa final é opcional).
/// Depende só das portas (IJiraGateway, ILogger), nunca de HTTP ou de arquivo.
/// </summary>
public sealed class ProcessarChamadosUseCase
{
    private enum ResultadoChamado { Movido, SemTransicao, Falha }

    private readonly IJiraGateway _jira;
    private readonly ILogger _log;
    private readonly ProcessamentoOptions _opcoes;

    public ProcessarChamadosUseCase(IJiraGateway jira, ILogger log, ProcessamentoOptions opcoes)
    {
        _jira = jira;
        _log = log;
        _opcoes = opcoes;
    }

    public async Task<ResumoProcessamento> ExecutarAsync()
    {
        // A autenticação também devolve o accountId do dono do token,
        // usado para atribuir os chamados movidos a quem está executando.
        var autenticacao = await _jira.AutenticarAsync();
        if (!autenticacao.Sucesso)
        {
            _log.Erro($"Autenticação falhou no Jira ({autenticacao.StatusHttp}). Verifique e-mail e token.");
            return new ResumoProcessamento(0, 0, 0, 1);
        }

        var encontrados = 0;
        var movidos = 0;
        var semTransicao = 0;
        var falhas = 0;

        try
        {
            var chaves = await _jira.BuscarChavesAsync(_opcoes.Jql);
            encontrados = chaves.Count;
            _log.Info($"{encontrados} issue(s) encontrada(s).");

            foreach (var chave in chaves)
            {
                try
                {
                    switch (await ProcessarChamadoAsync(chave, autenticacao.Valor!))
                    {
                        case ResultadoChamado.Movido: movidos++; break;
                        case ResultadoChamado.SemTransicao: semTransicao++; break;
                        default: falhas++; break;
                    }
                }
                catch (Exception ex)
                {
                    falhas++;
                    _log.Erro($"{chave}: erro inesperado: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            falhas++;
            _log.Erro($"Erro geral: {ex}");
        }

        _log.Info($"Resumo: {encontrados} encontrado(s), {movidos} movido(s), {semTransicao} sem transição, {falhas} falha(s).");
        return new ResumoProcessamento(encontrados, movidos, semTransicao, falhas);
    }

    private async Task<ResultadoChamado> ProcessarChamadoAsync(string chave, string meuAccountId)
    {
        // 1. Obter a transição que leva ao status alvo
        var transicao = await _jira.BuscarTransicaoAsync(chave, _opcoes.StatusDestino);
        if (!transicao.Sucesso)
        {
            _log.Erro($"{chave}: falha ao buscar transições ({transicao.StatusHttp}).");
            return ResultadoChamado.Falha;
        }

        if (transicao.Valor == null)
        {
            _log.Warn($"{chave}: sem transição disponível para '{_opcoes.StatusDestino}'.");
            return ResultadoChamado.SemTransicao;
        }

        // 2. Mover o chamado
        var mover = await _jira.MoverAsync(chave, transicao.Valor);
        if (!mover.Sucesso)
        {
            _log.Erro($"{chave}: falha ao mover ({mover.StatusHttp}) {mover.Detalhe}");
            return ResultadoChamado.Falha;
        }

        _log.Info($"{chave}: movido para '{_opcoes.StatusDestino}'.");

        // 3. Atribuir (opcional). Se falhar, o comentário não é enviado.
        if (_opcoes.AtribuirParaMim)
        {
            var atribuir = await _jira.AtribuirAsync(chave, meuAccountId);
            if (!atribuir.Sucesso)
            {
                _log.Erro($"{chave}: falha ao atribuir ({atribuir.StatusHttp}) {atribuir.Detalhe}");
                return ResultadoChamado.Movido;
            }

            _log.Info($"{chave}: atribuído ao usuário do token.");
        }

        // 4. Comentar (opcional: só se houver texto configurado)
        if (!string.IsNullOrWhiteSpace(_opcoes.Comentario))
        {
            var comentar = await _jira.ComentarAsync(chave, _opcoes.Comentario, _opcoes.ComentarioInterno);
            var tipo = _opcoes.ComentarioInterno ? "interno" : "público";

            if (comentar.Sucesso)
                _log.Info($"{chave}: comentário adicionado ({tipo}).");
            else
                _log.Erro($"{chave}: falha ao comentar ({comentar.StatusHttp}) {comentar.Detalhe}");
        }

        return ResultadoChamado.Movido;
    }
}
