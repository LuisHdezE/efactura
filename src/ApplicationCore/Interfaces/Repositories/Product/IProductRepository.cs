using ApplicationCore.ValueObjects.Product;

namespace ApplicationCore.Interfaces.Repositories.Product
{
    public interface IProductServices
    {
        public Task<GetProductVO> GetById(int id);

        public Task<IEnumerable<ListProductVO>> GetProductsPaginated(int page, int RowsPerPage);
        
        public Task Create(CreateProductVO product);
        
        public Task Update(UpdateProductVO product);
       
        public Task Delete(int id);
    }

}
