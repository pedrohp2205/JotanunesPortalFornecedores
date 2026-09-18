using AutoMapper;
using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests;

public class SupplyRequestServiceTest
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ISupplyRequestRepository> _repository = new();
    private readonly Mock<IDocumentComplianceService> _compliance = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly SupplyRequest _request = new(1, 1, SupplierType.ManpowerLabor, 3);
    private readonly SupplyRequestService _service;

    public SupplyRequestServiceTest()
    {
        _unitOfWork.SetupGet(u => u.SupplyRequestRepository).Returns(_repository.Object);
        _repository.Setup(r => r.GetById(It.IsAny<long>())).ReturnsAsync(_request);
        _service = new SupplyRequestService(_mapper.Object, _unitOfWork.Object, _compliance.Object);
    }

    private static OverdueSupplyRequestDto Pending(int onboarding = 0, int recurring = 0, int? required = null, int upToDate = 0)
    {
        return new OverdueSupplyRequestDto
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
        _compliance.Setup(c => c.GetPending(It.IsAny<long>())).ReturnsAsync((OverdueSupplyRequestDto?)null);

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
}
