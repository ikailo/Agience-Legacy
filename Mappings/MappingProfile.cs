using AutoMapper;
using System.Reflection;

namespace Agience.SDK.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            this.ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
