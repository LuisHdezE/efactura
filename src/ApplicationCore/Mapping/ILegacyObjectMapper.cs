namespace AutoMapper
{
    /// <summary>
    /// Temporary brownfield compatibility contract retained while legacy services are migrated
    /// away from the historical AutoMapper dependency. New v1 code must not depend on this type.
    /// </summary>
    public interface IMapper
    {
        TDestination Map<TDestination>(object source);
    }
}
