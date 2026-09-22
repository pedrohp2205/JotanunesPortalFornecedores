using AutoMapper;
using Jotanunes.Application.DTOs.Users;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;

namespace Jotanunes.Application.Services;

// Provisionamento dos acessos ao Portal do Fornecedor, feito pela frente interna.
public class SupplierUserService : ISupplierUserService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISupplierNotificationService _notificationService;

    public SupplierUserService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ISupplierNotificationService notificationService)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _notificationService = notificationService;
    }

    public async Task<List<SupplierUserDto>> GetByCompany(long companyId)
    {
        Company? company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        List<SupplierUser> users = await _unitOfWork.SupplierUserRepository.GetByCompany(companyId);
        return _mapper.Map<List<SupplierUserDto>>(users);
    }

    public async Task<SupplierUserDto> Create(long companyId, SupplierUserCreateDto model)
    {
        Company? company = await _unitOfWork.CompanyRepository.GetByIdWithUsers(companyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        JotanunesException.When(
            await _unitOfWork.SupplierUserRepository.EmailInUse(model.Email),
            "Já existe um usuário cadastrado com este e-mail.");

        var user = new SupplierUser(
            companyId,
            model.Name,
            model.Email,
            _passwordHasher.Hash(model.TemporaryPassword));

        company.AddUser(user);

        _unitOfWork.CompanyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.Welcome(user, model.TemporaryPassword);

        return _mapper.Map<SupplierUserDto>(user);
    }

    public async Task<SupplierUserDto> Activate(long companyId, long userId)
    {
        var user = await GetUser(companyId, userId);

        user.Activate();

        _unitOfWork.SupplierUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SupplierUserDto>(user);
    }

    public async Task<SupplierUserDto> Deactivate(long companyId, long userId)
    {
        var user = await GetUser(companyId, userId);

        user.Deactivate();

        _unitOfWork.SupplierUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SupplierUserDto>(user);
    }

    public async Task<SupplierUserDto> ResetPassword(long companyId, long userId, SupplierUserResetPasswordDto model)
    {
        var user = await GetUser(companyId, userId);

        user.ResetPassword(_passwordHasher.Hash(model.TemporaryPassword));

        _unitOfWork.SupplierUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.TemporaryPasswordIssued(user, model.TemporaryPassword);

        return _mapper.Map<SupplierUserDto>(user);
    }

    private async Task<SupplierUser> GetUser(long companyId, long userId)
    {
        SupplierUser? user = await _unitOfWork.SupplierUserRepository.GetById(userId);
        if (user is null || user.CompanyId != companyId)
        {
            throw new KeyNotFoundException("Usuário não encontrado");
        }

        return user;
    }
}
