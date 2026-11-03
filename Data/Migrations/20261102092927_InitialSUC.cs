using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuc.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSUC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ProviderVerified = table.Column<bool>(type: "bit", nullable: false),
                    SessionStamp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentGovernanceDecisionId = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorId = table.Column<int>(type: "int", nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanionAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionId = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    ProposerId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: true),
                    CostSharing = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RespondedBy = table.Column<int>(type: "int", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedBy = table.Column<int>(type: "int", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanionAgreements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CompanionId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemoSeedMarkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSeedMarkers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DossierSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierId = table.Column<int>(type: "int", nullable: false),
                    Submission = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Representative = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossierSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceReminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderExplanations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewerId = table.Column<int>(type: "int", nullable: true),
                    ReviewReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewEvidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderExplanations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedReferenceLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReferenceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TravelStyle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Wishes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharingScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShareContact = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    People = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companions_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Before = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    After = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Until = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovernanceDecisions_Accounts_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GovernanceDecisions_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrivateDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ObjectId = table.Column<int>(type: "int", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mime = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Submission = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivateDocuments_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProviderProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderProfiles_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAccepting = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServingHours = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosureStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosureEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosureReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosureScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedRevisionNumber = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    BlockedByDecisionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Residence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Interests = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharingScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShareContact = table.Column<bool>(type: "bit", nullable: false),
                    AvatarDocumentId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerificationDossiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Representative = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Submission = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationDossiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationDossiers_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedReferenceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SavedReferenceListId = table.Column<int>(type: "int", nullable: false),
                    PlaceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReferenceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedReferenceItems_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedReferenceItems_SavedReferenceLists_SavedReferenceListId",
                        column: x => x.SavedReferenceListId,
                        principalTable: "SavedReferenceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    TravelServiceId = table.Column<int>(type: "int", nullable: false),
                    SnapshotName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    People = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Guests = table.Column<int>(type: "int", nullable: false),
                    ArrivalDeparture = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionsVersion = table.Column<int>(type: "int", nullable: false),
                    CustomerAcceptedVersion = table.Column<int>(type: "int", nullable: false),
                    ProviderAcceptedVersion = table.Column<int>(type: "int", nullable: false),
                    ProviderOfferReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Services_TravelServiceId",
                        column: x => x.TravelServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TravelServiceId = table.Column<int>(type: "int", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    ServingHours = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosureStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosureEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosureReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosureScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewedById = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRevisions_Services_TravelServiceId",
                        column: x => x.TravelServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    PlaceId = table.Column<int>(type: "int", nullable: true),
                    TravelServiceId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    HasTime = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    TripId = table.Column<int>(type: "int", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerificationDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DossierId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationDecisions_Accounts_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationDecisions_VerificationDossiers_DossierId",
                        column: x => x.DossierId,
                        principalTable: "VerificationDossiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    RequesterId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseConditionsVersion = table.Column<int>(type: "int", nullable: false),
                    OldConditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Guests = table.Column<int>(type: "int", nullable: false),
                    People = table.Column<int>(type: "int", nullable: false),
                    ArrivalDeparture = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderOfferReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogicalVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingChanges_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingConditionVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingConditionVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingConditionVersions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingItemResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    ItemKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeedbackAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingItemResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingItemResults_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentId = table.Column<int>(type: "int", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentActivities_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentActivities_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChosenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentPlans_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingResultHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingItemResultId = table.Column<int>(type: "int", nullable: false),
                    PreviousOutcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewOutcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingResultHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingResultHistories_BookingItemResults_BookingItemResultId",
                        column: x => x.BookingItemResultId,
                        principalTable: "BookingItemResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Phone",
                table: "Accounts",
                column: "Phone",
                unique: true,
                filter: "[Phone] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TripId",
                table: "Activities",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingChanges_BookingId",
                table: "BookingChanges",
                column: "BookingId",
                unique: true,
                filter: "[Status] IN (N'Chờ phản hồi',N'Đề xuất khác')");

            migrationBuilder.CreateIndex(
                name: "IX_BookingConditionVersions_BookingId_VersionNumber",
                table: "BookingConditionVersions",
                columns: new[] { "BookingId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingItemResults_BookingId_ItemKey",
                table: "BookingItemResults",
                columns: new[] { "BookingId", "ItemKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingResultHistories_BookingItemResultId",
                table: "BookingResultHistories",
                column: "BookingItemResultId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_OperationId",
                table: "Bookings",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_OwnerId",
                table: "Bookings",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TravelServiceId",
                table: "Bookings",
                column: "TravelServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanionAgreements_ConnectionId_Revision",
                table: "CompanionAgreements",
                columns: new[] { "ConnectionId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CompanionAgreement_Active",
                table: "CompanionAgreements",
                column: "ConnectionId",
                unique: true,
                filter: "[Status] = N'Đã xác nhận'");

            migrationBuilder.CreateIndex(
                name: "UX_CompanionAgreement_Pending",
                table: "CompanionAgreements",
                column: "ConnectionId",
                unique: true,
                filter: "[Status] = N'Chờ xác nhận'");

            migrationBuilder.CreateIndex(
                name: "IX_Companions_OwnerId",
                table: "Companions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_CompanionId_SenderId",
                table: "Connections",
                columns: new[] { "CompanionId", "SenderId" },
                unique: true,
                filter: "[Status] IN (N'Chờ phản hồi',N'Đã chấp nhận')");

            migrationBuilder.CreateIndex(
                name: "IX_DossierSubmissions_DossierId_Submission",
                table: "DossierSubmissions",
                columns: new[] { "DossierId", "Submission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceDecisions_AdminId",
                table: "GovernanceDecisions",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceDecisions_OperationId",
                table: "GovernanceDecisions",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceDecisions_OwnerId",
                table: "GovernanceDecisions",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceReminders_DecisionId",
                table: "GovernanceReminders",
                column: "DecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentActivities_ActivityId",
                table: "IncidentActivities",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentActivities_IncidentId_ActivityId",
                table: "IncidentActivities",
                columns: new[] { "IncidentId", "ActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentPlans_IncidentId",
                table: "IncidentPlans",
                column: "IncidentId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_TripId",
                table: "Incidents",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OwnerId_CreatedAt",
                table: "Notifications",
                columns: new[] { "OwnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateDocuments_OwnerId_Scope_ObjectId",
                table: "PrivateDocuments",
                columns: new[] { "OwnerId", "Scope", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateDocuments_StorageKey",
                table: "PrivateDocuments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderExplanations_OwnerId_DecisionId",
                table: "ProviderExplanations",
                columns: new[] { "OwnerId", "DecisionId" },
                unique: true,
                filter: "[Status] = N'Chờ xem xét'");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderProfiles_OwnerId",
                table: "ProviderProfiles",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedReferenceItems_PlaceId",
                table: "SavedReferenceItems",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedReferenceItems_SavedReferenceListId",
                table: "SavedReferenceItems",
                column: "SavedReferenceListId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedReferenceLists_OperationId",
                table: "SavedReferenceLists",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRevisions_TravelServiceId",
                table: "ServiceRevisions",
                column: "TravelServiceId",
                unique: true,
                filter: "[Status] = N'Chờ duyệt'");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRevisions_TravelServiceId_RevisionNumber",
                table: "ServiceRevisions",
                columns: new[] { "TravelServiceId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_OwnerId",
                table: "Services",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_OwnerId",
                table: "Trips",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_OwnerId",
                table: "UserProfiles",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationDecisions_AdminId",
                table: "VerificationDecisions",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationDecisions_DossierId_Sequence",
                table: "VerificationDecisions",
                columns: new[] { "DossierId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationDossiers_OwnerId",
                table: "VerificationDossiers",
                column: "OwnerId",
                unique: true,
                filter: "[Status] IN (N'Chờ xác minh',N'Cần bổ sung')");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationDossiers_OwnerId_SubmittedAt",
                table: "VerificationDossiers",
                columns: new[] { "OwnerId", "SubmittedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "BookingChanges");

            migrationBuilder.DropTable(
                name: "BookingConditionVersions");

            migrationBuilder.DropTable(
                name: "BookingResultHistories");

            migrationBuilder.DropTable(
                name: "CompanionAgreements");

            migrationBuilder.DropTable(
                name: "Companions");

            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropTable(
                name: "DemoSeedMarkers");

            migrationBuilder.DropTable(
                name: "DossierSubmissions");

            migrationBuilder.DropTable(
                name: "GovernanceDecisions");

            migrationBuilder.DropTable(
                name: "GovernanceReminders");

            migrationBuilder.DropTable(
                name: "IncidentActivities");

            migrationBuilder.DropTable(
                name: "IncidentPlans");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PrivateDocuments");

            migrationBuilder.DropTable(
                name: "ProviderExplanations");

            migrationBuilder.DropTable(
                name: "ProviderProfiles");

            migrationBuilder.DropTable(
                name: "SavedReferenceItems");

            migrationBuilder.DropTable(
                name: "ServiceRevisions");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "VerificationDecisions");

            migrationBuilder.DropTable(
                name: "BookingItemResults");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropTable(
                name: "SavedReferenceLists");

            migrationBuilder.DropTable(
                name: "VerificationDossiers");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
