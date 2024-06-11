using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Agience.SDK.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // SDK.Host -> Models.Host: AutoMapperAtribute
            // SDK.Agent -> Models.Agent: AutoMapperAtribute
            // SDK.Agency -> Models.Agency: AutoMapperAtribute

            // Add complex mappings here.
            // We want to keep it all internal to the SDK, so we shouldn't use public Interfaces.
        }
    }
}
