using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests;

public class CompanyTest
{
    private static Address ValidAddress()
    {
        return new Address("Rua Sao Cristovao", "123", "Centro", "Aracaju", "SE", "49000-000", "Sala 2");
    }

    private static Company ValidCompany()
    {
        return new Company(
            "11.222.333/0001-81",
            "Construtora Exemplo LTDA",
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "(79) 99999-8888",
            "Maria Souza",
            ValidAddress());
    }

    [Fact]
    public void Should_Create_Company_Normalizing_Cnpj_And_Phone()
    {
        var company = ValidCompany();

        Assert.Equal("11222333000181", company.Cnpj);
        Assert.Equal("79999998888", company.Phone);
        Assert.Equal("contato@exemplo.com.br", company.Email);
        Assert.Equal(CompanyStatus.PendingDocumentation, company.Status);
    }

    [Fact]
    public void Should_Throw_Exception_When_Cnpj_Is_Invalid()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Company(
            "11222333000182",
            "Construtora Exemplo LTDA",
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "79999998888",
            "Maria Souza",
            ValidAddress()));

        Assert.Equal("CNPJ inválido.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Email_Is_Invalid()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Company(
            "11222333000181",
            "Construtora Exemplo LTDA",
            "Construtora Exemplo",
            "contato-exemplo",
            "79999998888",
            "Maria Souza",
            ValidAddress()));

        Assert.Equal("E-mail deve conter '@' e '.'", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Phone_Has_Wrong_Length()
    {
        var ex = Assert.Throws<JotanunesException>(() => new Company(
            "11222333000181",
            "Construtora Exemplo LTDA",
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "99998888",
            "Maria Souza",
            ValidAddress()));

        Assert.Equal("Telefone deve ter 10 ou 11 dígitos (com DDD).", ex.Message);
    }

    [Fact]
    public void Should_Update_Company_Keeping_Cnpj()
    {
        var company = ValidCompany();

        company.Update(
            "Construtora Nova LTDA",
            "Construtora Nova",
            "novo@exemplo.com.br",
            "7933332222",
            "Joao Lima",
            ValidAddress());

        Assert.Equal("11222333000181", company.Cnpj);
        Assert.Equal("Construtora Nova LTDA", company.CorporateName);
        Assert.Equal("novo@exemplo.com.br", company.Email);
    }

    [Fact]
    public void Should_Throw_Exception_When_Adding_User_With_Duplicated_Email()
    {
        var company = ValidCompany();
        company.AddUser(new SupplierUser(1, "Maria Souza", "maria@exemplo.com.br", "hash"));

        var ex = Assert.Throws<JotanunesException>(() =>
            company.AddUser(new SupplierUser(1, "Maria S.", "maria@exemplo.com.br", "hash")));

        Assert.Equal("Já existe um usuário com este e-mail nesta empresa.", ex.Message);
    }
}
