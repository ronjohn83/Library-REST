using AutoMapper;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Entities;
using Library.API.Helpers;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;
        private ILogger<BooksController> _logger;
        private IUrlHelper _urlHelper;

        public BooksController(ILibraryRepository libraryRepository,
            ILogger<BooksController> logger, IUrlHelper urlHelper )
        {
            _logger = logger;
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);

            var booksForAuthor = Mapper.Map<IEnumerable<BooksDto>>(booksForAuthorFromRepo);

            booksForAuthor = booksForAuthor.Select(book =>
            {
                book = CreateLinksForBook(book);
                return book;
            });

            var wrapper = new LinkedCollectionResourceWrapperDto<BooksDto>(booksForAuthor);

            return Ok(CreateLinksForBooks(wrapper));
        }

        [HttpGet("{id}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
                return NotFound();

            var bookForAuthor = Mapper.Map<BooksDto>(bookForAuthorFromRepo);
            return Ok(CreateLinksForBook(bookForAuthor));
        }

        [HttpPost]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book )
        {
            if (book == null)
                return BadRequest();

            if (book.Title == book.Description)
            {
                ModelState.AddModelError(nameof(BookForCreationDto),
                    "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
                return new UnproccessableEntityObjectResult(ModelState);

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookEntity = Mapper.Map<Book>(book);

            _libraryRepository.AddBookForAuthor(authorId, bookEntity);
            if (!_libraryRepository.Save())
                throw new Exception($"Creating a book for author {authorId} failed on save.");

            var bookToReturn = Mapper.Map<BooksDto>(bookEntity);
            return CreatedAtRoute("GetBookForAuthor", new
            {
                authorId = authorId,
                id = bookToReturn.Id
            },
            CreateLinksForBook(bookToReturn));
        }

        [HttpDelete("{id}", Name = "DeleteBookForAuthor")]
        public IActionResult DeleteBookForAuthor(Guid id, Guid authorid)
        {
            if (!_libraryRepository.AuthorExists(authorid))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorid, id);
            if (bookForAuthorFromRepo == null)
                return NotFound();

            _libraryRepository.DeleteBook(bookForAuthorFromRepo);

            if(!_libraryRepository.Save())
                throw new Exception($"Book {id} for author {authorid} failed to save");

            _logger.LogInformation(100, $"Book {id} for author {authorid} was deleted.");

            return NoContent();
        }

        [HttpPut("{id}", Name = "UpdateBookForAuthor")]
        public IActionResult UpdateBookBookForAuthor(Guid id, Guid authorId, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
                return BadRequest();

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should");
            }

            if (!ModelState.IsValid)
            {
                return new UnproccessableEntityObjectResult(ModelState);
            }

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                var bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if(!_libraryRepository.Save())
                    throw new Exception($"Upserting book {id} for author {authorId} failed to save");

                var bookToReturn = Mapper.Map<BooksDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor", new
                {
                    authorId = authorId,
                    id = bookToReturn.Id
                },
                bookToReturn);
            }

            Mapper.Map(book, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if(!_libraryRepository.Save())
                throw new Exception($"Updating book {id} failed on save.");

            return NoContent();
        }

        [HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            if (!_libraryRepository.AuthorExists(authorId))
                return NotFound();

            var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto);

                var bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = id;

                _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

                if (!_libraryRepository.Save())
                    throw new Exception($"Book failed on save");

                var bookToReturn = Mapper.Map<BooksDto>(bookToAdd);
                return CreatedAtRoute("GetBookForAuthor", 
                    new { authorId = authorId, id = bookToReturn.Id },
                    bookToReturn);
            }

            var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            patchDoc.ApplyTo(bookToPatch);

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if(!_libraryRepository.Save())
                throw new Exception($"Patching book {id} for author {authorId} failed on savings");

            return NoContent();
        }

        private BooksDto CreateLinksForBook(BooksDto book)
        {

            book.Links.Add(new LinkDto(_urlHelper.Link("GetBookForAuthor",
                new {id = book.Id}),
                "self",
                "GET"));

            book.Links.Add(new LinkDto(_urlHelper.Link("DeleteBookForAuthor",
                new {id = book.Id}),
                "delete_book",
                "DELELTE"));

            book.Links.Add(new LinkDto(_urlHelper.Link("UpdateBookForAuthor",
                new {id = book.Id}),
                "update_book",
                "PUT"));

            book.Links.Add(new LinkDto(_urlHelper.Link("PartiallyUpdateBookForAuthor",
                new {id = book.Id}),
                "partially_update_book",
                "PATCH"));

            return book;
        }

        private LinkedCollectionResourceWrapperDto<BooksDto> CreateLinksForBooks(
            LinkedCollectionResourceWrapperDto<BooksDto> booksWrapper)
        {
            booksWrapper.Links.Add(
                new LinkDto(_urlHelper.Link("GetbooksForAuthor", new {}),
                "self",
                "GET"));

            return booksWrapper;
        } 

    }
}
