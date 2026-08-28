using ApplicationCore.Interfaces.Repositories.ContactTypeRepository;
using ApplicationCore.Interfaces.Services.ContactType;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.Result;
using ApplicationCore.Entities;
using AutoMapper;

namespace ApplicationCore.Services.ContactTypeService
{
    public class ContactTypeService : IContactTypeService
    {
        private readonly IMapper _mapper;

        private readonly IContactTypeRepository _contactTypeRepository;

        public ContactTypeService(IContactTypeRepository contactTypeRepository, IMapper mapper)
        {
            _contactTypeRepository = contactTypeRepository;
            _mapper = mapper;
        }

        public async Task<ResultObject> Create(CreateContactTypeVO contactTypeVO)
        {
            try
            {
                await _contactTypeRepository.Create(contactTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Type created successfully",
                    Data = contactTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Contact Type",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_CONTACT_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _contactTypeRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Type deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Contact Type",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_CONTACT_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var contactTypes = await _contactTypeRepository.GetAll();
                var contactTypesVO = _mapper.Map<IEnumerable<ListContactTypeVO>>(contactTypes);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Type retrieved successfully",
                    Data = contactTypesVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Contact Type",
                    Detail = ex.Message,
                    ErrorCode = "GET_CONTACT_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var contactType = await _contactTypeRepository.GetById(id);
                var contactTypeVO = _mapper.Map<GetContactTypeVO>(contactType);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Type retrieved successfully",
                    Data = contactTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Contact Type",
                    Detail = ex.Message,
                    ErrorCode = "GET_CONTACT_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateContactTypeVO contactTypeVo)
        {
            try
            {
                var contactType = _mapper.Map<ContactType>(contactTypeVo);
                await _contactTypeRepository.Update(contactTypeVo);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Type updated successfully",
                    Data = contactType
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating contact type",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_CONTACT_TYPE_ERROR"
                };
            }
        }
    }
}
