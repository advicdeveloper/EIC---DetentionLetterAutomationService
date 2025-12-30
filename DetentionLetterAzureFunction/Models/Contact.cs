using System;

namespace DetentionLetterAzureFunction.Models
{
    public class Contact
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
