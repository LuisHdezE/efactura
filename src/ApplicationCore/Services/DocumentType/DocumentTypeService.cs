using ApplicationCore.Interfaces.Repositories.DocumentType;
using ApplicationCore.Interfaces.Services.DocumentType;
using ApplicationCore.ValueObjects.DocumentType;
using ApplicationCore.ValueObjects.Result;
using AutoMapper;

namespace ApplicationCore.Services.DocumentType
{
    public class DocumentTypeService : IDocumentTypeService
    {
        private readonly IDocumentTypeRepository _documentTypeRepository;
        private readonly IMapper _mapper;

        public DocumentTypeService(IDocumentTypeRepository documentTypeRepository, IMapper mapper)
        {
            _documentTypeRepository = documentTypeRepository;
            _mapper = mapper;
        }

        public async Task<ResultObject> Create(CreateDocumentTypeVO createDocumentTypeVO)
        {
            try
            {
                await _documentTypeRepository.Create(createDocumentTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Document Type created successfully",
                    Data = createDocumentTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Document Type",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_DOCUMENT_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _documentTypeRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Document type deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting document type",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_DOCUMENT_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var documentTypes = await _documentTypeRepository.GetAll();
                var documentTypeVO = _mapper.Map<IEnumerable<ListDocumentTypeVO>>(documentTypes);

                return new ResultObject
                {
                    Status = true,
                    Message = "Document Type retrieved successfully",
                    Data = documentTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving document type",
                    Detail = ex.Message,
                    ErrorCode = "GET_DOCUMENT_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var documentType = await _documentTypeRepository.GetById(id);
                var documentTypeVO = _mapper.Map<GetDocumentTypeVO>(documentType);

                return new ResultObject
                {
                    Status = true,
                    Message = "Document Type retrieved successfully",
                    Data = documentTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Document Type",
                    Detail = ex.Message,
                    ErrorCode = "GET_DOCUMENT_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateDocumentTypeVO documentTypeVO)
        {
            try
            {
                await _documentTypeRepository.Update(documentTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Document Type updated successfully",
                    Data = documentTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Document Type",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_DOCUMENT_TYPE_ERROR"
                };
            }
        }
    }
}
