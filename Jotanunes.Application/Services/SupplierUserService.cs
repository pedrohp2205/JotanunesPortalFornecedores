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

    public SupplierUserService(IMapper mapper, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
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
        Company? company = await _unitOfWork.CompanyRepository.GetById(companyId);
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

        return _mapper.Map<SupplierUserDto>(user);
    }
}
