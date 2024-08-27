using AuthAPI.Models;
using AuthAPI.Models.Dto;
using AutoMapper;

namespace AuthAPI.Mapping
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<UserDto, ApplicationUser>().ReverseMap();
            });
            return mappingConfig;
        }
    }
}
