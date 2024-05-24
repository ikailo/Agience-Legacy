using Agience.SDK.Mappings;
using AutoMapper;

namespace Agience.SDK
{
    public static class AutoMapperConfig
    {
        private static IMapper? _mapper;

        public static IMapper GetMapper()
        {
            if (_mapper == null)
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<MappingProfile>(); // Assuming MappingProfile contains all the mappings
                });
                _mapper = config.CreateMapper();
            }

            return _mapper;
        }
    }
}
