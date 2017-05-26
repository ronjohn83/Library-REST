using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authorcollections")]
    public class AuthorCollectionController : Controller
    {
        private readonly ILibraryRepository _repo;

        public AuthorCollectionController(ILibraryRepository repo)
        {
            _repo = repo;
        }

        [HttpPost]
        public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDto> authors)
        {
            if (authors == null)
            {
                return BadRequest();
            }

            var authorEntity = Mapper.Map<IEnumerable<Author>>(authors);

            foreach (var author in authorEntity)
            {
                _repo.AddAuthor(author);
            }

            if (!_repo.Save())
            {
                throw new Exception("Creating an author failed on save.");
            }

            return Ok();
        }
    }
}
