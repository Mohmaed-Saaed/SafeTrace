using SafeTrace.Application.DTOs.Chat;
using SafeTrace.Application.DTOs.Message;
using SafeTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Mapping
{
    public class ChatMappingProfile : Profile
    {
        public ChatMappingProfile()
        {
            // Chat → ChatDetailsResponse

            CreateMap<Chat, ChatDetailsDto>()
            .ForMember(dest => dest.ChatId, opt => opt.MapFrom(src => src.Id));

            // Message → MessageResponse
            CreateMap<Message, MessageDto>();
        }
    }
     
}
