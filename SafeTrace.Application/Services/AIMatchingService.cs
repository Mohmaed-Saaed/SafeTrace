using Microsoft.Extensions.Logging;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

namespace SafeTrace.Application.Services
{
    public class AIMatchingService : IAIMatchingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly ILogger<AIMatchingService> _logger;
        public AIMatchingService(IUnitOfWork unitOfWork, IFaceRecognitionService faceRecognitionService, ILogger<AIMatchingService> logger)
        {
            _unitOfWork = unitOfWork;
            _faceRecognitionService = faceRecognitionService;
            _logger = logger;
        }
    }
}