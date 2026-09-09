using AutoMapper;

namespace Jotanunes.Application.DTOs.Mapping;

// O mapeamento é usado apenas no sentido entidade -> DTO (leitura).
// A escrita passa pelos construtores e métodos do domínio, para que as
// regras de negócio das entidades não sejam contornadas pelo mapper.
public class MappingProfile : Profile
{
    public MappingProfile()
    {
    }
}
