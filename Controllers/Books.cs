using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.API.Services;
using AutoMapper;
using Library.API.Models;
using Library.API.Entities;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class Books : Controller
    {
        private readonly ILibraryRepository _libraryRepo;

        public Books(ILibraryRepository libraryRepo)
        {
            _libraryRepo = libraryRepo;
        }

        [HttpGet]
        public IActionResult GetBooksforAuthor(Guid authorId)
        {
            if (!_libraryRepo.AuthorExists(authorId))
                return NotFound();

            var booksfromAuthorRepo = _libraryRepo.GetBooksForAuthor(authorId);

            var booksFromAuthor = Mapper.Map<IEnumerable<BookDto>>(booksfromAuthorRepo);

            return Ok(booksFromAuthor);
        }

        [HttpGet("{id}", Name = "GetBook")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepo.AuthorExists(authorId))
                return NotFound();

            var bookFromAuthorRepo = _libraryRepo.GetBookForAuthor(authorId, id);
            if (bookFromAuthorRepo == null)
                return NotFound();

            var bookFromAuthor = Mapper.Map<BookDto>(bookFromAuthorRepo);

            return Ok(bookFromAuthor);
        }

        [HttpPost]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
                return BadRequest();

            if (_libraryRepo.AuthorExists(authorId))
                return NotFound();

            var bookEntity = Mapper.Map<Book>(book);

            _libraryRepo.AddBookForAuthor(authorId, bookEntity);

            if (!_libraryRepo.Save())
                throw new Exception("Error while saving");

            var booktoReturn = Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBook", new
            {
                authorId = booktoReturn.AuthorId,
                bookId = booktoReturn.Id,
                booktoReturn
            });
        }

        [HttpPut("{id}")]
        public IActionResult UpdateBookForAuthor([FromBody] BookForUpdateDto book,
            Guid authorId, Guid id)
        {
            if (book == null)
                return BadRequest();

            if (!_libraryRepo.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepo.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = id;

                _libraryRepo.AddBookForAuthor(authorId, bookToAdd);

                if (!_libraryRepo.Save())
                {
                    throw new Exception("Upserting book failed.");
                }

                var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBook", new
                {
                    authorId = authorId,
                    id = bookToReturn.Id
                },
                bookToReturn);
            }

            Mapper.Map(book, bookForAuthorFromRepo);

            _libraryRepo.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_libraryRepo.Save())
                throw new Exception("Error while saving.");

            return NoContent();
        }
    }
}
