using Mapster;
using MapsterMapper;

namespace WebApi.Compatibility
{
    /// <summary>
    /// Outer compatibility adapter for legacy services that still request the historical mapper contract.
    /// New API v1 application/domain code must not depend on this adapter or on Mapster directly.
    /// </summary>
    public sealed class LegacyObjectMapper : AutoMapper.IMapper
    {
        private readonly MapsterMapper.IMapper _mapper;

        public LegacyObjectMapper(MapsterMapper.IMapper mapper)
        {
            _mapper = mapper;
        }

        public TDestination Map<TDestination>(object source)
        {
            return _mapper.Map<TDestination>(source);
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            return _mapper.Map<TSource, TDestination>(source);
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Brownfield compatibility shim preserving the historical AddAutoMapper call site while
    /// the vulnerable AutoMapper package is removed. New code must not call this extension.
    /// </summary>
    public static class LegacyMappingServiceCollectionExtensions
    {
        public static IServiceCollection AddAutoMapper(this IServiceCollection services, Type legacyProfileMarker)
        {
            ArgumentNullException.ThrowIfNull(services);
            _ = legacyProfileMarker;

            var config = new TypeAdapterConfig();
            services.AddSingleton(config);
            services.AddScoped<MapsterMapper.IMapper, ServiceMapper>();
            services.AddScoped<AutoMapper.IMapper, WebApi.Compatibility.LegacyObjectMapper>();

            return services;
        }
    }
}
