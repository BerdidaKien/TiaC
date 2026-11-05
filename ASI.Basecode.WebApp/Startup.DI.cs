using ASI.Basecode.Data;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Repositories;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.Services.Services;
using ASI.Basecode.WebApp.Authentication;
using ASI.Basecode.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ASI.Basecode.WebApp
{
    // Other services configuration
    internal partial class StartupConfigurer
    {
        /// <summary>
        /// Configures the other services.
        /// </summary>
        private void ConfigureOtherServices()
        {
            // Framework
            this._services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            this._services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            // Common
            this._services.AddScoped<TokenProvider>();
            this._services.TryAddSingleton<TokenProviderOptionsFactory>();
            this._services.TryAddSingleton<TokenValidationParametersFactory>();
            this._services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Services
            this._services.TryAddSingleton<TokenValidationParametersFactory>();
            this._services.AddScoped<IUserService, UserService>();


            // Repositories
            this._services.AddScoped<IUserRepository, UserRepository>();

            // Equipment Master Data Dependency Injection
            this._services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            this._services.AddScoped<IEquipmentService, EquipmentService>();
            this._services.AddScoped<IHistoryService, HistoryService>();
            this._services.AddScoped<IReportService, ReportService>();
            this._services.AddScoped<IReservationRepository, ReservationRepository>();
            this._services.AddScoped<IReservationService, ReservationService>();
            this._services.AddScoped<IUserAdminRepository, UserAdminRepository>();
            this._services.AddScoped<IAdminUserService, AdminUserService>();
            this._services.AddScoped<IAdminReservationService, AdminReservationService>();
            this._services.AddScoped<IItemReportService, ItemReportService>();
            // Manager Class
            this._services.AddScoped<SignInManager>();
            this._services.AddScoped<ISettingsRepository, SettingsRepository>();
            this._services.AddScoped<ISettingsService, SettingsService>();
            this._services.AddScoped<ICategoryRepository, CategoryRepository>();
            this._services.AddScoped<ICategoryService, CategoryService>();
            this._services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            this._services.AddScoped<IAccountAdminRepository, AccountAdminRepository>();
            this._services.AddScoped<IUserStatusService, UserStatusService>();
            this._services.AddScoped<IInfoRequestService, InfoRequestService>();
            this._services.AddScoped<ProofStorage>();
            this._services.AddHttpClient();
        }
    }
}
