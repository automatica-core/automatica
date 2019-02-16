using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.EF.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AreaTypes",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaTypes", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "BoardTypes",
                columns: table => new
                {
                    Type = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardTypes", x => x.Type);
                });

            migrationBuilder.CreateTable(
                name: "CategoryGroups",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryGroups", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "InterfaceTypes",
                columns: table => new
                {
                    Type = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    MaxChilds = table.Column<int>(type: "int(11)", nullable: false),
                    MaxInstances = table.Column<int>(type: "int(11)", nullable: false),
                    IsDriverInterface = table.Column<bool>(nullable: false, defaultValue: false),
                    CanProvideBoardType = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterfaceTypes", x => x.Type);
                });

            migrationBuilder.CreateTable(
                name: "NodeDataTypes",
                columns: table => new
                {
                    Type = table.Column<long>(type: "bigint(20)", nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeDataTypes", x => x.Type);
                });

            migrationBuilder.CreateTable(
                name: "Priviledges",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Priviledges", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTypes",
                columns: table => new
                {
                    Type = table.Column<long>(type: "bigint(20)", nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Meta = table.Column<string>(maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTypes", x => x.Type);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 128, nullable: false),
                    IsDeleteable = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "RuleInterfaceDirections",
                columns: table => new
                {
                    ObjId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleInterfaceDirections", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "RulePageTypes",
                columns: table => new
                {
                    ObjId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulePageTypes", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    ObjId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ValueKey = table.Column<string>(type: "varchar(254)", nullable: false),
                    ValueText = table.Column<string>(type: "text", nullable: true),
                    ValueInt = table.Column<int>(type: "int", nullable: true),
                    ValueDouble = table.Column<double>(type: "double", nullable: true),
                    Group = table.Column<string>(type: "varchar(1024)", nullable: true, defaultValue: "System"),
                    Type = table.Column<long>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    UserName = table.Column<string>(maxLength: 128, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    FirstName = table.Column<string>(maxLength: 128, nullable: false),
                    LastName = table.Column<string>(maxLength: 128, nullable: false),
                    Password = table.Column<string>(maxLength: 1024, nullable: false),
                    Salt = table.Column<string>(maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "VersionInformations",
                columns: table => new
                {
                    ObjId = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DriverGuid = table.Column<Guid>(maxLength: 36, nullable: true),
                    RuleGuid = table.Column<Guid>(maxLength: 36, nullable: false),
                    Version = table.Column<string>(maxLength: 1024, nullable: false),
                    Name = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionInformations", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "VisuPageTypes",
                columns: table => new
                {
                    ObjId = table.Column<long>(type: "bigint(20)", nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisuPageTypes", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "AreaTemplates",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    This2AreaType = table.Column<Guid>(nullable: false),
                    ProvidesThis2AreayType = table.Column<Guid>(nullable: false),
                    NeedsThis2AreaType = table.Column<Guid>(nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    IsDeleteable = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaTemplates", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_AreaTemplate_NeedsThis2AreaTypeNavigation",
                        column: x => x.NeedsThis2AreaType,
                        principalTable: "AreaTypes",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AreaTemplate_ProvidesThis2AreayTypeNavigation",
                        column: x => x.ProvidesThis2AreayType,
                        principalTable: "AreaTypes",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AreaTemplate_AreaType",
                        column: x => x.This2AreaType,
                        principalTable: "AreaTypes",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoardInterfaces",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2BoardType = table.Column<Guid>(nullable: false),
                    This2InterfaceType = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Meta = table.Column<string>(maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardInterfaces", x => x.ObjId);
                    table.ForeignKey(
                        name: "BoardInterface_ibfk_1",
                        column: x => x.This2BoardType,
                        principalTable: "BoardTypes",
                        principalColumn: "Type",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "BoardInterface_ibfk_2",
                        column: x => x.This2InterfaceType,
                        principalTable: "InterfaceTypes",
                        principalColumn: "Type",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Priviledge2Roles",
                columns: table => new
                {
                    This2Priviledge = table.Column<Guid>(nullable: false),
                    This2Role = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Priviledge2Roles", x => x.This2Role);
                    table.UniqueConstraint("AK_Priviledge2Roles_This2Priviledge", x => x.This2Priviledge);
                    table.ForeignKey(
                        name: "FK_Priviledge2Roles_Priviledges_This2Priviledge",
                        column: x => x.This2Priviledge,
                        principalTable: "Priviledges",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Priviledge2Roles_Roles_This2Role",
                        column: x => x.This2Role,
                        principalTable: "Roles",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RulePages",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    This2RulePageType = table.Column<long>(type: "bigint(20)", nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulePages", x => x.ObjId);
                    table.ForeignKey(
                        name: "RulePage_ibfk_1",
                        column: x => x.This2RulePageType,
                        principalTable: "RulePageTypes",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoryInstances",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    This2CategoryGroup = table.Column<Guid>(nullable: false),
                    Rating = table.Column<int>(nullable: false),
                    IsFavorite = table.Column<bool>(nullable: false, defaultValue: false),
                    Icon = table.Column<string>(maxLength: 128, nullable: false),
                    Color = table.Column<string>(maxLength: 128, nullable: false, defaultValue: "rgba(255, 255, 255, 1)"),
                    IsDeleteable = table.Column<bool>(nullable: false, defaultValue: true),
                    This2UserGroup = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryInstances", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_CategoryInstance_This2CategoryGroupNavigation",
                        column: x => x.This2CategoryGroup,
                        principalTable: "CategoryGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeInstance_This2UserGroup",
                        column: x => x.This2UserGroup,
                        principalTable: "UserGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroup2Roles",
                columns: table => new
                {
                    This2UserGroup = table.Column<Guid>(nullable: false),
                    This2Role = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroup2Roles", x => new { x.This2UserGroup, x.This2Role });
                    table.ForeignKey(
                        name: "FK_UserGroup2Roles_Roles_This2Role",
                        column: x => x.This2Role,
                        principalTable: "Roles",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroup2Roles_UserGroups_This2UserGroup",
                        column: x => x.This2UserGroup,
                        principalTable: "UserGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User2Groups",
                columns: table => new
                {
                    This2User = table.Column<Guid>(nullable: false),
                    This2UserGroup = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User2Groups", x => new { x.This2User, x.This2UserGroup });
                    table.ForeignKey(
                        name: "FK_User2Groups_Users_This2User",
                        column: x => x.This2User,
                        principalTable: "Users",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_User2Groups_UserGroups_This2UserGroup",
                        column: x => x.This2UserGroup,
                        principalTable: "UserGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User2Roles",
                columns: table => new
                {
                    This2User = table.Column<Guid>(nullable: false),
                    This2Role = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User2Roles", x => new { x.This2User, x.This2Role });
                    table.ForeignKey(
                        name: "FK_User2Roles_Roles_This2Role",
                        column: x => x.This2Role,
                        principalTable: "Roles",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_User2Roles_Users_This2User",
                        column: x => x.This2User,
                        principalTable: "Users",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisuObjectTemplates",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 1024, nullable: false),
                    Group = table.Column<string>(maxLength: 1024, nullable: false),
                    Width = table.Column<float>(nullable: false),
                    Height = table.Column<float>(nullable: false),
                    This2VisuPageType = table.Column<long>(type: "bigint(20)", nullable: false),
                    IsVisibleForUser = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisuObjectTemplates", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_VisuObjectTemplates_VisuPageTypes_VisuPageTypeObjId",
                        column: x => x.This2VisuPageType,
                        principalTable: "VisuPageTypes",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VisuPages",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    This2VisuPageType = table.Column<long>(type: "bigint(20)", nullable: false),
                    DefaultPage = table.Column<bool>(type: "bit(1)", nullable: false),
                    Height = table.Column<double>(nullable: false, defaultValue: 4.0),
                    Width = table.Column<double>(nullable: false, defaultValue: 6.0),
                    This2UserGroup = table.Column<Guid>(nullable: true),
                    IsFavorite = table.Column<bool>(nullable: false),
                    Rating = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisuPages", x => x.ObjId);
                    table.ForeignKey(
                        name: "VisuPage_This2UserGroup",
                        column: x => x.This2UserGroup,
                        principalTable: "UserGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "VisuPage_ibfk_1",
                        column: x => x.This2VisuPageType,
                        principalTable: "VisuPageTypes",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AreaInstances",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2AreaTemplate = table.Column<Guid>(nullable: false),
                    This2Parent = table.Column<Guid>(nullable: true),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Icon = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    This2UserGroup = table.Column<Guid>(nullable: true),
                    IsFavorite = table.Column<bool>(nullable: false),
                    Rating = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaInstances", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_AreaInstance_This2AreaTemplateNavigation",
                        column: x => x.This2AreaTemplate,
                        principalTable: "AreaTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AreaInstance_This2ParentNavigation",
                        column: x => x.This2Parent,
                        principalTable: "AreaInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeInstance_This2UserGroup",
                        column: x => x.This2UserGroup,
                        principalTable: "UserGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeTemplates",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 1024, nullable: false),
                    NeedsInterface2InterfacesType = table.Column<Guid>(nullable: false),
                    ProvidesInterface2InterfaceType = table.Column<Guid>(nullable: false),
                    IsDeleteable = table.Column<bool>(nullable: false, defaultValue: false),
                    DefaultCreated = table.Column<bool>(nullable: false, defaultValue: false),
                    IsReadable = table.Column<bool>(nullable: false, defaultValue: false),
                    IsReadableFixed = table.Column<bool>(nullable: false, defaultValue: false),
                    IsWriteable = table.Column<bool>(nullable: false, defaultValue: false),
                    IsWriteableFixed = table.Column<bool>(nullable: false, defaultValue: false),
                    This2NodeDataType = table.Column<long>(type: "bigint(20)", nullable: false),
                    MaxInstances = table.Column<int>(type: "int(11)", nullable: false),
                    IsAdapterInterface = table.Column<bool>(nullable: true, defaultValue: false),
                    NameMeta = table.Column<string>(type: "varchar(1024)", nullable: true),
                    This2DefaultMobileVisuTemplate = table.Column<Guid>(nullable: false, defaultValue: new Guid("16780dfd-887a-4a0a-9b2a-4d62ccc32c93"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeTemplates", x => x.ObjId);
                    table.ForeignKey(
                        name: "NodeTemplate_ibfk_1",
                        column: x => x.NeedsInterface2InterfacesType,
                        principalTable: "InterfaceTypes",
                        principalColumn: "Type",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeTemplate_ibfk_2",
                        column: x => x.ProvidesInterface2InterfaceType,
                        principalTable: "InterfaceTypes",
                        principalColumn: "Type",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeTemplate_DefaultVisuMobileTemplate",
                        column: x => x.This2DefaultMobileVisuTemplate,
                        principalTable: "VisuObjectTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeTemplate_ibfk_3",
                        column: x => x.This2NodeDataType,
                        principalTable: "NodeDataTypes",
                        principalColumn: "Type",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleTemplates",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 1024, nullable: false),
                    Group = table.Column<string>(maxLength: 1024, nullable: false),
                    Width = table.Column<float>(nullable: false),
                    Height = table.Column<float>(nullable: false),
                    This2DefaultMobileVisuTemplate = table.Column<Guid>(nullable: false, defaultValue: new Guid("16780dfd-887a-4a0a-9b2a-4d62ccc32c93"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleTemplates", x => x.ObjId);
                    table.ForeignKey(
                        name: "RuleTemplate_DefaultVisuMobileTemplate",
                        column: x => x.This2DefaultMobileVisuTemplate,
                        principalTable: "VisuObjectTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VisuObjectInstances",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2VisuObjectTemplate = table.Column<Guid>(nullable: false),
                    This2VisuPage = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: true, defaultValue: ""),
                    X = table.Column<float>(nullable: false),
                    Y = table.Column<float>(nullable: false),
                    Height = table.Column<float>(nullable: false),
                    Width = table.Column<float>(nullable: false),
                    This2UserGroup = table.Column<Guid>(nullable: true),
                    IsFavorite = table.Column<bool>(nullable: false),
                    Rating = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisuObjectInstances", x => x.ObjId);
                    table.ForeignKey(
                        name: "VisuObjectInstance_This2UserGroup",
                        column: x => x.This2UserGroup,
                        principalTable: "UserGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "VisuObjectInstance_ibfk_1",
                        column: x => x.This2VisuObjectTemplate,
                        principalTable: "VisuObjectTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "VisuObjectInstance_ibfk_2",
                        column: x => x.This2VisuPage,
                        principalTable: "VisuPages",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeInstances",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2NodeTemplate = table.Column<Guid>(nullable: true),
                    This2ParentNodeInstance = table.Column<Guid>(nullable: true),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    VisuName = table.Column<string>(nullable: true),
                    IsReadable = table.Column<bool>(nullable: false, defaultValue: false),
                    IsWriteable = table.Column<bool>(nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                    UseInVisu = table.Column<bool>(nullable: false, defaultValue: false),
                    This2UserGroup = table.Column<Guid>(nullable: true),
                    IsFavorite = table.Column<bool>(nullable: false),
                    Rating = table.Column<int>(nullable: false),
                    This2AreaInstance = table.Column<Guid>(nullable: true),
                    This2CategoryInstance = table.Column<Guid>(nullable: true),
                    StateTextValueTrue = table.Column<string>(nullable: true, defaultValue: "1"),
                    StateTextValueFalse = table.Column<string>(nullable: true, defaultValue: "0"),
                    StateColorValueTrue = table.Column<string>(nullable: true, defaultValue: "rgba(0, 0, 0, 255)"),
                    StateColorValueFalse = table.Column<string>(nullable: true, defaultValue: "rgba(0, 0, 0, 255)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeInstances", x => x.ObjId);
                    table.ForeignKey(
                        name: "NodeInstance_This2AreaInstance",
                        column: x => x.This2AreaInstance,
                        principalTable: "AreaInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeInstance_This2CategoryInstance",
                        column: x => x.This2CategoryInstance,
                        principalTable: "CategoryInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeInstance_ibfk_2",
                        column: x => x.This2NodeTemplate,
                        principalTable: "NodeTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeInstance_ibfk_3",
                        column: x => x.This2ParentNodeInstance,
                        principalTable: "NodeInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "NodeInstance_This2UserGroup",
                        column: x => x.This2UserGroup,
                        principalTable: "UserGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTemplates",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    Key = table.Column<string>(maxLength: 1024, nullable: false),
                    This2PropertyType = table.Column<long>(type: "bigint(20)", nullable: false),
                    This2NodeTemplate = table.Column<Guid>(nullable: true),
                    This2VisuObjectTemplate = table.Column<Guid>(nullable: true),
                    Group = table.Column<string>(maxLength: 1024, nullable: false),
                    IsVisible = table.Column<bool>(nullable: false, defaultValue: false),
                    IsReadonly = table.Column<bool>(nullable: false, defaultValue: false),
                    Meta = table.Column<string>(nullable: false),
                    DefaultValue = table.Column<string>(maxLength: 1024, nullable: true),
                    GroupOrder = table.Column<int>(type: "int(8)", nullable: false, defaultValueSql: "1")
                        .Annotation("Sqlite:Autoincrement", true),
                    Order = table.Column<int>(type: "int(8)", nullable: false, defaultValueSql: "1")
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTemplates", x => x.ObjId);
                    table.ForeignKey(
                        name: "PropertyTemplate_ibfk_2",
                        column: x => x.This2NodeTemplate,
                        principalTable: "NodeTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "PropertyTemplate_ibfk_1",
                        column: x => x.This2PropertyType,
                        principalTable: "PropertyTypes",
                        principalColumn: "Type",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "PropertyTemplate_ibfk_3",
                        column: x => x.This2VisuObjectTemplate,
                        principalTable: "VisuObjectTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleInstances",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: true, defaultValue: ""),
                    This2RuleTemplate = table.Column<Guid>(nullable: false),
                    This2RulePage = table.Column<Guid>(nullable: false),
                    X = table.Column<float>(nullable: false),
                    Y = table.Column<float>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                    UseInVisu = table.Column<bool>(nullable: false, defaultValue: false),
                    This2UserGroup = table.Column<Guid>(nullable: true),
                    This2AreaInstance = table.Column<Guid>(nullable: true),
                    This2CategoryInstance = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleInstances", x => x.ObjId);
                    table.ForeignKey(
                        name: "RuleInstance_This2AreaInstance",
                        column: x => x.This2AreaInstance,
                        principalTable: "AreaInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "RuleInstance_This2CategoryInstance",
                        column: x => x.This2CategoryInstance,
                        principalTable: "CategoryInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "RuleInstance_ibfk_2",
                        column: x => x.This2RulePage,
                        principalTable: "RulePages",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "RuleInstance_ibfk_1",
                        column: x => x.This2RuleTemplate,
                        principalTable: "RuleTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "RuleInstance_This2UserGroup",
                        column: x => x.This2UserGroup,
                        principalTable: "UserGroups",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleInterfaceTemplates",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    This2RuleTemplate = table.Column<Guid>(nullable: false),
                    This2RuleInterfaceDirection = table.Column<long>(type: "bigint(20)", nullable: false),
                    MaxLinks = table.Column<int>(type: "int(11)", nullable: false),
                    ParameterDataType = table.Column<int>(type: "int(11)", nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    DefaultValue = table.Column<string>(type: "varchar(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleInterfaceTemplates", x => x.ObjId);
                    table.ForeignKey(
                        name: "RuleInterfaceTemplate_ibfk_2",
                        column: x => x.This2RuleInterfaceDirection,
                        principalTable: "RuleInterfaceDirections",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "RuleInterfaceTemplate_ibfk_1",
                        column: x => x.This2RuleTemplate,
                        principalTable: "RuleTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NodeInstance2RulePages",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2RulePage = table.Column<Guid>(nullable: false),
                    This2NodeInstance = table.Column<Guid>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                    X = table.Column<float>(nullable: false),
                    Y = table.Column<float>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeInstance2RulePages", x => x.ObjId);
                    table.ForeignKey(
                        name: "NodeInstance2RulePage_ibfk_2",
                        column: x => x.This2NodeInstance,
                        principalTable: "NodeInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NodeInstance2RulePage_ibfk_1",
                        column: x => x.This2RulePage,
                        principalTable: "RulePages",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyInstances",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2PropertyTemplate = table.Column<Guid>(nullable: false),
                    This2NodeInstance = table.Column<Guid>(nullable: true),
                    This2VisuObjectInstance = table.Column<Guid>(nullable: true),
                    ValueString = table.Column<string>(type: "text", nullable: true),
                    ValueInt = table.Column<int>(type: "int(11)", nullable: true),
                    ValueBool = table.Column<bool>(nullable: true, defaultValue: false),
                    ValueDouble = table.Column<double>(nullable: true),
                    ValueLong = table.Column<long>(type: "bigint(64)", nullable: true),
                    ValueNodeInstance = table.Column<Guid>(nullable: true),
                    ValueRulePage = table.Column<Guid>(nullable: true),
                    ValueVisuPage = table.Column<Guid>(nullable: true),
                    ValueAreaInstance = table.Column<Guid>(nullable: true),
                    ValueCategoryInstance = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyInstances", x => x.ObjId);
                    table.ForeignKey(
                        name: "PropertyInstance_ibfk_2",
                        column: x => x.This2NodeInstance,
                        principalTable: "NodeInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PropertyInstance_ibfk_1",
                        column: x => x.This2PropertyTemplate,
                        principalTable: "PropertyTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "PropertyInstance_VisuObjectInstance_ibfk_3",
                        column: x => x.This2VisuObjectInstance,
                        principalTable: "VisuObjectInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PropertyInstance_AreaInstance_ValueAreaInstance",
                        column: x => x.ValueAreaInstance,
                        principalTable: "AreaInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PropertyInstance_CategoryInstance_ValueCategoryInstance",
                        column: x => x.ValueCategoryInstance,
                        principalTable: "CategoryInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PropertyInstance_NodeInstance_ValueNodeInstance",
                        column: x => x.ValueNodeInstance,
                        principalTable: "NodeInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "PropertyInstance_RulePage_ValueRulePage",
                        column: x => x.ValueRulePage,
                        principalTable: "RulePages",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "PropertyInstance_VisuPage_ValueVisuPage",
                        column: x => x.ValueVisuPage,
                        principalTable: "VisuPages",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTemplateConstraints",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 1024, nullable: false),
                    Description = table.Column<string>(maxLength: 1024, nullable: false, defaultValue: ""),
                    ConstraintType = table.Column<long>(type: "int(20)", nullable: false),
                    ConstraintLevel = table.Column<long>(type: "int(20)", nullable: false),
                    This2PropertyTemplate = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTemplateConstraints", x => x.ObjId);
                    table.ForeignKey(
                        name: "PropertyTemplateConstraint_PropertyTemplate_Navigation",
                        column: x => x.This2PropertyTemplate,
                        principalTable: "PropertyTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleInterfaceInstances",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2RuleInstance = table.Column<Guid>(nullable: false),
                    This2RuleInterfaceTemplate = table.Column<Guid>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                    ValueInteger = table.Column<long>(nullable: true),
                    ValueDouble = table.Column<double>(nullable: true),
                    ValueString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleInterfaceInstances", x => x.ObjId);
                    table.ForeignKey(
                        name: "RuleInterfaceInstance_ibfk_1",
                        column: x => x.This2RuleInstance,
                        principalTable: "RuleInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "RuleInterfaceInstance_ibfk_2",
                        column: x => x.This2RuleInterfaceTemplate,
                        principalTable: "RuleInterfaceTemplates",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTemplateConstraintData",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Factor = table.Column<double>(type: "double", nullable: false, defaultValue: 1.0),
                    Offset = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    PropertyKey = table.Column<string>(type: "varchar(1024)", nullable: true),
                    This2PropertyTemplateConstraint = table.Column<Guid>(nullable: false),
                    ConditionType = table.Column<long>(type: "int(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTemplateConstraintData", x => x.ObjId);
                    table.ForeignKey(
                        name: "This2PropertyTemplateConstraintNavigation_PropertyTemplate_Navigation",
                        column: x => x.This2PropertyTemplateConstraint,
                        principalTable: "PropertyTemplateConstraints",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Links",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2RulePage = table.Column<Guid>(nullable: false),
                    This2RuleInterfaceInstanceInput = table.Column<Guid>(nullable: true),
                    This2RuleInterfaceInstanceOutput = table.Column<Guid>(nullable: true),
                    This2NodeInstance2RulePageInput = table.Column<Guid>(nullable: true),
                    This2NodeInstance2RulePageOutput = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Links", x => x.ObjId);
                    table.ForeignKey(
                        name: "Link_ibfk_2",
                        column: x => x.This2NodeInstance2RulePageInput,
                        principalTable: "NodeInstance2RulePages",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Link_ibfk_5",
                        column: x => x.This2NodeInstance2RulePageOutput,
                        principalTable: "NodeInstance2RulePages",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Link_ibfk_3",
                        column: x => x.This2RuleInterfaceInstanceInput,
                        principalTable: "RuleInterfaceInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Link_ibfk_4",
                        column: x => x.This2RuleInterfaceInstanceOutput,
                        principalTable: "RuleInterfaceInstances",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Link_ibfk_1",
                        column: x => x.This2RulePage,
                        principalTable: "RulePages",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AreaInstances_This2AreaTemplate",
                table: "AreaInstances",
                column: "This2AreaTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_AreaInstances_This2Parent",
                table: "AreaInstances",
                column: "This2Parent");

            migrationBuilder.CreateIndex(
                name: "IX_AreaInstances_This2UserGroup",
                table: "AreaInstances",
                column: "This2UserGroup");

            migrationBuilder.CreateIndex(
                name: "IX_AreaTemplates_NeedsThis2AreaType",
                table: "AreaTemplates",
                column: "NeedsThis2AreaType");

            migrationBuilder.CreateIndex(
                name: "IX_AreaTemplates_ProvidesThis2AreayType",
                table: "AreaTemplates",
                column: "ProvidesThis2AreayType");

            migrationBuilder.CreateIndex(
                name: "IX_AreaTemplates_This2AreaType",
                table: "AreaTemplates",
                column: "This2AreaType");

            migrationBuilder.CreateIndex(
                name: "IX_BoardInterfaces_This2BoardType",
                table: "BoardInterfaces",
                column: "This2BoardType");

            migrationBuilder.CreateIndex(
                name: "IX_BoardInterfaces_This2InterfaceType",
                table: "BoardInterfaces",
                column: "This2InterfaceType");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryInstances_This2CategoryGroup",
                table: "CategoryInstances",
                column: "This2CategoryGroup");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryInstances_This2UserGroup",
                table: "CategoryInstances",
                column: "This2UserGroup");

            migrationBuilder.CreateIndex(
                name: "IX_Links_This2NodeInstance2RulePageInput",
                table: "Links",
                column: "This2NodeInstance2RulePageInput");

            migrationBuilder.CreateIndex(
                name: "IX_Links_This2NodeInstance2RulePageOutput",
                table: "Links",
                column: "This2NodeInstance2RulePageOutput");

            migrationBuilder.CreateIndex(
                name: "IX_Links_This2RuleInterfaceInstanceInput",
                table: "Links",
                column: "This2RuleInterfaceInstanceInput");

            migrationBuilder.CreateIndex(
                name: "IX_Links_This2RuleInterfaceInstanceOutput",
                table: "Links",
                column: "This2RuleInterfaceInstanceOutput");

            migrationBuilder.CreateIndex(
                name: "IX_Links_This2RulePage",
                table: "Links",
                column: "This2RulePage");

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstance2RulePages_This2NodeInstance",
                table: "NodeInstance2RulePages",
                column: "This2NodeInstance");

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstance2RulePages_This2RulePage",
                table: "NodeInstance2RulePages",
                column: "This2RulePage");

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstances_This2AreaInstance",
                table: "NodeInstances",
                column: "This2AreaInstance");

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstances_This2CategoryInstance",
                table: "NodeInstances",
                column: "This2CategoryInstance");

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstances_This2NodeTemplate",
                table: "NodeInstances",
                column: "This2NodeTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstances_This2ParentNodeInstance",
                table: "NodeInstances",
                column: "This2ParentNodeInstance");

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstances_This2UserGroup",
                table: "NodeInstances",
                column: "This2UserGroup");

            migrationBuilder.CreateIndex(
                name: "IX_NodeTemplates_NeedsInterface2InterfacesType",
                table: "NodeTemplates",
                column: "NeedsInterface2InterfacesType");

            migrationBuilder.CreateIndex(
                name: "IX_NodeTemplates_ProvidesInterface2InterfaceType",
                table: "NodeTemplates",
                column: "ProvidesInterface2InterfaceType");

            migrationBuilder.CreateIndex(
                name: "IX_NodeTemplates_This2DefaultMobileVisuTemplate",
                table: "NodeTemplates",
                column: "This2DefaultMobileVisuTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_NodeTemplates_This2NodeDataType",
                table: "NodeTemplates",
                column: "This2NodeDataType");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstances_This2NodeInstance",
                table: "PropertyInstances",
                column: "This2NodeInstance");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstances_This2PropertyTemplate",
                table: "PropertyInstances",
                column: "This2PropertyTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstances_This2VisuObjectInstance",
                table: "PropertyInstances",
                column: "This2VisuObjectInstance");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstances_ValueAreaInstance",
                table: "PropertyInstances",
                column: "ValueAreaInstance");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstances_ValueCategoryInstance",
                table: "PropertyInstances",
                column: "ValueCategoryInstance");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstances_ValueNodeInstance",
                table: "PropertyInstances",
                column: "ValueNodeInstance");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstances_ValueRulePage",
                table: "PropertyInstances",
                column: "ValueRulePage");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInstances_ValueVisuPage",
                table: "PropertyInstances",
                column: "ValueVisuPage");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTemplateConstraintData_This2PropertyTemplateConstraint",
                table: "PropertyTemplateConstraintData",
                column: "This2PropertyTemplateConstraint");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTemplateConstraints_This2PropertyTemplate",
                table: "PropertyTemplateConstraints",
                column: "This2PropertyTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTemplates_This2NodeTemplate",
                table: "PropertyTemplates",
                column: "This2NodeTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTemplates_This2PropertyType",
                table: "PropertyTemplates",
                column: "This2PropertyType");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTemplates_This2VisuObjectTemplate",
                table: "PropertyTemplates",
                column: "This2VisuObjectTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInstances_This2AreaInstance",
                table: "RuleInstances",
                column: "This2AreaInstance");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInstances_This2CategoryInstance",
                table: "RuleInstances",
                column: "This2CategoryInstance");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInstances_This2RulePage",
                table: "RuleInstances",
                column: "This2RulePage");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInstances_This2RuleTemplate",
                table: "RuleInstances",
                column: "This2RuleTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInstances_This2UserGroup",
                table: "RuleInstances",
                column: "This2UserGroup");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInterfaceInstances_This2RuleInstance",
                table: "RuleInterfaceInstances",
                column: "This2RuleInstance");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInterfaceInstances_This2RuleInterfaceTemplate",
                table: "RuleInterfaceInstances",
                column: "This2RuleInterfaceTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInterfaceTemplates_This2RuleInterfaceDirection",
                table: "RuleInterfaceTemplates",
                column: "This2RuleInterfaceDirection");

            migrationBuilder.CreateIndex(
                name: "IX_RuleInterfaceTemplates_This2RuleTemplate",
                table: "RuleInterfaceTemplates",
                column: "This2RuleTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_RulePages_This2RulePageType",
                table: "RulePages",
                column: "This2RulePageType");

            migrationBuilder.CreateIndex(
                name: "IX_RuleTemplates_This2DefaultMobileVisuTemplate",
                table: "RuleTemplates",
                column: "This2DefaultMobileVisuTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_User2Groups_This2UserGroup",
                table: "User2Groups",
                column: "This2UserGroup");

            migrationBuilder.CreateIndex(
                name: "IX_User2Roles_This2Role",
                table: "User2Roles",
                column: "This2Role");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroup2Roles_This2Role",
                table: "UserGroup2Roles",
                column: "This2Role");

            migrationBuilder.CreateIndex(
                name: "IX_VisuObjectInstances_This2UserGroup",
                table: "VisuObjectInstances",
                column: "This2UserGroup");

            migrationBuilder.CreateIndex(
                name: "IX_VisuObjectInstances_This2VisuObjectTemplate",
                table: "VisuObjectInstances",
                column: "This2VisuObjectTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_VisuObjectInstances_This2VisuPage",
                table: "VisuObjectInstances",
                column: "This2VisuPage");

            migrationBuilder.CreateIndex(
                name: "IX_VisuObjectTemplates_This2VisuPageType",
                table: "VisuObjectTemplates",
                column: "This2VisuPageType");

            migrationBuilder.CreateIndex(
                name: "IX_VisuPages_ObjId",
                table: "VisuPages",
                column: "ObjId");

            migrationBuilder.CreateIndex(
                name: "IX_VisuPages_This2UserGroup",
                table: "VisuPages",
                column: "This2UserGroup");

            migrationBuilder.CreateIndex(
                name: "This2VisuPageType",
                table: "VisuPages",
                column: "This2VisuPageType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardInterfaces");

            migrationBuilder.DropTable(
                name: "Links");

            migrationBuilder.DropTable(
                name: "Priviledge2Roles");

            migrationBuilder.DropTable(
                name: "PropertyInstances");

            migrationBuilder.DropTable(
                name: "PropertyTemplateConstraintData");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "User2Groups");

            migrationBuilder.DropTable(
                name: "User2Roles");

            migrationBuilder.DropTable(
                name: "UserGroup2Roles");

            migrationBuilder.DropTable(
                name: "VersionInformations");

            migrationBuilder.DropTable(
                name: "BoardTypes");

            migrationBuilder.DropTable(
                name: "NodeInstance2RulePages");

            migrationBuilder.DropTable(
                name: "RuleInterfaceInstances");

            migrationBuilder.DropTable(
                name: "Priviledges");

            migrationBuilder.DropTable(
                name: "VisuObjectInstances");

            migrationBuilder.DropTable(
                name: "PropertyTemplateConstraints");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "NodeInstances");

            migrationBuilder.DropTable(
                name: "RuleInstances");

            migrationBuilder.DropTable(
                name: "RuleInterfaceTemplates");

            migrationBuilder.DropTable(
                name: "VisuPages");

            migrationBuilder.DropTable(
                name: "PropertyTemplates");

            migrationBuilder.DropTable(
                name: "AreaInstances");

            migrationBuilder.DropTable(
                name: "CategoryInstances");

            migrationBuilder.DropTable(
                name: "RulePages");

            migrationBuilder.DropTable(
                name: "RuleInterfaceDirections");

            migrationBuilder.DropTable(
                name: "RuleTemplates");

            migrationBuilder.DropTable(
                name: "NodeTemplates");

            migrationBuilder.DropTable(
                name: "PropertyTypes");

            migrationBuilder.DropTable(
                name: "AreaTemplates");

            migrationBuilder.DropTable(
                name: "CategoryGroups");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "RulePageTypes");

            migrationBuilder.DropTable(
                name: "InterfaceTypes");

            migrationBuilder.DropTable(
                name: "VisuObjectTemplates");

            migrationBuilder.DropTable(
                name: "NodeDataTypes");

            migrationBuilder.DropTable(
                name: "AreaTypes");

            migrationBuilder.DropTable(
                name: "VisuPageTypes");
        }
    }
}
