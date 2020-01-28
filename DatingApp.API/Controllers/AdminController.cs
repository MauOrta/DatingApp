using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IDatingRepository _datingRepository;
        private readonly IMapper _mapper;     
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;
        public AdminController(DataContext context, UserManager<User> userManager,
        IDatingRepository datingRepository, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _datingRepository = datingRepository;
            _userManager = userManager;
            _context = context;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("usersWithRoles")]
    public async Task<IActionResult> GetUserWithRoles()
    {
        var userList = await _context.Users
        .OrderBy(x => x.UserName)
        .Select(user => new
        {
            id = user.Id,
            UserName = user.UserName,
            Roles = (from userRole in user.userRole
                     join role in _context.Roles
                     on userRole.RoleId
                     equals role.Id
                     select role.Name).ToList()
        }).ToListAsync();

        return Ok(userList);
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("editRoles/{userName}")]
    public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
    {
        var user = await _userManager.FindByNameAsync(userName);

        var userRoles = await _userManager.GetRolesAsync(user);

        var selectedRoles = roleEditDto.RoleNames;

        // selectedRoles = selectedRoles != null ? selectedRoles : new string[] {};
        selectedRoles = selectedRoles ?? new string[] { };

        var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to add to roles");

        result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to remove the roles");

        return Ok(await _userManager.GetRolesAsync(user));
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpGet("photosForModeration")]
    public async Task<IActionResult> GetPhotosForModeration()
    {
        var photosWaitApp = await _datingRepository.GetPhotosForModeration();

        if (photosWaitApp == null)
            return Ok(photosWaitApp);

        var photoToReturnDto = _mapper.Map<IEnumerable<PhotosForDetailDto>>(photosWaitApp);

        return Ok(photoToReturnDto);
    }
    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpDelete("deletePhoto/{userId}/{id}")]
    public async Task<IActionResult> DeletePhoto(int userId, int id)
    {
        var user = await _datingRepository.GetUser(userId);

        if (!user.Photos.Any(p => p.Id == id))
            return Unauthorized();

        var photoFromRepo = await _datingRepository.GetPhoto(id);

        if (photoFromRepo.PublicID != null)
        {
            var deleteParams = new DeletionParams(photoFromRepo.PublicID);

            var result = _cloudinary.Destroy(deleteParams);

            if (result.Result == "ok")
            {
                _datingRepository.Delete(photoFromRepo);
            }
        }

        if (photoFromRepo.PublicID != null)
        {
            _datingRepository.Delete(photoFromRepo);
        }
        if (await _datingRepository.SaveAll())
            return Ok();

        return BadRequest("Failed to delete the photo");
    }

    [Authorize(Policy = "ModeratePhotoRole")]
    [HttpPost("authPhoto/{userId}/{id}")]
    public async Task<IActionResult> ApprovePhoto(int userId, int id)
    {
        var user = await _datingRepository.GetUser(userId);

        if (!user.Photos.Any(p => p.Id == id))
            return Unauthorized();

        var photoFromRepo = await _datingRepository.GetPhoto(id);

        photoFromRepo.IsApproved = true;

        if (await _datingRepository.SaveAll())
            return Ok();

        return BadRequest("Failed to Approve the photo");
    }
 }
}