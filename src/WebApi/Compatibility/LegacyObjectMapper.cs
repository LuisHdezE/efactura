using MapsterMapper;

namespace WebApi.Compatibility;

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
}
