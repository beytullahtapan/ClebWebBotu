namespace ClebwebBot.Models
{
    public class Messages
    {
        public int MessageID { get; set; }
        public string FullName { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public bool Status { get; set; }
        public DateTime DateTime { get; set; }
        public string Phone { get; set; }
    }
}
