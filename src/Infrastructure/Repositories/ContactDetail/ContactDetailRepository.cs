using ApplicationCore.Interfaces.Repositories.ContactDetail;
using ApplicationCore.ValueObjects.ContactDetail;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories
{
    public class ContactDetailRepository : IContactDetailRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "ContactDetails";
        private const string _idColumn = "Id";

        public ContactDetailRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task<GetContactDetailVO> GetById(int id)
        {
            const string query = "SELECT * FROM \"ContactDetails\" WHERE id = @Id";
            return await _connection.QuerySingleOrDefaultAsync<GetContactDetailVO>(query, new { Id = id });
        }

        public async Task Create(CreateContactDetailVO contactDetail)
        {
            const string query = "INSERT INTO \"ContactDetails\" (customer_id, contact_type_id, detail) VALUES (@CustomerId, @ContactTypeId, @Detail)";
            await _connection.ExecuteAsync(query, contactDetail);
        }

        public async Task Update(UpdateContactDetailVO contactDetail)
        {
            const string query = "UPDATE \"ContactDetails\" SET detail = @Detail WHERE id = @Id";
            await _connection.ExecuteAsync(query, contactDetail);
        }

        public async Task Delete(int id)
        {
            const string query = "DELETE FROM \"ContactDetails\" WHERE id = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListContactDetailVO>> GetAll()
        {
            const string query = "SELECT * FROM \"ContactDetails\"";
            return await _connection.QueryAsync<ListContactDetailVO>(query);
        }

        public async Task<IEnumerable<GetContactDetailVO>> GetByCustomerId(long customerId)
        {
            const string query = "SELECT * FROM \"ContactDetails\" WHERE customer_id = @CustomerId";
            return await _connection.QueryAsync<GetContactDetailVO>(query, new { CustomerId = customerId });
        }

        public async Task<IEnumerable<GetContactDetailVO>> GetByContactTypeId(long contactTypeId)
        {
            const string query = "SELECT * FROM \"ContactDetails\" WHERE contact_type_id = @ContactTypeId";
            return await _connection.QueryAsync<GetContactDetailVO>(query, new { ContactTypeId = contactTypeId });
        }

        public async Task<GetContactDetailVO> GetByCustomerIdAndContactTypeId(long customerId, long contactTypeId)
        {
            const string query = "SELECT * FROM \"ContactDetails\" WHERE customer_id = @CustomerId AND contact_type_id = @ContactTypeId";
            return await _connection.QuerySingleOrDefaultAsync<GetContactDetailVO>(query, new { CustomerId = customerId, ContactTypeId = contactTypeId });
        }
    }
}
