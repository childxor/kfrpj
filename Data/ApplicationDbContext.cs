using kfrpj.Models;
using kfrpj.Models.rooms;
using kfrpj.Models.settings;
using kfrpj.Models.tenants;
using kfrpj.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using kfrpj.Models.finance;

namespace kfrpj.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<sys_users> sys_users { get; set; }

        public DbSet<sys_users_password> sys_users_password { get; set; }

        public DbSet<rooms_list> rooms_list { get; set; }

        public DbSet<tenants_list> tenants_list { get; set; }

        public DbSet<room_tenant_rel> room_tenant_rel { get; set; }

        public DbSet<TenantReportViewModel> tenant_report_view_models { get; set; }

        public DbSet<settings_list> settings_list { get; set; }

        public DbSet<water_meters_list> water_meters_list { get; set; }
        public DbSet<electric_meters_list> electric_meters_list { get; set; }
        public DbSet<room_charges_list> room_charges_list { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantReportViewModel>().HasNoKey();
        }
    }
}
