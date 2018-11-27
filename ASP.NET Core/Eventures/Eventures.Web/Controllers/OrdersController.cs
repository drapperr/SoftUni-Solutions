namespace Eventures.Web.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using Infrastructure;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Services.Contracts;
    using Services.Models;

    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrdersService ordersService;

        public OrdersController(IOrdersService ordersService)
        {
            this.ordersService = ordersService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(OrderCreateBindingModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.RedirectToAction("All", "Events");
            }

            var serviceModel = Mapper.Map<OrderServiceModel>(model);

            serviceModel.OrderedOn = DateTime.Now;

            await this.ordersService.Create(serviceModel, this.User.Identity.Name);

            return this.RedirectToAction("My", "Events");
        }

        [Authorize(Roles = GlobalConstants.AdminRoleName)]
        public async Task<IActionResult> All()
        {
            var orders = (await this.ordersService.GetAll())
                .Select(Mapper.Map<OrderListingViewModel>);

            return this.View(orders);
        }
    }
}