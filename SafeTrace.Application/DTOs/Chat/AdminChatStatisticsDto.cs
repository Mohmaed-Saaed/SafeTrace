namespace SafeTrace.Application.DTOs.Chat
{
    public class AdminChatStatisticsDto
    {
        public int TotalChats { get; set; }
        public int ActiveChats { get; set; }
        public int DeletedBySenderOnly { get; set; }
        public int DeletedByReceiverOnly { get; set; }
        public int DeletedByBoth { get; set; }
    }
}
