using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace Biblioteca;

// ========================= ENTIDADE (SRP) =========================
public class Livro
{
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public bool Disponivel { get; set; } = true;
    public DateTime? DataEmprestimo { get; set; }
    public string EmailUsuario { get; set; } = string.Empty;
}

// ========================= MULTA =========================
public interface ICalculadoraMulta
{
    decimal Calcular(DateTime? dataEmprestimo);
}

public class CalculadoraMulta : ICalculadoraMulta
{
    public decimal Calcular(DateTime? data)
    {
        if (!data.HasValue) return 0;

        var dias = (DateTime.Now - data.Value).Days;
        return dias > 14 ? (dias - 14) * 2.5m : 0;
    }
}

// ========================= DESCONTOS (OCP) =========================
public interface IDesconto
{
    decimal Aplicar(decimal multa);
}

public class DescontoEstudante : IDesconto
{
    public decimal Aplicar(decimal m) => m * 0.5m;
}

public class DescontoProfessor : IDesconto
{
    public decimal Aplicar(decimal m) => m * 0.8m;
}

public class SemDesconto : IDesconto
{
    public decimal Aplicar(decimal m) => m;
}

// ========================= ITENS DO ACERVO (LSP) =========================
public interface IEmprestavel
{
    string Titulo { get; set; }
    bool Disponivel { get; set; }
    void Emprestar(string usuario);
    void Devolver();
}

public interface IReservavel
{
    void ReservarItem(string usuario);
}

public class LivroFisico : IEmprestavel, IReservavel
{
    public string Titulo { get; set; } = string.Empty;
    public bool Disponivel { get; set; } = true;

    public void Emprestar(string usuario)
    {
        Disponivel = false;
        Console.WriteLine($"[FÍSICO] '{Titulo}' emprestado para {usuario}.");
    }

    public void Devolver()
    {
        Disponivel = true;
        Console.WriteLine($"[FÍSICO] '{Titulo}' devolvido.");
    }

    public void ReservarItem(string usuario)
    {
        Console.WriteLine($"[FÍSICO] '{Titulo}' reservado para {usuario} por 3 dias.");
    }
}

public class EbookEmprestavel : IEmprestavel
{
    public string Titulo { get; set; } = string.Empty;
    public bool Disponivel { get; set; } = true;

    public void Emprestar(string usuario)
    {
        Disponivel = false;
        Console.WriteLine($"[EBOOK] Link de download enviado para {usuario}.");
    }

    public void Devolver()
    {
        Disponivel = true;
        Console.WriteLine($"[EBOOK] Acesso revogado.");
    }
}

// ========================= REPOSITÓRIO (DIP) =========================
public interface IRepositorioLivro
{
    void Salvar(Livro livro);
    List<Livro> BuscarDisponiveis();
}

public class BancoDados : IRepositorioLivro
{
    public void Salvar(Livro livro)
    {
        Console.WriteLine($"[DB] Salvando: {livro.Titulo}");
    }

    public List<Livro> BuscarDisponiveis()
    {
        Console.WriteLine("[DB] Buscando livros disponíveis...");
        return new List<Livro>();
    }
}

// ========================= NOTIFICAÇÃO =========================
public interface INotificacao
{
    void Enviar(string para, string mensagem);
}

public class EmailSmtp : INotificacao
{
    public void Enviar(string para, string mensagem)
    {
        Console.WriteLine($"[EMAIL] Para: {para} | Msg: {mensagem}");
    }
}

// ========================= RELATÓRIOS (ISP) =========================
public interface IGerarPdf { void Gerar(); }
public interface IGerarExcel { void Gerar(); }
public interface IGerarHtml { void Gerar(); }
public interface IEnviarRelatorio { void Enviar(string destino); }
public interface ISalvarRelatorio { void Salvar(string caminho); }

public class RelatorioEmprestimos : IGerarPdf, IEnviarRelatorio
{
    public void Gerar() => Console.WriteLine("Gerando PDF de empréstimos...");
    public void Enviar(string destino) => Console.WriteLine($"Enviando relatório para {destino}");
}

public class RelatorioInventario : IGerarExcel, ISalvarRelatorio
{
    public void Gerar() => Console.WriteLine("Gerando Excel de inventário...");
    public void Salvar(string caminho) => Console.WriteLine($"Salvando em {caminho}");
}

// ========================= GERENCIADOR =========================
public class GerenciadorAcervo
{
    private readonly IRepositorioLivro _repo;

    public GerenciadorAcervo(IRepositorioLivro repo)
    {
        _repo = repo;
    }

    public void Cadastrar(Livro livro)
    {
        _repo.Salvar(livro);
        Console.WriteLine($"Livro '{livro.Titulo}' cadastrado.");
    }

    public List<Livro> ListarDisponiveis()
    {
        return _repo.BuscarDisponiveis();
    }
}

// ========================= SERVIÇO =========================
public class ServicoEmprestimo(
    IRepositorioLivro repo,
    INotificacao notificacao,
    ICalculadoraMulta calculadora)
{
    public void Emprestar(Livro livro, string email)
    {
        if (!livro.Disponivel)
        {
            Console.WriteLine("Livro indisponível.");
            return;
        }

        livro.Disponivel = false;
        livro.DataEmprestimo = DateTime.Now;
        livro.EmailUsuario = email;
        repo.Salvar(livro);

        Console.WriteLine($"Empréstimo realizado: {livro.Titulo}");
    }

    public void Devolver(Livro livro, IDesconto desconto)
    {
        var multaFinal = desconto.Aplicar(calculadora.Calcular(livro.DataEmprestimo));

        livro.Disponivel = true;
        livro.DataEmprestimo = null;
        repo.Salvar(livro);

        if (multaFinal > 0)
        {
            Console.WriteLine($"Devolução com multa: R${multaFinal}");
            notificacao.Enviar(livro.EmailUsuario, $"Multa: R${multaFinal}");
        }
        else
        {
            Console.WriteLine("Devolução sem multa.");
        }
    }
}

// ========================= PROGRAMA =========================
public class Program
{
    public static void Main()
    {
        var repo = new BancoDados();
        var email = new EmailSmtp();
        var multa = new CalculadoraMulta();

        var servico = new ServicoEmprestimo(repo, email, multa);
        var gerenciador = new GerenciadorAcervo(repo);

        var livro = new Livro
        {
            Titulo = "Clean Code",
            Autor = "Robert C. Martin",
            Disponivel = true
        };

        gerenciador.Cadastrar(livro);
        servico.Emprestar(livro, "user@email.com");
        livro.DataEmprestimo = DateTime.Now.AddDays(-20);
        servico.Devolver(livro, new DescontoEstudante());

        var relatorio = new RelatorioEmprestimos();
        relatorio.Gerar();
        relatorio.Enviar("admin@email.com");
    }
}

// Justificativas para as mudanças: 

// SRP: A classe Livro agora possui apenas a responsabilidade de manter os dados (estado). 
// As regras de negócio (multa, persistência, e-mail) foram delegadas para outras classes especializadas.

// OCP: Utilizando a interface IDesconto, podemos adicionar novos tipos de desconto (como Idoso, VIP) c
// riando novas classes, sem precisar modificar o código existente (fechado para modificação, aberto para extensão).

// LSP: A classe base "ItemAcervo" forçava Ebooks a implementarem reservas físicas. 
// Agora temos interfaces focadas. Ebooks não precisam implementar IReservavel, 
// garantindo que não lancem "NotSupportedException" e respeitem o contrato (Liskov).

// ISP: A interface IRelatorio foi segregada em interfaces menores. 
// Nenhuma classe é forçada a implementar métodos de exportação (Excel, HTML) que não utiliza.

// DIP: O sistema agora depende de abstrações (IRepositorioLivro, INotificacao) 
// e não de implementações concretas (BancoDadosMySQL, ServicoEmailSMTP). 
// Isso facilita testes e a troca de tecnologias.