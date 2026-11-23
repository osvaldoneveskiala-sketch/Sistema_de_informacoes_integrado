using System.Text.Json;

var sistema = SistemaRepository.Carregar();

while (true)
{
    Console.Clear();
    Console.WriteLine("===============================================");
    Console.WriteLine(" SISTEMA INTEGRADO DE INFORMACAO DE SAUDE ");
    Console.WriteLine("===============================================");
    Console.WriteLine("1 - Registrar unidade de saúde");
    Console.WriteLine("2 - Registrar utente");
    Console.WriteLine("3 - Adicionar histórico clínico");
    Console.WriteLine("4 - Consultar utentes");
    Console.WriteLine("5 - Consultar unidades de saúde");
    Console.WriteLine("6 - Partilhar dados entre unidades");
    Console.WriteLine("7 - Coordenar serviço");
    Console.WriteLine("8 - Consultar coordenações");
    Console.WriteLine("0 - Sair");
    Console.Write("\nOpção: ");

    switch (Console.ReadLine())
    {
        case "1": RegistrarUnidade(sistema); break;
        case "2": RegistrarUtente(sistema); break;
        case "3": AdicionarHistorico(sistema); break;
        case "4": ConsultarUtentes(sistema); break;
        case "5": ConsultarUnidades(sistema); break;
        case "6": PartilharDados(sistema); break;
        case "7": CoordenarServico(sistema); break;
        case "8": ConsultarCoordenacoes(sistema); break;
        case "0":
            SistemaRepository.Guardar(sistema);
            return;
        default:
            MostrarMensagem("Opção inválida.");
            break;
    }
}

static void RegistrarUnidade(SistemaDados sistema)
{
    Console.Clear();
    Console.WriteLine("REGISTRAR UNIDADE DE SAÚDE");
    Console.Write("Nome: ");
    var nome = Console.ReadLine()?.Trim();
    Console.Write("Localização: ");
    var localizacao = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(localizacao))
    {
        MostrarMensagem("Nome e localização são obrigatórios.");
        return;
    }

    var unidade = new UnidadeSaude
    {
        Id = SistemaRepository.ProximoId(sistema.Unidades.Select(u => u.Id)),
        Nome = nome,
        Localizacao = localizacao
    };

    sistema.Unidades.Add(unidade);
    SistemaRepository.Guardar(sistema);
    MostrarMensagem($"Unidade registrada com o ID {unidade.Id}.");
}

static void RegistrarUtente(SistemaDados sistema)
{
    Console.Clear();
    Console.WriteLine("REGISTRAR UTENTE");
    Console.Write("Nome: ");
    var nome = Console.ReadLine()?.Trim();
    Console.Write("Documento: ");
    var documento = Console.ReadLine()?.Trim();
    Console.Write("Data de nascimento (AAAA-MM-DD): ");
    var dataTexto = Console.ReadLine()?.Trim();
    Console.Write("Contacto: ");
    var contacto = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(nome) ||
        string.IsNullOrWhiteSpace(documento) ||
        !DateOnly.TryParse(dataTexto, out var dataNascimento))
    {
        MostrarMensagem("Nome, documento e uma data de nascimento válida são obrigatórios.");
        return;
    }

    if (sistema.Utentes.Any(u =>
        u.Documento.Equals(documento, StringComparison.OrdinalIgnoreCase)))
    {
        MostrarMensagem("Já existe um utente com este documento.");
        return;
    }

    var utente = new Utente
    {
        Id = SistemaRepository.ProximoId(sistema.Utentes.Select(u => u.Id)),
        Nome = nome,
        Documento = documento,
        DataNascimento = dataNascimento,
        Contacto = contacto ?? ""
    };

    sistema.Utentes.Add(utente);
    SistemaRepository.Guardar(sistema);
    MostrarMensagem($"Utente registrado com o ID {utente.Id}.");
}

static void AdicionarHistorico(SistemaDados sistema)
{
    Console.Clear();
    Console.WriteLine("ADICIONAR HISTÓRICO CLÍNICO");
    var utente = SelecionarUtente(sistema);
    if (utente is null) return;

    var unidade = SelecionarUnidade(sistema);
    if (unidade is null) return;

    Console.Write("Diagnóstico ou descrição: ");
    var descricao = Console.ReadLine()?.Trim();
    Console.Write("Observações: ");
    var observacoes = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(descricao))
    {
        MostrarMensagem("A descrição do histórico é obrigatória.");
        return;
    }

    utente.HistoricoClinico.Add(new RegistoClinico
    {
        Id = SistemaRepository.ProximoId(utente.HistoricoClinico.Select(r => r.Id)),
        UnidadeId = unidade.Id,
        UnidadeNome = unidade.Nome,
        Data = DateTime.Now,
        Descricao = descricao,
        Observacoes = observacoes ?? ""
    });

    SistemaRepository.Guardar(sistema);
    MostrarMensagem("Registro clínico adicionado ao histórico.");
}

static void ConsultarUtentes(SistemaDados sistema)
{
    Console.Clear();
    Console.WriteLine("CONSULTAR UTENTES");
    Console.Write("Nome ou documento (Enter para listar todos): ");
    var pesquisa = Console.ReadLine()?.Trim();

    var resultados = sistema.Utentes
        .Where(u => string.IsNullOrWhiteSpace(pesquisa) ||
                    u.Nome.Contains(pesquisa, StringComparison.OrdinalIgnoreCase) ||
                    u.Documento.Contains(pesquisa, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (resultados.Count == 0)
    {
        MostrarMensagem("Nenhum utente encontrado.");
        return;
    }

    foreach (var utente in resultados)
    {
        Console.WriteLine($"\nID: {utente.Id}");
        Console.WriteLine($"Nome: {utente.Nome}");
        Console.WriteLine($"Documento: {utente.Documento}");
        Console.WriteLine($"Nascimento: {utente.DataNascimento:yyyy-MM-dd}");
        Console.WriteLine($"Contacto: {utente.Contacto}");

        if (utente.UnidadesComAcesso.Count > 0)
            Console.WriteLine("Dados partilhados com: " +
                string.Join(", ", utente.UnidadesComAcesso.Select(id =>
                    sistema.Unidades.FirstOrDefault(u => u.Id == id)?.Nome ?? $"Unidade {id}")));

        Console.WriteLine("Histórico clínico:");
        if (utente.HistoricoClinico.Count == 0)
        {
            Console.WriteLine("  Sem registros.");
        }
        else
        {
            foreach (var registro in utente.HistoricoClinico.OrderByDescending(r => r.Data))
            {
                Console.WriteLine($"  [{registro.Data:yyyy-MM-dd HH:mm}] Unidade: {registro.UnidadeNome}");
                Console.WriteLine($"  Descrição: {registro.Descricao}");
                if (!string.IsNullOrWhiteSpace(registro.Observacoes))
                    Console.WriteLine($"  Observações: {registro.Observacoes}");
            }
        }
    }

    Pausar();
}

static void ConsultarUnidades(SistemaDados sistema)
{
    Console.Clear();
    Console.WriteLine("UNIDADES DE SAÚDE");

    if (sistema.Unidades.Count == 0)
    {
        MostrarMensagem("Nenhuma unidade registrada.");
        return;
    }

    foreach (var unidade in sistema.Unidades)
    {
        Console.WriteLine($"ID: {unidade.Id}");
        Console.WriteLine($"Nome: {unidade.Nome}");
        Console.WriteLine($"Localização: {unidade.Localizacao}");
        Console.WriteLine();
    }

    Pausar();
}

static void PartilharDados(SistemaDados sistema)
{
    Console.Clear();
    Console.WriteLine("PARTILHAR DADOS ENTRE UNIDADES");
    var utente = SelecionarUtente(sistema);
    if (utente is null) return;

    var unidade = SelecionarUnidade(sistema);
    if (unidade is null) return;

    if (!utente.UnidadesComAcesso.Contains(unidade.Id))
        utente.UnidadesComAcesso.Add(unidade.Id);

    SistemaRepository.Guardar(sistema);
    MostrarMensagem($"Dados de {utente.Nome} partilhados com {unidade.Nome}.");
}

static void CoordenarServico(SistemaDados sistema)
{
    Console.Clear();
    Console.WriteLine("COORDENAR SERVIÇO");
    var utente = SelecionarUtente(sistema);
    if (utente is null) return;

    var unidadeOrigem = SelecionarUnidade(sistema);
    if (unidadeOrigem is null) return;

    Console.WriteLine("\nSelecione a unidade de destino:");
    var unidadeDestino = SelecionarUnidade(sistema);
    if (unidadeDestino is null) return;

    Console.Write("Serviço solicitado: ");
    var servico = Console.ReadLine()?.Trim();
    Console.Write("Observação: ");
    var observacao = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(servico))
    {
        MostrarMensagem("O serviço solicitado é obrigatório.");
        return;
    }

    sistema.Coordenacoes.Add(new CoordenacaoServico
    {
        Id = SistemaRepository.ProximoId(sistema.Coordenacoes.Select(c => c.Id)),
        UtenteId = utente.Id,
        UtenteNome = utente.Nome,
        UnidadeOrigemId = unidadeOrigem.Id,
        UnidadeOrigemNome = unidadeOrigem.Nome,
        UnidadeDestinoId = unidadeDestino.Id,
        UnidadeDestinoNome = unidadeDestino.Nome,
        Servico = servico,
        Observacao = observacao ?? "",
        Estado = "Agendado",
        Data = DateTime.Now
    });

    SistemaRepository.Guardar(sistema);
    MostrarMensagem("Coordenação de serviço registrada.");
}

static void ConsultarCoordenacoes(SistemaDados sistema)
{
    Console.Clear();
    Console.WriteLine("COORDENAÇÃO DE SERVIÇOS");

    if (sistema.Coordenacoes.Count == 0)
    {
        MostrarMensagem("Nenhuma coordenação registrada.");
        return;
    }

    foreach (var coordenacao in sistema.Coordenacoes.OrderByDescending(c => c.Data))
    {
        Console.WriteLine($"ID: {coordenacao.Id}");
        Console.WriteLine($"Data: {coordenacao.Data:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"Utente: {coordenacao.UtenteNome} (ID {coordenacao.UtenteId})");
        Console.WriteLine($"Origem: {coordenacao.UnidadeOrigemNome}");
        Console.WriteLine($"Destino: {coordenacao.UnidadeDestinoNome}");
        Console.WriteLine($"Serviço: {coordenacao.Servico}");
        Console.WriteLine($"Estado: {coordenacao.Estado}");
        if (!string.IsNullOrWhiteSpace(coordenacao.Observacao))
            Console.WriteLine($"Observação: {coordenacao.Observacao}");
        Console.WriteLine();
    }

    Pausar();
}

static Utente? SelecionarUtente(SistemaDados sistema)
{
    if (sistema.Utentes.Count == 0)
    {
        MostrarMensagem("Não existem utentes registrados.");
        return null;
    }

    Console.Write("ID do utente: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        MostrarMensagem("ID inválido.");
        return null;
    }

    var utente = sistema.Utentes.FirstOrDefault(u => u.Id == id);
    if (utente is null) MostrarMensagem("Utente não encontrado.");
    return utente;
}

static UnidadeSaude? SelecionarUnidade(SistemaDados sistema)
{
    if (sistema.Unidades.Count == 0)
    {
        MostrarMensagem("Não existem unidades registradas.");
        return null;
    }

    Console.Write("ID da unidade: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        MostrarMensagem("ID inválido.");
        return null;
    }

    var unidade = sistema.Unidades.FirstOrDefault(u => u.Id == id);
    if (unidade is null) MostrarMensagem("Unidade não encontrada.");
    return unidade;
}

static void MostrarMensagem(string mensagem)
{
    Console.WriteLine($"\n{mensagem}");
    Pausar();
}

static void Pausar()
{
    Console.WriteLine("\nPressione Enter para continuar...");
    Console.ReadLine();
}

sealed class SistemaDados
{
    public List<UnidadeSaude> Unidades { get; set; } = new();
    public List<Utente> Utentes { get; set; } = new();
    public List<CoordenacaoServico> Coordenacoes { get; set; } = new();
}

sealed class UnidadeSaude
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Localizacao { get; set; } = "";
}

sealed class Utente
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Documento { get; set; } = "";
    public DateOnly DataNascimento { get; set; }
    public string Contacto { get; set; } = "";
    public List<int> UnidadesComAcesso { get; set; } = new();
    public List<RegistoClinico> HistoricoClinico { get; set; } = new();
}

sealed class RegistoClinico
{
    public int Id { get; set; }
    public int UnidadeId { get; set; }
    public string UnidadeNome { get; set; } = "";
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = "";
    public string Observacoes { get; set; } = "";
}

sealed class CoordenacaoServico
{
    public int Id { get; set; }
    public int UtenteId { get; set; }
    public string UtenteNome { get; set; } = "";
    public int UnidadeOrigemId { get; set; }
    public string UnidadeOrigemNome { get; set; } = "";
    public int UnidadeDestinoId { get; set; }
    public string UnidadeDestinoNome { get; set; } = "";
    public string Servico { get; set; } = "";
    public string Observacao { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime Data { get; set; }
}

static class SistemaRepository
{
    private static readonly string PastaDados =
        Path.Combine(AppContext.BaseDirectory, "dados");
    private static readonly string CaminhoArquivo =
        Path.Combine(PastaDados, "sistema.json");

    public static SistemaDados Carregar()
    {
        if (!File.Exists(CaminhoArquivo))
            return new SistemaDados();

        var json = File.ReadAllText(CaminhoArquivo);
        return JsonSerializer.Deserialize<SistemaDados>(json)
            ?? new SistemaDados();
    }

    public static void Guardar(SistemaDados sistema)
    {
        Directory.CreateDirectory(PastaDados);
        var json = JsonSerializer.Serialize(sistema,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(CaminhoArquivo, json);
    }

    public static int ProximoId(IEnumerable<int> ids)
    {
        var lista = ids.ToList();
        return lista.Count == 0 ? 1 : lista.Max() + 1;
    }
}
