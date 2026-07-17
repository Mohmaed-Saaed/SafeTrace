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
            .ForMember(dest => dest.ChatId, opt => opt.MapFrom(src => src.Id))

            .ForMember(d => d.CaseTitle,
            opt => opt.MapFrom(s =>
                $"{s.Case.FName} {s.Case.SName} {s.Case.TName} {s.Case.LName}"))
            .ForMember(d => d.SenderName,
            opt => opt.MapFrom(s =>
                $"{s.Sender.FName} {s.Sender.LName}"))
            .ForMember(d => d.ReceiverName,
            opt => opt.MapFrom(s =>
                $"{s.Receiver.FName} {s.Receiver.LName}"));




            // Message → MessageResponse
            CreateMap<Message, MessageDto>()
            .ForMember(
            dest => dest.SendAt,
            opt => opt.MapFrom(src =>
            DateTime.SpecifyKind(src.SendAt, DateTimeKind.Utc))
            )
            .ForMember(
             dest => dest.ForEveryoneDeletedAt,
             opt => opt.MapFrom(src =>
             src.ForEveryoneDeletedAt.HasValue
                ? DateTime.SpecifyKind(src.ForEveryoneDeletedAt.Value, DateTimeKind.Utc)
                : (DateTime?)null))
             .ForMember(
              dest => dest.SenderDeletedAt,
               opt => opt.MapFrom(src =>
               src.SenderDeletedAt.HasValue
                ? DateTime.SpecifyKind(src.SenderDeletedAt.Value, DateTimeKind.Utc)
                : (DateTime?)null))
             .ForMember(
                 dest => dest.ReceiverDeletedAt,
                opt => opt.MapFrom(src =>
                src.ReceiverDeletedAt.HasValue
                ? DateTime.SpecifyKind(src.ReceiverDeletedAt.Value, DateTimeKind.Utc)
                : (DateTime?)null))
             .ForMember(
                dest => dest.Content,
                opt => opt.MapFrom(src =>
                src.IsDeletedForEveryone
                ? "تم حذف هذه الرسالة"
                : src.Content
                )
                );
        }
    }
     
}
