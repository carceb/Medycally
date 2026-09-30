using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Medycally.Core.Hubs;

public interface IAppointmentClient
{
    Task AppointmentChanged(object payload);
}

[Authorize]
public class AppointmentHub : Hub<IAppointmentClient>
{
    private readonly IClinic _clinic;

    public AppointmentHub(IClinic clinic)
    {
        _clinic = clinic;
    }

    public static string GroupName(int clinicId) => $"clinic-{clinicId}";

    public async Task JoinClinic(int clinicId)
    {
        if (clinicId <= 0) return;
        if (!UserHasAccessToClinic(clinicId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(clinicId));
    }

    public async Task LeaveClinic(int clinicId)
    {
        if (clinicId <= 0) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(clinicId));
    }

    private bool UserHasAccessToClinic(int clinicId)
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated != true) return false;

        bool isSuperAdmin = string.Equals(
            user.FindFirst("IsSuperAdmin")?.Value, "true",
            StringComparison.OrdinalIgnoreCase);
        if (isSuperAdmin) return true;

        if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int securityUserId))
            return false;

        int? doctorId = int.TryParse(user.FindFirst("DoctorId")?.Value, out int did) && did > 0
            ? did : null;
        bool hasGlobalScope = string.Equals(
            user.FindFirst("HasGlobalScope")?.Value, "true",
            StringComparison.OrdinalIgnoreCase);

        var clinics = _clinic.GetByUser(securityUserId, isSuperAdmin: false, doctorId, hasGlobalScope);
        return clinics.Any(c => c.ClinicId == clinicId);
    }
}
