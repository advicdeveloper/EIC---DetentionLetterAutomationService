using System;

namespace CONTECH.Service.BusinessEntities
{
    [Serializable]
    public class Users
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }

    }
}
