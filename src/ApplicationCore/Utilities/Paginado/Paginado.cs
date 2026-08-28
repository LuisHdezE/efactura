using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Utilities.Paginado
{
    public static class Paginado
    {
        public static PagedResult<T> CargarPaginado<T>(IEnumerable<T> data, int cantidadRegistrosPorPagina = 0, int total = 0, int pagina = 0) where T : class
        {
            return new PagedResult<T>
            {
                CurrentPage = pagina,
                PageSize = cantidadRegistrosPorPagina,
                RowCount = total,
                PageCount = cantidadRegistrosPorPagina == 0 ? 0 : total / cantidadRegistrosPorPagina,
                Results = data ?? new List<T>(),
            };
        }
    }
}