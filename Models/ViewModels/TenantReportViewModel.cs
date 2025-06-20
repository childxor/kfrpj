using kfrpj.Models.tenants;

namespace kfrpj.Models.ViewModels;

public class TenantReportViewModel
{
    public List<tenants_list> Tenants { get; set; }
    public int TotalCount { get; set; }
    public DateTime GeneratedDate { get; set; }
    public string GeneratedBy { get; set; }
}
