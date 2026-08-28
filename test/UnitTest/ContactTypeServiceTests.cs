using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.ContactTypeRepository;
using ApplicationCore.Interfaces.Services.ContactType;
using ApplicationCore.Services.ContactTypeService;
using ApplicationCore.ValueObjects.ContactType;
using AutoMapper;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ApplicationCore.Tests.Services
{
    public class ContactTypeTests
    {
        private readonly Mock<IContactTypeRepository> _mockContactTypeRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly IContactTypeService _contactTypeService;

        public ContactTypeTests()
        {
            _mockContactTypeRepository = new Mock<IContactTypeRepository>();
            _mockMapper = new Mock<IMapper>();
            _contactTypeService = new ContactTypeService(_mockContactTypeRepository.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task Create_ShouldReturnSuccess_WhenContactTypeIsCreated()
        {
            // Arrange
            var createVO = new CreateContactTypeVO { Name = "Email" };
            _mockContactTypeRepository.Setup(repo => repo.Create(It.IsAny<CreateContactTypeVO>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _contactTypeService.Create(createVO);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Contact Type created successfully", result.Message);
            _mockContactTypeRepository.Verify(repo => repo.Create(It.IsAny<CreateContactTypeVO>()), Times.Once);
        }

        [Fact]
        public async Task Create_ShouldReturnError_WhenExceptionIsThrown()
        {
            // Arrange
            var createVO = new CreateContactTypeVO { Name = "Email" };
            _mockContactTypeRepository.Setup(repo => repo.Create(It.IsAny<CreateContactTypeVO>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _contactTypeService.Create(createVO);

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error creating Contact Type", result.Message);
            Assert.Equal("Database error", result.Detail);
            _mockContactTypeRepository.Verify(repo => repo.Create(It.IsAny<CreateContactTypeVO>()), Times.Once);
        }

        [Fact]
        public async Task Delete_ShouldReturnSuccess_WhenContactTypeIsDeleted()
        {
            // Arrange
            int id = 1;
            _mockContactTypeRepository.Setup(repo => repo.Delete(id))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _contactTypeService.Delete(id);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Contact Type deleted successfully", result.Message);
            _mockContactTypeRepository.Verify(repo => repo.Delete(id), Times.Once);
        }

        [Fact]
        public async Task Delete_ShouldReturnError_WhenExceptionIsThrown()
        {
            // Arrange
            int id = 1;
            _mockContactTypeRepository.Setup(repo => repo.Delete(id))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _contactTypeService.Delete(id);

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error deleting Contact Type", result.Message);
            Assert.Equal("Database error", result.Detail);
            _mockContactTypeRepository.Verify(repo => repo.Delete(id), Times.Once);
        }

        [Fact]
        public async Task GetAll_ShouldReturnSuccess_WhenContactTypesAreRetrieved()
        {
            // Arrange
            var contactTypes = new List<ListContactTypeVO> { new ListContactTypeVO { Id = 1, Name = "Email" } };
            _mockContactTypeRepository.Setup(repo => repo.GetAll())
                .ReturnsAsync(contactTypes);

            var mappedContactTypes = new List<ListContactTypeVO> { new ListContactTypeVO { Id = 1, Name = "Email" } };
            _mockMapper.Setup(mapper => mapper.Map<IEnumerable<ListContactTypeVO>>(contactTypes))
                .Returns(mappedContactTypes);

            // Act
            var result = await _contactTypeService.GetAll();

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Contact Type retrieved successfully", result.Message);
            Assert.Equal(mappedContactTypes, result.Data);
            _mockContactTypeRepository.Verify(repo => repo.GetAll(), Times.Once);
        }

        [Fact]
        public async Task GetAll_ShouldReturnError_WhenExceptionIsThrown()
        {
            // Arrange
            _mockContactTypeRepository.Setup(repo => repo.GetAll())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _contactTypeService.GetAll();

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error retrieving Contact Type", result.Message);
            Assert.Equal("Database error", result.Detail);
            _mockContactTypeRepository.Verify(repo => repo.GetAll(), Times.Once);
        }

        [Fact]
        public async Task GetById_ShouldReturnSuccess_WhenContactTypeIsRetrieved()
        {
            // Arrange
            int id = 1;
            var contactType = new GetContactTypeVO { Id = id, Name = "Email" };
            _mockContactTypeRepository.Setup(repo => repo.GetById(id))
                .ReturnsAsync(contactType);

            _mockMapper.Setup(mapper => mapper.Map<GetContactTypeVO>(contactType))
                .Returns(contactType);

            // Act
            var result = await _contactTypeService.GetById(id);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Contact Type retrieved successfully", result.Message);
            Assert.Equal(contactType, result.Data);
            _mockContactTypeRepository.Verify(repo => repo.GetById(id), Times.Once);
        }

        [Fact]
        public async Task GetById_ShouldReturnError_WhenExceptionIsThrown()
        {
            // Arrange
            int id = 1;
            _mockContactTypeRepository.Setup(repo => repo.GetById(id))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _contactTypeService.GetById(id);

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error retrieving Contact Type", result.Message);
            Assert.Equal("Database error", result.Detail);
            _mockContactTypeRepository.Verify(repo => repo.GetById(id), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnSuccess_WhenContactTypeIsUpdated()
        {
            // Arrange
            var updateVO = new UpdateContactTypeVO { Id = 1, Name = "Phone" };
            var contactType = new ContactType { ContactTypeId = 1, ContactTypeName = "Phone" };
            _mockMapper.Setup(mapper => mapper.Map<ContactType>(updateVO))
                .Returns(contactType);
            _mockContactTypeRepository.Setup(repo => repo.Update(updateVO))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _contactTypeService.Update(updateVO);

            // Assert
            Assert.True(result.Status);
            Assert.Equal("Contact Type updated successfully", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(contactType, result.Data);
            _mockContactTypeRepository.Verify(repo => repo.Update(updateVO), Times.Once);
        }

        [Fact]
        public async Task Update_ShouldReturnError_WhenExceptionIsThrown()
        {
            // Arrange
            var updateVO = new UpdateContactTypeVO { Id = 1, Name = "Phone" };
            _mockContactTypeRepository.Setup(repo => repo.Update(updateVO))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _contactTypeService.Update(updateVO);

            // Assert
            Assert.False(result.Status);
            Assert.Equal("Error updating contact type", result.Message);
            Assert.Equal("Database error", result.Detail);
            _mockContactTypeRepository.Verify(repo => repo.Update(updateVO), Times.Once);
        }
    }
}

