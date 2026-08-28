using ApplicationCore.Interfaces.Repositories.Country;
using ApplicationCore.Interfaces.Services.Country;
using ApplicationCore.Services.CountryService;
using ApplicationCore.ValueObjects.Country;
using AutoMapper;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ApplicationCore.Tests.Services
{
    public class CountryTests
    {
        private readonly Mock<ICountryRepository> _countryRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ICountryService _countryService;

        public CountryTests()
        {
            _countryRepositoryMock = new Mock<ICountryRepository>();
            _mapperMock = new Mock<IMapper>();
            _countryService = new CountryService(_countryRepositoryMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task GetById_Returns_Country_Successfully()
        {
            // Arrange
            var countryId = 1;
            var countryVO = new GetCountryByIdVO { Id = countryId, Name = "TestCountry", Code = "TC" };
            _countryRepositoryMock.Setup(repo => repo.GetById(countryId)).ReturnsAsync(countryVO);

            // Act
            var result = await _countryService.GetById(countryId);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Country retrieved successfully", result.Message);
            //Assert.NotNull(result.Data);
           // Assert.IsType<GetCountryByIdVO>(result.Data);
        }

        [Fact]
        public async Task GetById_Returns_Error_When_Country_Not_Found()
        {
            // Arrange
            var countryId = 999;
            _countryRepositoryMock.Setup(repo => repo.GetById(countryId)).ThrowsAsync(new Exception("Country not found"));

            // Act
            var result = await _countryService.GetById(countryId);

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error retrieving country", result.Message);
            Assert.Equal("GET_COUNTRY_ERROR", result.ErrorCode);
        }

        [Fact]
        public async Task GetAll_Returns_Countries_Successfully()
        {
            // Arrange
            var countriesList = new List<ListCountryVO> { new ListCountryVO { Name = "Country1", Code = "C1" } };
            _countryRepositoryMock.Setup(repo => repo.GetAll()).ReturnsAsync(countriesList);

            // Act
            var result = await _countryService.GetAll();

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Countries retrieved successfully", result.Message);
            Assert.IsAssignableFrom<IEnumerable<ListCountryVO>>(result.Data);
        }

        [Fact]
        public async Task CreateCountry_Successfully()
        {
            // Arrange
            var countryVO = new CreateCountryVO { Name = "NewCountry", Code = "NC" };
            _countryRepositoryMock.Setup(repo => repo.Create(countryVO)).Returns(Task.CompletedTask);

            // Act
            var result = await _countryService.Create(countryVO);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Country created successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.IsType<CreateCountryVO>(result.Data);
        }

        [Fact]
        public async Task CreateCountry_Returns_Error()
        {
            // Arrange
            var countryVO = new CreateCountryVO { Name = "NewCountry", Code = "NC" };
            _countryRepositoryMock.Setup(repo => repo.Create(countryVO)).ThrowsAsync(new Exception("Create error"));

            // Act
            var result = await _countryService.Create(countryVO);

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error creating country", result.Message);
            Assert.Equal("CREATE_COUNTRY_ERROR", result.ErrorCode);
        }

        [Fact]
        public async Task UpdateCountry_Successfully()
        {
            // Arrange
            var updateCountryVO = new UpdateCountryVO { Id = 1, Name = "UpdatedCountry", Code = "UC" };
            _countryRepositoryMock.Setup(repo => repo.Update(updateCountryVO)).Returns(Task.CompletedTask);

            // Act
            var result = await _countryService.Update(updateCountryVO);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Country updated successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.IsType<UpdateCountryVO>(result.Data);
        }
        
        [Fact]
        public async Task UpdateCountry_Returns_Error()
        {
            // Arrange
            var updateCountryVO = new UpdateCountryVO { Id = 1, Name = "UpdatedCountry", Code = "UC" };
            _countryRepositoryMock.Setup(repo => repo.Update(updateCountryVO)).ThrowsAsync(new Exception("Update error"));

            // Act
            var result = await _countryService.Update(updateCountryVO);

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error updating country", result.Message);
            Assert.Equal("UPDATE_COUNTRY_ERROR", result.ErrorCode);
        }

        [Fact]
        public async Task DeleteCountry_Successfully()
        {
            // Arrange
            var countryId = 1;
            _countryRepositoryMock.Setup(repo => repo.Delete(countryId)).Returns(Task.CompletedTask);

            // Act
            var result = await _countryService.Delete(countryId);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Country deleted successfully", result.Message);
        }

        [Fact]
        public async Task DeleteCountry_Returns_Error()
        {
            // Arrange
            var countryId = 1;
            _countryRepositoryMock.Setup(repo => repo.Delete(countryId)).ThrowsAsync(new Exception("Delete error"));

            // Act
            var result = await _countryService.Delete(countryId);

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error deleting country", result.Message);
            Assert.Equal("DELETE_COUNTRY_ERROR", result.ErrorCode);
        }
    }
}
