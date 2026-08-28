using ApplicationCore.ValueObjects.Result;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Utils
{
    public static class EFPagination
    {
        public static async Task<PagedResult<T>> GetPagedAsNoTracking<T>(this IQueryable<T> query, int page, int pageSize) where T : class
        {
            var result = new PagedResult<T>
            {
                CurrentPage = page,
                PageSize = pageSize,
                RowCount = await query.CountAsync()
            };

            var pageCount = (double)result.RowCount / pageSize;
            result.PageCount = (int)Math.Ceiling(pageCount);

            var skip = (page - 1) * pageSize;
            result.Results = await query.AsNoTracking()
                                        .Skip(skip)
                                        .Take(pageSize)
                                        .ToListAsync();

            return result;
        }

        public static async Task<PagedResult<T>> GetPaged<T>(this IQueryable<T> query, int page, int pageSize) where T : class
        {
            var result = new PagedResult<T>
            {
                CurrentPage = page,
                PageSize = pageSize,
                RowCount = await query.CountAsync()
            };

            var pageCount = (double)result.RowCount / pageSize;
            result.PageCount = (int)Math.Ceiling(pageCount);

            var skip = (page - 1) * pageSize;
            result.Results = await query.Skip(skip)
                                        .Take(pageSize)
                                        .ToListAsync();

            return result;
        }

        public static async Task<PagedResult<U>> GetPagedAndMap<T, U>(this IQueryable<T> query, int page, int pageSize, IMapper mapper) where U : class
        {
            var result = new PagedResult<U>
            {
                CurrentPage = page,
                PageSize = pageSize,
                RowCount = await query.CountAsync()
            };

            var pageCount = (double)result.RowCount / pageSize;
            result.PageCount = (int)Math.Ceiling(pageCount);

            var skip = (page - 1) * pageSize;
            result.Results = await query.Skip(skip)
                                        .Take(pageSize)
                                        .ProjectTo<U>(mapper.ConfigurationProvider)
                                        .ToListAsync();

            return result;
        }
    }
}