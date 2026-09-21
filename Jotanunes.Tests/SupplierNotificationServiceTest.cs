using Jotanunes.Application.DTOs.Notifications;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Application.Settings;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Jotanunes.Tests;

public class SupplierNotificationServiceTest
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISupplierUserRepository> _users = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Company _company;
    private readonly SupplierNotificationService _service;

    public SupplierNotificationServiceTest()
    {
        _unitOfWork.SetupGet(u => u.SupplierUserRepository).Returns(_users.Object);
        _users.Setup(r => r.GetByCompany(It.IsAny<long>())).ReturnsAsync(new List<SupplierUser>());

        _company = new Company(
            "11.222.333/0001-81",
            "Construtora Exemplo LTDA",
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "(79) 99999-8888",
            "Maria Souza",
            new Address("Rua Sao Cristovao", "123", "Centro", "Aracaju", "SE", "49000-000", "Sala 2"),
            SupplierType.Material);

        _service = new SupplierNotificationService(
            _unitOfWork.Object,
            _emailSender.Object,
            Options.Create(new NotificationSettings { PortalUrl = "https://portal.exemplo.com" }),
            NullLogger<SupplierNotificationService>.Instance);
    }

    private static SupplierUser User(string email, bool active = true)
    {
        var user = new SupplierUser(1, "Fulano", email, "hash");
        if (!active)
        {
            user.Deactivate();
        }

        return user;
    }

    private static Document RejectedDocument(Company company, string reason)
    {
        var document = new Document(
            1, 1, 1,
            DocumentCategory.Onboarding,
            DocumentSubject.Company,
            "companies/1/documents/abc-cnpj.pdf",
            "cnpj.pdf",
            "application/pdf");
        document.Reject(reason);

        typeof(Document).GetProperty(nameof(Document.Company))!.SetValue(document, company);
        var type = new DocumentType("CNPJ", "Cartão CNPJ", DocumentCategory.Onboarding, SupplierType.Material, DocumentSubject.Company);
        typeof(Document).GetProperty(nameof(Document.DocumentType))!.SetValue(document, type);

        return document;
    }

    [Fact]
    public async Task Should_Notify_Company_Email_And_Active_Users_Without_Duplicates()
    {
        _users.Setup(r => r.GetByCompany(It.IsAny<long>())).ReturnsAsync(new List<SupplierUser>
        {
            User("contato@exemplo.com.br"),
            User("ativo@exemplo.com.br"),
            User("inativo@exemplo.com.br", active: false)
        });

        EmailMessage? sent = null;
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, _) => sent = m)
            .Returns(Task.CompletedTask);

        await _service.CompanyEligible(_company);

        Assert.NotNull(sent);
        Assert.Equal(new[] { "contato@exemplo.com.br", "ativo@exemplo.com.br" }, sent!.To);
        Assert.Contains("Habilitação concluída", sent.Subject);
        Assert.Contains("https://portal.exemplo.com", sent.HtmlBody);
    }

    [Fact]
    public async Task Should_Include_Rejection_Reason_And_Encode_Html()
    {
        EmailMessage? sent = null;
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, _) => sent = m)
            .Returns(Task.CompletedTask);

        await _service.DocumentRejected(RejectedDocument(_company, "Ilegível <b>borrado</b>"));

        Assert.NotNull(sent);
        Assert.Contains("Documento rejeitado: Cartão CNPJ", sent!.Subject);
        Assert.Contains("Ilegível &lt;b&gt;borrado&lt;/b&gt;", sent.HtmlBody);
        Assert.DoesNotContain("<b>borrado</b>", sent.HtmlBody);
        Assert.Contains("Ilegível <b>borrado</b>", sent.TextBody);
    }

    [Fact]
    public async Task Should_Not_Throw_When_Email_Sender_Fails()
    {
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Resend indisponível"));

        await _service.CompanyEligible(_company);

        _emailSender.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Send_Welcome_Only_To_The_New_User_With_Temporary_Password()
    {
        EmailMessage? sent = null;
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, _) => sent = m)
            .Returns(Task.CompletedTask);

        await _service.Welcome(User("novo@exemplo.com.br"), "Temp#1234");

        Assert.NotNull(sent);
        Assert.Equal(new[] { "novo@exemplo.com.br" }, sent!.To);
        Assert.Contains("Temp#1234", sent.TextBody);
        _users.Verify(r => r.GetByCompany(It.IsAny<long>()), Times.Never);
    }
}
