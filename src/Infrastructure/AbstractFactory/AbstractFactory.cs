using ApplicationCore.Interfaces.AbstractFactory;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.AbstractFactory
{
    public class AbstractFactory : IAbstractFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public AbstractFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        //public IDemoEntityRepositoryConFactory GetDemoEntityRepository(string provider = null)
        //{
        //    provider = !string.IsNullOrEmpty(provider) ? provider : _configuration["PreferedContext:DemoEntity"];

        //    if (String.IsNullOrEmpty(provider))
        //    {
        //        throw new ProviderNotFoundException(Messages.ProveedorNoEspecificado + "DemoEntity");
        //    }

        //    if (provider == AppConstants.DemoEntityDapperRepository)
        //        return (IDemoEntityRepositoryConFactory)_serviceProvider.GetService(typeof(DemoEntityDapperRepository));
        //    else if (provider == AppConstants.DemoEntityHttpRepository)
        //        return (IDemoEntityRepositoryConFactory)_serviceProvider.GetService(typeof(DemoEntityHttpRepository));
        //    else if (provider == AppConstants.DemoEntityEFRepository)
        //        return (IDemoEntityRepositoryConFactory)_serviceProvider.GetService(typeof(DemoEntityEFRepository));
        //    else
        //        throw new ProviderNotFoundException(Messages.ProveedorNoEspecificado + "DemoEntity");
        //}
    }
}