using ApplicationCore.ValueObjects.Logs;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Repositories.Logs
{
    public interface ILogRepository
    {


        public Task<ResultObject> CrearLog(CrearLogVO Log);


    }
}