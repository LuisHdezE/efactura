using ApplicationCore.Entites;
using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.TaxType;
using ApplicationCore.Interfaces.Services.TaxType;
using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.TaxType;
using AutoMapper;

namespace ApplicationCore.Services.TaxType
{
    public class TaxTypeService : ITaxTypeService
    {
        private readonly IMapper _mapper;

        private readonly ITaxTypeRepository _taxTypeRepository;

        public TaxTypeService(ITaxTypeRepository taxTypeRepository, IMapper mapper)
        {
            _mapper = mapper;
            _taxTypeRepository = taxTypeRepository;
        }

        public async Task<ResultObject> Create(CreateTaxTypeVO taxTypeVO)
        {
            try
            {
                await _taxTypeRepository.Create(taxTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Tax Type created successfully",
                    Data = taxTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Tax Type",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_TAX_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _taxTypeRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Tax Type deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Tax Type",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_TAX_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var taxTypes = await _taxTypeRepository.GetAll();
                var taxTypesVO = _mapper.Map<IEnumerable<ListTaxTypeVO>>(taxTypes);

                return new ResultObject
                {
                    Status = true,
                    Message = "Tax Type retrieved successfully",
                    Data = taxTypesVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Tax Type",
                    Detail = ex.Message,
                    ErrorCode = "GET_TAX_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetByIdAsync(int id)
        {
            try
            {
                var taxType = await _taxTypeRepository.GetById(id);
                var taxTypeVO = _mapper.Map<GetTaxTypeVO>(taxType);

                return new ResultObject
                {
                    Status = true,
                    Message = "Tax Type retrieved successfully",
                    Data = taxTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving TAX Type",
                    Detail = ex.Message,
                    ErrorCode = "GET_TAX_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateTaxTypeVO taxTypeVO)
        {
            try
            {
                var taxType = _mapper.Map<TaxTypes>(taxTypeVO);
                await _taxTypeRepository.Update(taxTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Tax Type updated successfully",
                    Data = taxType
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Tax type",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_Tax_TYPE_ERROR"
                };
            }
        }
    }
}
