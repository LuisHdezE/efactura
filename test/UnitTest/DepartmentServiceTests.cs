using ApplicationCore.Interfaces.Repositories.Department;
using ApplicationCore.Interfaces.Services.Department;
using ApplicationCore.Services.Department;
using ApplicationCore.ValueObjects.Department;
using AutoMapper;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace ApplicationCore.Tests.Services
{
    public class DepartmentServiceTests
    {
        private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly IDepartmentService _departmentService;

        public DepartmentServiceTests()
        {
            _departmentRepositoryMock = new Mock<IDepartmentRepository>();
            _mapperMock = new Mock<IMapper>();
            _departmentService = new DepartmentService(_departmentRepositoryMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task GetById_Returns_Department_Successfully()
        {
            // Arrange
            var departmentId = 1;
            var departmentVO = new GetDepartmentVO { Id = departmentId, Name = "TestDepartment", CountryId = 2 };
            _departmentRepositoryMock.Setup(repo => repo.GetById(departmentId)).ReturnsAsync(departmentVO);

            // Act
            var result = await _departmentService.GetById(departmentId);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Department retrieved successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.IsType<GetDepartmentVO>(result.Data);
        }
    }
}
