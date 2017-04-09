using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.API.Services;
using Library.API.Models;
using Library.API.Helpers;
using AutoMapper;
using Library.API.Entities;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class Authors : Controller
    {
        private readonly ILibraryRepository _libraryRepo;

        public Authors(ILibraryRepository libraryRepo)
        {
            _libraryRepo = libraryRepo;
        }

        [HttpGet]
        public IActionResult GetAuthors()
        {
            var authorsFromRepo = _libraryRepo.GetAuthors();

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);
              
            return new JsonResult(authors);
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id)
        {
            var authorFromRepo = _libraryRepo.GetAuthor(id);
            if (authorFromRepo == null)
                return BadRequest();

            var authorFromDto = Mapper.Map<AuthorDto>(authorFromRepo);

            return Ok(authorFromDto);
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
                return BadRequest();

            var authorEntities = Mapper.Map<Author>(author);

            _libraryRepo.AddAuthor(authorEntities);

            if (!_libraryRepo.Save())
                throw new Exception("Error while saving.");

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntities);

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }
    }
}
