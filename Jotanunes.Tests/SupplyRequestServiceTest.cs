using AutoMapper;
using Jotanunes.Application.DTOs;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Application.DTOs.Users;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests;

public class SupplyRequestServiceTest
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISupplyRequestRepository> _repository = new();
    private readonly Mock<IDocumentComplianceService> _compliance = new();
    private readonly Mock<ISupplierNotificationService> _notifications = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<ISupplierUserRepository> _users = new();
    private readonly Mock<IWorkSiteRepository> _workSites = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly SupplyRequest _request = new(1, 1, SupplierType.ManpowerLabor, 3);
    private readonly SupplyRequestService _service;

    public SupplyRequestServiceTest()
    {
        _unitOfWork.SetupGet(u => u.SupplyRequestRepository).Returns(_repository.Object);
        _unitOfWork.SetupGet(u => u.CompanyRepository).Returns(_companies.Object);
        _unitOfWork.SetupGet(u => u.SupplierUserRepository).Returns(_users.Object);
        _unitOfWork.SetupGet(u => u.WorkSiteRepository).Returns(_workSites.Object);
        _repository.Setup(r => r.GetById(It.IsAny<long>())).ReturnsAsync(_request);
        _workSites.Setup(w => w.GetById(It.IsAny<long>())).ReturnsAsync(new WorkSite("Obra Teste"));
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _mapper.Setup(m => m.Map<SupplyRequestDto>(It.IsAny<object>()))
            .Returns<object>(source => new SupplyRequestDto { Id = ((SupplyRequest)source).Id });
        _compliance.Setup(c => c.GetPendingBatch(It.IsAny<IReadOnlyCollection<SupplyRequest>>()))
            .ReturnsAsync(new Dictionary<long, SupplyRequestPendingDto>());
        _service = new SupplyRequestService(_mapper.Object, _unitOfWork.Object, _compliance.Object, _notifications.Object, _passwordHasher.Object);
    }

    private static SupplyRequestPendingDto Pending(int onboarding = 0, int recurring = 0, int? required = null, int upToDate = 0)
    {
        return new SupplyRequestPendingDto
        {
            MissingOnboardingCount = onboarding,
            MissingRecurringCompanyCount = recurring,
            RequiredWorkerCount = required,
            WorkersUpToDate = upToDate,
            PeriodStart = new DateOnly(2026, 9, 16),
            PeriodEnd = new DateOnly(2026, 9, 30)
        };
    }

    [Fact]
    public async Task Should_Complete_When_Checklist_Has_No_Pending_Items()
    {
        _compliance.Setup(c => c.GetPending(It.IsAny<long>())).ReturnsAsync((SupplyRequestPendingDto?)null);

        await _service.Complete(1);

        Assert.Equal(SupplyRequestStatus.Completed, _request.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Block_Completion_And_Explain_What_Is_Missing()
    {
        _compliance.Setup(c => c.GetPending(It.IsAny<long>())).ReturnsAsync(Pending(recurring: 5, required: 3, upToDate: 1));

        var ex = await Assert.ThrowsAsync<JotanunesException>(() => _service.Complete(1));

        Assert.Equal(
            "Não é possível concluir a solicitação: faltam 5 documentos recorrentes da empresa e 2 trabalhadores em dia (meta 3, em dia 1) no período 16/09/2026 a 30/09/2026. Para encerrar com pendências, cancele a solicitação.",
            ex.Message);
        Assert.Equal(SupplyRequestStatus.Open, _request.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Should_Use_Singular_And_List_All_Kinds_Of_Pending_Items()
    {
        _compliance.Setup(c => c.GetPending(It.IsAny<long>())).ReturnsAsync(Pending(onboarding: 1, recurring: 1, required: 3, upToDate: 2));

        var ex = await Assert.ThrowsAsync<JotanunesException>(() => _service.Complete(1));

        Assert.Contains("faltam 1 documento de habilitação, 1 documento recorrente da empresa e 1 trabalhador em dia (meta 3, em dia 2)", ex.Message);
    }

    [Fact]
    public async Task Should_Report_Closed_Request_Before_Checking_Pending_Items()
    {
        _request.Cancel();
        _compliance.Setup(c => c.GetPending(It.IsAny<long>())).ReturnsAsync(Pending(recurring: 5));

        var ex = await Assert.ThrowsAsync<JotanunesException>(() => _service.Complete(1));

        Assert.Equal("Solicitação encerrada não aceita alterações nem novos documentos.", ex.Message);
        _compliance.Verify(c => c.GetPending(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Should_Cancel_Even_With_Pending_Items()
    {
        _compliance.Setup(c => c.GetPending(It.IsAny<long>())).ReturnsAsync(Pending(recurring: 5));

        await _service.Cancel(1);

        Assert.Equal(SupplyRequestStatus.Cancelled, _request.Status);
        _compliance.Verify(c => c.GetPending(It.IsAny<long>()), Times.Never);
    }

    private static SupplyRequestWithNewCompanyCreateDto NewCompanyModel(
        SupplierType companyType = SupplierType.Material | SupplierType.ManpowerLabor,
        SupplierType requestType = SupplierType.Material)
    {
        return new SupplyRequestWithNewCompanyCreateDto
        {
            Company = new CompanyCreateDto
            {
                Cnpj = "11.222.333/0001-81",
                CorporateName = "Construtora Alfa Ltda",
                TradeName = "Alfa",
                Email = "contato@alfa.com.br",
                Phone = "81999998888",
                ResponsibleName = "Maria Souza",
                Address = new AddressDto
                {
                    Street = "Rua A", Number = "10", Neighborhood = "Centro", City = "Recife", State = "PE", ZipCode = "50000000"
                },
                SupplierType = companyType
            },
            User = new SupplierUserCreateDto { Name = "Maria Souza", Email = "maria@alfa.com.br", TemporaryPassword = "Senha@1234" },
            WorkSiteId = 1,
            SupplierType = requestType
        };
    }

    [Fact]
    public async Task Should_Create_Company_User_And_Request_In_A_Single_Save_And_Notify()
    {
        Company? addedCompany = null;
        SupplyRequest? addedRequest = null;
        _companies.Setup(c => c.Add(It.IsAny<Company>())).Callback<Company>(c => addedCompany = c);
        _repository.Setup(r => r.Add(It.IsAny<SupplyRequest>())).Callback<SupplyRequest>(r => addedRequest = r);

        await _service.CreateWithNewCompany(NewCompanyModel());

        Assert.NotNull(addedCompany);
        Assert.Equal(CompanyStatus.PendingDocumentation, addedCompany!.Status);
        var user = Assert.Single(addedCompany.Users);
        Assert.Equal("maria@alfa.com.br", user.Email);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Same(addedCompany, addedRequest!.Company);
        Assert.Equal(SupplyRequestStatus.Open, addedRequest.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notifications.Verify(n => n.Welcome(user, "Senha@1234"), Times.Once);
        _notifications.Verify(n => n.SupplyRequestCreated(It.IsAny<SupplyRequest>()), Times.Once);
    }

    [Fact]
    public async Task Should_Block_New_Company_When_Cnpj_Already_Exists()
    {
        _companies.Setup(c => c.CnpjInUse(It.IsAny<string>(), null)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<JotanunesException>(() => _service.CreateWithNewCompany(NewCompanyModel()));

        Assert.Contains("Já existe uma empresa cadastrada com este CNPJ", ex.Message);
        AssertNothingSaved();
    }

    [Fact]
    public async Task Should_Block_New_Company_When_Work_Site_Does_Not_Exist()
    {
        _workSites.Setup(w => w.GetById(It.IsAny<long>())).ReturnsAsync((WorkSite?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateWithNewCompany(NewCompanyModel()));

        AssertNothingSaved();
    }

    [Fact]
    public async Task Should_Block_New_Company_When_User_Email_Is_In_Use()
    {
        _users.Setup(u => u.EmailInUse(It.IsAny<string>(), null)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<JotanunesException>(() => _service.CreateWithNewCompany(NewCompanyModel()));

        Assert.Equal("Já existe um usuário cadastrado com este e-mail.", ex.Message);
        AssertNothingSaved();
    }

    [Fact]
    public async Task Should_Block_New_Company_When_Request_Type_Is_Not_Supplied_By_Company()
    {
        var model = NewCompanyModel(companyType: SupplierType.Material, requestType: SupplierType.ManpowerLabor);

        var ex = await Assert.ThrowsAsync<JotanunesException>(() => _service.CreateWithNewCompany(model));

        Assert.Equal("A empresa não está cadastrada para fornecer este tipo de serviço.", ex.Message);
        AssertNothingSaved();
    }

    private void AssertNothingSaved()
    {
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notifications.Verify(n => n.Welcome(It.IsAny<SupplierUser>(), It.IsAny<string>()), Times.Never);
        _notifications.Verify(n => n.SupplyRequestCreated(It.IsAny<SupplyRequest>()), Times.Never);
    }

    private static SupplyRequest WithId(long id, SupplyRequestStatus? closeAs = null)
    {
        var request = new SupplyRequest(1, 1, SupplierType.ManpowerLabor, 3) { Id = id };
        if (closeAs == SupplyRequestStatus.Cancelled)
        {
            request.Cancel();
        }

        return request;
    }

    [Fact]
    public async Task Should_Attach_Pending_To_Each_Request_In_The_List()
    {
        var withPending = WithId(1);
        var upToDate = WithId(2);
        _repository.Setup(r => r.Get(It.IsAny<SupplyRequestFilter>())).ReturnsAsync(new List<SupplyRequest> { withPending, upToDate });
        _compliance.Setup(c => c.GetPendingBatch(It.IsAny<IReadOnlyCollection<SupplyRequest>>()))
            .ReturnsAsync(new Dictionary<long, SupplyRequestPendingDto> { [1] = Pending(recurring: 2) });

        var result = await _service.Get(new SupplyRequestFilter());

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Single(r => r.Id == 1).Pending!.MissingRecurringCompanyCount);
        Assert.Null(result.Single(r => r.Id == 2).Pending);
    }

    [Theory]
    [InlineData(true, new long[] { 1 })]
    [InlineData(false, new long[] { 2, 3 })]
    public async Task Should_Filter_List_By_Pending(bool hasPending, long[] expectedIds)
    {
        _repository.Setup(r => r.Get(It.IsAny<SupplyRequestFilter>()))
            .ReturnsAsync(new List<SupplyRequest> { WithId(1), WithId(2), WithId(3, SupplyRequestStatus.Cancelled) });
        _compliance.Setup(c => c.GetPendingBatch(It.IsAny<IReadOnlyCollection<SupplyRequest>>()))
            .ReturnsAsync(new Dictionary<long, SupplyRequestPendingDto> { [1] = Pending(onboarding: 1) });

        var result = await _service.Get(new SupplyRequestFilter { HasPending = hasPending });

        Assert.Equal(expectedIds, result.Select(r => r.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task Should_Attach_Pending_When_Getting_A_Single_Request()
    {
        _request.Id = 7;
        _compliance.Setup(c => c.GetPendingBatch(It.IsAny<IReadOnlyCollection<SupplyRequest>>()))
            .ReturnsAsync(new Dictionary<long, SupplyRequestPendingDto> { [7] = Pending(onboarding: 2) });

        var result = await _service.GetById(7);

        Assert.Equal(2, result.Pending!.MissingOnboardingCount);
    }
}
