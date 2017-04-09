using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authorcollections")]
    public class AuthorsCollection : Controller
    {
        private readonly ILibraryRepository _libraryRepo;

        public AuthorsCollection(ILibraryRepository libriaryRepo)
        {
            _libraryRepo = libriaryRepo;
        }

        [HttpPost]
        public IActionResult CreateAuthorCollection(
            [FromBody] IEnumerable<AuthorForCreationDto> authors)
        {
            if (authors == null)
                return BadRequest();

            var authorEntities = Mapper.Map<IEnumerable<Author>>(authors);

            foreach (var author in authorEntities)
            {
                _libraryRepo.AddAuthor(author);
            }

            if (!_libraryRepo.Save())
                throw new Exception("Error while saving");

            return Ok();
        }
    }
}
