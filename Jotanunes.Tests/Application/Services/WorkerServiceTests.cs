using AutoMapper;
using Jotanunes.Application.DTOs.Workers;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;
using Moq;

namespace Jotanunes.Tests.Application.Services;

public class WorkerServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWorkerRepository> _workers = new();
    private readonly Mock<IWorkerAllocationRepository> _allocations = new();
    private readonly Mock<ISupplyRequestRepository> _requests = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Worker _worker = new(1, "Cicero Fernandes da Silva", "529.982.247-25") { Id = 7 };
    private readonly SupplyRequest _request = new(1, 1, SupplierType.ManpowerLabor, 2) { Id = 10 };
    private readonly WorkerService _service;

    public WorkerServiceTests()
    {
        _unitOfWork.SetupGet(u => u.WorkerRepository).Returns(_workers.Object);
        _unitOfWork.SetupGet(u => u.WorkerAllocationRepository).Returns(_allocations.Object);
        _unitOfWork.SetupGet(u => u.SupplyRequestRepository).Returns(_requests.Object);
        _unitOfWork.SetupGet(u => u.CompanyRepository).Returns(_companies.Object);
        _workers.Setup(w => w.GetById(7)).ReturnsAsync(_worker);
        _requests.Setup(r => r.GetById(10)).ReturnsAsync(_request);

        _service = new WorkerService(new Mock<IMapper>().Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Should_Not_Create_Worker_With_Cpf_Already_In_The_Company()
    {
        _companies.Setup(c => c.GetById(1)).ReturnsAsync(new Mock<Company>().Object);
        _workers.Setup(w => w.CpfInUse(1, "52998224725")).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<JotanunesException>(() =>
            _service.Create(1, new WorkerCreateDto { Name = "Cicero", Cpf = "529.982.247-25" }));

        Assert.Equal("Já existe um trabalhador cadastrado com este CPF.", ex.Message);
        _workers.Verify(w => w.Add(It.IsAny<Worker>()), Times.Never);
    }

    [Fact]
    public async Task Should_Allocate_Worker_When_There_Is_Room()
    {
        _allocations.Setup(a => a.CountActive(10)).ReturnsAsync(1);

        await _service.Allocate(10, 1, new WorkerAllocateDto { WorkerId = 7 });

        _allocations.Verify(a => a.Add(It.Is<WorkerAllocation>(x => x.WorkerId == 7 && x.SupplyRequestId == 10)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Allocate_When_Request_Is_Full()
    {
        _allocations.Setup(a => a.CountActive(10)).ReturnsAsync(2);

        await Assert.ThrowsAsync<JotanunesException>(() => _service.Allocate(10, 1, new WorkerAllocateDto { WorkerId = 7 }));

        _allocations.Verify(a => a.Add(It.IsAny<WorkerAllocation>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Allocate_Same_Worker_Twice()
    {
        _allocations.Setup(a => a.GetActive(10, 7)).ReturnsAsync(new WorkerAllocation(_request, _worker, 0));

        var ex = await Assert.ThrowsAsync<JotanunesException>(() => _service.Allocate(10, 1, new WorkerAllocateDto { WorkerId = 7 }));

        Assert.Equal("Trabalhador já está alocado nesta solicitação.", ex.Message);
    }

    [Fact]
    public async Task Should_Not_Expose_Worker_Or_Request_Of_Another_Company()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetById(7, companyId: 2));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.Allocate(10, 2, new WorkerAllocateDto { WorkerId = 7 }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.Release(10, 7, companyId: 2));
    }

    [Fact]
    public async Task Should_Release_Allocations_Of_Active_Requests_When_Worker_Is_Deactivated()
    {
        var active = new WorkerAllocation(_request, _worker, 0);
        var closedRequest = new SupplyRequest(1, 2, SupplierType.ManpowerLabor) { Id = 11 };
        var fromClosedRequest = new WorkerAllocation(closedRequest, _worker, 0);
        closedRequest.Complete();
        _allocations.Setup(a => a.GetActiveByWorker(7)).ReturnsAsync(new List<WorkerAllocation> { active, fromClosedRequest });

        await _service.Deactivate(7, 1);

        Assert.False(_worker.Active);
        Assert.False(active.IsActive);
        Assert.True(fromClosedRequest.IsActive);
    }

    [Fact]
    public async Task Should_Not_Release_Worker_Not_Allocated()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.Release(10, 7));
    }
}
