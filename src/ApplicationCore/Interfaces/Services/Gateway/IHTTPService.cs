using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services
{
    public interface IHTTPService
    {
        /// <summary>
        /// Realizar una solicitud HTTP y devolver la respuesta como <c>HttpResponseMessage</c>.
        /// </summary>
        /// <param name="method">Método HTTP</param>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="data">Datos del cuerpo de la solicitud</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<HttpResponseMessage> RequestRaw(HttpMethod method, string endpoint, string host = null, HttpContent data = null, IDictionary<string, string> headers = null);

        /// <summary>
        /// Realizar una solicitud HTTP y devolver la respuesta como <c>ResultObject</c>.
        /// </summary>
        /// <param name="method">Método HTTP</param>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="data">Datos del cuerpo de la solicitud</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<ResultObject> Request(HttpMethod method, string endpoint, string host = null, HttpContent data = null, IDictionary<string, string> headers = null);

        /// <summary>
        /// Realizar una solicitud HTTP de tipo GET y devolver la respuesta como <c>ResultObject</c>.
        /// </summary>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<ResultObject> Get(string endpoint, string host = null, IDictionary<string, string> headers = null);

        /// <summary>
        /// Realizar una solicitud HTTP de tipo GET y devolver la respuesta como <c>T</c>.
        /// </summary>
        /// <typeparam name="T"><c>Value Object</c></typeparam>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<T> Get<T>(string endpoint, string host = null, IDictionary<string, string> headers = null) where T : class;

        /// <summary>
        /// Realizar una solicitud HTTP de tipo GET y devolver la respuesta paginada.
        /// </summary>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<PagedResult<object>> GetAsPagedResult(string endpoint, string host = null, IDictionary<string, string> headers = null);

        /// <summary>
        /// Realizar una solicitud HTTP de tipo GET y devolver la respuesta paginada como <c>T</c>.
        /// </summary>
        /// <typeparam name="T"><c>Value Object</c></typeparam>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<PagedResult<T>> GetAsPagedResult<T>(string endpoint, string host = null, IDictionary<string, string> headers = null) where T : class;

        /// <summary>
        /// Realizar una solicitud HTTP de tipo POST y devolver la respuesta como <c>ResultObject</c>.
        /// </summary>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="data">Datos del cuerpo de la solicitud</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<ResultObject> Post(string endpoint, string host = null, HttpContent data = null, IDictionary<string, string> headers = null);

        /// <summary>
        /// Realizar una solicitud HTTP de tipo PUT y devolver la respuesta como <c>ResultObject</c>.
        /// </summary>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="data">Datos del cuerpo de la solicitud</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<ResultObject> Put(string endpoint, string host = null, HttpContent data = null, IDictionary<string, string> headers = null);

        /// <summary>
        /// Realizar una solicitud HTTP de tipo DELETE y devolver la respuesta como <c>ResultObject</c>.
        /// </summary>
        /// <param name="endpoint">Ruta del gateway</param>
        /// <param name="headers">Cabeceras personalizadas</param>
        /// <returns></returns>
        public Task<ResultObject> Delete(string endpoint, string host = null, IDictionary<string, string> headers = null);
    }
}