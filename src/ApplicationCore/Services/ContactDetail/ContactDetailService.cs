using ApplicationCore.Interfaces.Repositories.ContactDetail;
using ApplicationCore.Interfaces.Services.ContactDetail;
using ApplicationCore.ValueObjects.ContactDetail;
using ApplicationCore.ValueObjects.Result;
using AutoMapper;

namespace ApplicationCore.Services
{
    public class ContactDetailService : IContactDetailService
    {
        private readonly IContactDetailRepository _contactDetailRepository;
        private readonly IMapper _mapper;

        public ContactDetailService(IContactDetailRepository contactDetailRepository, IMapper mapper)
        {
            _contactDetailRepository = contactDetailRepository;
            _mapper = mapper;
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var contactDetail = await _contactDetailRepository.GetById(id);
                var contactDetailVO = _mapper.Map<GetContactDetailVO>(contactDetail);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Detail retrieved successfully",
                    Data = contactDetailVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Contact Detail",
                    Detail = ex.Message,
                    ErrorCode = "GET_CONTACT_DETAIL_ERROR"
                };

            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var contactDetail = await _contactDetailRepository.GetAll();
                var contactDetailVO = _mapper.Map<IEnumerable<ListContactDetailVO>>(contactDetail);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Detail retrieved successfully",
                    Data = contactDetailVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Contact Detail",
                    Detail = ex.Message,
                    ErrorCode = "GET_CONTACT_DETAIL_ERROR"
                };
            }
        }

        public async Task<ResultObject> Create(CreateContactDetailVO contactDetailVO)
        {
            try
            {
                await _contactDetailRepository.Create(contactDetailVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Detail created successfully",
                    Data = contactDetailVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Contact Detai",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_CONTACT_DETAIL_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateContactDetailVO contactDetailVO)
        {
            try
            {
                await _contactDetailRepository.Update(contactDetailVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact detail updated successfully",
                    Data = contactDetailVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Contact detail",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_CONTACT_DETAIL_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _contactDetailRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact detail deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Contact detail",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_CONTACT_DETAIL_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetByCustomerIdAsync(int customerId)
        {
            try
            {
                var contactDetail = await _contactDetailRepository.GetById(customerId);
                var contactDetailVO = _mapper.Map<GetContactDetailVO>(contactDetail);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Detail retrieved successfully",
                    Data = contactDetailVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Contact Detail",
                    Detail = ex.Message,
                    ErrorCode = "GET_CONTACT_DETAIL_ERROR"
                };

            }
        }

        public async Task<ResultObject> GetByCustomerIdAndContactTypeId(long customerId, long contactTypeId)
        {
            try
            {
                var contactDetail = await _contactDetailRepository.GetByCustomerIdAndContactTypeId(customerId, contactTypeId);
                var contactDetailVO = _mapper.Map<GetContactDetailVO>(contactDetail);

                return new ResultObject
                {
                    Status = true,
                    Message = "Contact Detail retrieved successfully",
                    Data = contactDetailVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Contact Detail",
                    Detail = ex.Message,
                    ErrorCode = "GET_CONTACT_DETAIL_ERROR"
                };

            }
        }
    }
}
