using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosquare.Labs.EmbedIO.IoT
{
    class UsersTest
    {
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string LastName { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string Country { get; set; }


            public User GetUser()
            {               
                var user = new User()
                {
                    Id = 0,
                    Name = "My Name",
                    LastName = "My Last Name",
                    Address = "My Address",
                    City = "My City",
                    Country = "My country"
                };
                return user;
            }
            public User[] GetUsers()
            {
                var userList = new List<User>();
                for (int i = 1; i < 10; i++)
                {
                    userList.Add(new User()
                    {
                        Id = i,
                        Name = "My Name",
                        LastName = "My Last Name",
                        Address = "My Address",
                        City = "My City",
                        Country = "My country"
                    });
                }
                return userList.ToArray();
            }
        }    
    }
}
