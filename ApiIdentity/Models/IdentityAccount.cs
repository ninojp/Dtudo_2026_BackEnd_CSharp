using Microsoft.AspNetCore.Identity;

namespace ApiIdentity.Models;

public sealed class IdentityAccount : IdentityUser
{
	public bool HasConfirmedAdultAge { get; set; }

	public DateTimeOffset? AdultAgeConfirmedAtUtc { get; set; }
}
