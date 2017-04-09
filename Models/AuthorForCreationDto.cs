using Library.API.Entities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Library.API.Models
{
    public class AuthorForCreationDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTimeOffset DateOfBirth { get; set; }

        public string Genre { get; set; }

        public ICollection<Book> MyProperty { get; set; }

    }
}