namespace ApiIdentity.Authorization;

public static class AuthorizationCatalog
{
    public const string PermissionClaimType = "permission";

    public static class Roles
    {
        public const string SuperAdministrator = "Superadministrador";
        public const string SiteUser = "Usuario do Site";
    }

    public static class Permissions
    {
        public const string CatalogRead = "catalog.read";
        public const string CatalogWrite = "catalog.write";
        public const string CatalogDelete = "catalog.delete";
        public const string IdentityProvision = "identity.provision";
        public const string IdentityLogin = "identity.login";
        public const string IdentitySelfRead = "identity.self.read";
        public const string HealthRead = "health.read";
        public const string ServiceMalRead = "service.mal.read";
        public const string FilesystemCommand = "filesystem.command";
    }

    public static IReadOnlyList<PermissionCatalogEntry> AllPermissions { get; } =
    [
        new(Permissions.CatalogRead, "Leitura do catalogo publico."),
        new(Permissions.CatalogWrite, "Criacao e alteracao do catalogo."),
        new(Permissions.CatalogDelete, "Exclusao do catalogo com step-up quando aplicavel."),
        new(Permissions.IdentityProvision, "Bootstrap e provisionamento administrativo de contas."),
        new(Permissions.IdentityLogin, "Autenticacao no servico de identidade."),
        new(Permissions.IdentitySelfRead, "Leitura do proprio perfil de identidade."),
        new(Permissions.HealthRead, "Leitura do health minimo restrito."),
        new(Permissions.ServiceMalRead, "Chamada interna autorizada a dados MyAnimeList."),
        new(Permissions.FilesystemCommand, "Operacao de arquivos por ID e comando autorizado.")
    ];

    public static IReadOnlyList<RoleCatalogEntry> AllRoles { get; } =
    [
        new(
            "bb9c24e5-6b8a-4464-a420-11db01021681",
            Roles.SuperAdministrator,
            [
                Permissions.CatalogRead,
                Permissions.CatalogWrite,
                Permissions.CatalogDelete,
                Permissions.IdentityProvision,
                Permissions.IdentitySelfRead,
                Permissions.HealthRead,
                Permissions.FilesystemCommand
            ]),
        new(
            "206268dd-529c-49f9-973f-030ddcbba450",
            Roles.SiteUser,
            [
                Permissions.CatalogRead,
                Permissions.IdentitySelfRead
            ])
    ];

    public static string PolicyName(string permission) => $"permission:{permission}";
}

public sealed record PermissionCatalogEntry(string Key, string Description);

public sealed record RoleCatalogEntry(string Id, string Name, IReadOnlyList<string> PermissionKeys);
