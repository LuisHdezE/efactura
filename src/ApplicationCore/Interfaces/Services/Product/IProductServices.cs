using ApplicationCore.ValueObjects.Product;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.Product
{
    public interface IProductServices
    {
        public Task<ResultObject> GetById(int id);

        public Task<ResultObject> GetProductsPaginated(int page, int RowsPerPage);

        public Task<ResultObject> Create(CreateProductVO product);
        
        public Task<ResultObject> Update(UpdateProductVO product);
       
        public Task<ResultObject> Delete(int id);
    }

}
