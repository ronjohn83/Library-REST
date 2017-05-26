using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorid}/books/")]
    public class BooksController : Controller
    {
        private ILibraryRepository _repo;
        private ILogger<BooksController> _logger;
        private IUrlHelper _urlHelper;

        public BooksController(ILibraryRepository repo,
            ILogger<BooksController> logger,
            IUrlHelper urlHelper)
        {
            _repo = repo;
            _logger = logger;
            _urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorid)
        {
            if (!_repo.AuthorExists(authorid))
            {
                return NotFound();
            }

            var booksForAuthorFromRepo = _repo.GetBooksForAuthor(authorid);

            var booksForAuthor = Mapper.Map<IEnumerable<BooksDto>>(booksForAuthorFromRepo);

            if (booksForAuthor == null)
                return NotFound();

            booksForAuthor = booksForAuthor.Select(book =>
            {
                book = CreateLinksForBooks(book);
                return book;
            });

            var wrapper = new LinksCollectionResourceWrapperDto<BooksDto>(booksForAuthor);

            return Ok(CreateLinksForBooks(wrapper));

        }

        [HttpGet("{id}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_repo.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _repo.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            var bookForAuthor = Mapper.Map<BooksDto>(bookForAuthorFromRepo);

            return Ok(CreateLinksForBooks(bookForAuthor));
        }

        [HttpPost]
        public IActionResult CreateBookForAuthor([FromBody] BookForCreationDto book, Guid authorId)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForCreationDto),
                    "The provided description should be different from title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            if (!_repo.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookEntity = Mapper.Map<Book>(book);

            _repo.AddBookForAuthor(authorId, bookEntity);

            if (!_repo.Save())
            {
                throw new Exception("Creating book for author failed on save");
            }

            var bookToReturn = Mapper.Map<BooksDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor",
                new
                {
                    authorId = bookToReturn.AuthorId,
                    id = bookToReturn.Id
                }, CreateLinksForBooks(bookToReturn));
        }

        [HttpPut("{id}", Name = "UpdateBookForAuthor")]
        public IActionResult UpdateBookForAuthor(
          Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "Books description should not be same as title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            if (!_repo.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _repo.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                var bookEntity = Mapper.Map<Book>(book);
                bookEntity.Id = id;

                _repo.AddBookForAuthor(authorId, bookEntity);

                if (!_repo.Save())
                {
                    throw new Exception($"Book {id} failed on save");
                }

                var bookToReturn = Mapper.Map<BooksDto>(bookEntity);

                return CreatedAtRoute("GetBookForAuthor", new {Id = bookToReturn.Id}, bookToReturn);
            }

            Mapper.Map(book, bookForAuthorFromRepo);

            _repo.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_repo.Save())
            {
                throw new Exception($"Book id {id} failed on save.");
            }

            return NoContent();
        }

        [HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            if (!_repo.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = _repo.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            var booktoPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            patchDoc.ApplyTo(booktoPatch, ModelState);

            if (booktoPatch.Description == booktoPatch.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            TryValidateModel(booktoPatch);

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            Mapper.Map(booktoPatch, bookForAuthorFromRepo);

            _repo.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_repo.Save())
            {
                throw new Exception("Error while patching.");
            }

            return NoContent();

        }

        [HttpDelete("{id}", Name = "DeleteBookForAuthor")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_repo.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthor = _repo.GetBookForAuthor(authorId, id);
            if (bookForAuthor == null)
            {
                return NotFound();
            }

            _repo.DeleteBook(bookForAuthor);

            if (!_repo.Save())
            {
                throw new Exception($"Book id {id} failed to delete on save");
            }

            _logger.LogInformation(100, $"Book {id} for author {authorId} was deleted.");

            return NoContent();
        }

        private BooksDto CreateLinksForBooks(BooksDto book)
        {
            book.Links.Add(new LinkDto(_urlHelper.Link("GetBookForAuthor",
                new { Id = book.Id }),
                "self",
                "GET"));

            book.Links.Add(new LinkDto(_urlHelper.Link("DeleteBookForAuthor",
                new {Id = book.Id}),
                "delete_book",
                "DELETE"));

            book.Links.Add(new LinkDto(_urlHelper.Link("UpdateBookForAuthor",
                new {Id = book.Id}),
                "update_book",
                "PUT"));

            book.Links.Add(new LinkDto(_urlHelper.Link("PartiallyUpdateBookForAuthor",
                new {Id = book.Id}),
                "partially_update_book",
                "PATCH"));

            return book;
        }

        private LinksCollectionResourceWrapperDto<BooksDto> CreateLinksForBooks(
            LinksCollectionResourceWrapperDto<BooksDto> booksWrapper)
        {
            // link to self
            booksWrapper.Links.Add(
                new LinkDto(_urlHelper.Link("GetBooksForAuthor",
                new { }),
                "self",
                "GET"));

            return booksWrapper;
        } 
    }


}
