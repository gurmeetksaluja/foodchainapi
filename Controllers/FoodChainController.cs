using FoodChainAPI.Core.Entities;
using FoodChainAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FoodChainBrandsAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FoodChainController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public FoodChainController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [Route("FoodChains")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var foodChains = await _unitOfWork.FoodChain.GetMultipleDataByConditionAsync(new { @IsActive = true, @IsDeleted = false });
            return Ok(foodChains);
        }

        [Route("FoodChainDetail")]
        [HttpGet]
        public async Task<IActionResult> GetDetailById(int id)
        {
            var foodChainDetail = await _unitOfWork.FoodChain.GetDataByIdAsync(new { @Id = id });
            return Ok(foodChainDetail);
        }

        [Route("AddFoodChain")]
        [HttpPost]
        public async Task<IActionResult> AddFoodChain([FromForm] FoodChainModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            return StatusCode(201, await _unitOfWork.FoodChain.AddAsync(new FoodChainBrands { FoodChainName = model.FoodChainName, FoodChainLogoURL = model.FoodChainLogoURL }));
            // return Ok(foodChainId);
        }

        [Route("UpdateFoodChain")]
        [HttpPut]
        public async Task<IActionResult> UpdateFoodChain([FromForm] FoodChainModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var foodChainEntity = await _unitOfWork.FoodChain.GetDataByIdAsync(new { @Id = model.Id });
            if (foodChainEntity != null)
            {
                foodChainEntity.FoodChainName = model.FoodChainName;
                foodChainEntity.FoodChainLogoURL = model.FoodChainLogoURL;

                return Ok(await _unitOfWork.FoodChain.UpdateAsync(foodChainEntity, new { @Id = model.Id }));
            }

            return BadRequest();
        }

        [Route("RemoveFoodChain")]
        [HttpDelete]
        public async Task<IActionResult> DeleteFoodChain(int id)
        {
            return Ok(await _unitOfWork.FoodChain.RemoveAsync(new { @Id = id }));
        }



        [Route("Image")]
        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var fileName = "";
            if (file != null)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                fileName = Path.GetFileNameWithoutExtension(file.FileName);
                string extension = Path.GetExtension(file.FileName);
                fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;

                var filePath = Path.Combine(_webHostEnvironment.WebRootPath + "/Images/", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }
            }
            return Ok(fileName);
        }
    }
}
