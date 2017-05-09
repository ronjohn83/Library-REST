using System;
using Library.API.Services;

namespace Library.API.Models
{
    public class BooksDto : LinkedResourceBaseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid AuthorId { get; set; }

    }
}