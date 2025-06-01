using AutoMapper;
using RezeptbuchAPI.Models;
using RezeptbuchAPI.Models.DTO;
using System.Collections.Generic;

namespace RezeptbuchAPI.Models.DTO
{
    public class RecipeMappingProfile : Profile
    {
        public RecipeMappingProfile()
        {
            CreateMap<InstructionXmlDto, Instruction>()
                .ConvertUsing(src => Instruction.FromXmlDto(src));

            CreateMap<RecipeXmlImport, Recipe>()
                .ForMember(dest => dest.Categories, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerUuid, opt => opt.Ignore())
                .ForMember(dest => dest.Hash, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Hash) ? System.Guid.NewGuid().ToString("N") : src.Hash))
                .ForMember(dest => dest.Instructions, opt => opt.MapFrom(src => src.Instructions ?? new List<InstructionXmlDto>()));
        }
    }
}