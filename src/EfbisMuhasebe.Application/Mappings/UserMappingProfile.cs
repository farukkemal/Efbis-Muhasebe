using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;

namespace EfbisMuhasebe.Application.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}
