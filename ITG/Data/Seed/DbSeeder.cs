using System.Text.Json;
using ITG.Constants;
using ITG.Models.Entities;
using ITG.Models.Enums;
using ITG.Services.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ITG.Data.Seed;

// Creates roles, the admin account and (in Development) a full set of demo data
// that covers every scenario needed for the presentation.
public static class DbSeeder
{
    private const string SampleEmployeePassword = "Employee@123";

    public static async Task SeedAsync(IServiceProvider services, bool includeSampleData)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<AppDbContext>();
        var userManager = provider.GetRequiredService<UserManager<Employee>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var environment = provider.GetRequiredService<IWebHostEnvironment>();

        // Applies any pending migrations automatically (no need for Update-Database)
        await context.Database.MigrateAsync();

        // Reading data never waits for another connection that is writing (or that left a table locked).
        // Without this, a page can hang for 30 seconds and then fail with "Execution Timeout Expired".
        // On shared hosting the database user may not be allowed to change this: the site still works without it.
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "IF (SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME()) = 0 " +
                "ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;");
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            // Not allowed on this server: keep the default setting
        }

        await SeedRolesAsync(roleManager);
        var admin = await SeedAdminAsync(userManager, configuration);

        EnsureSampleCertificateFile(environment);

        // Demo data is added only when asked for, and only once
        if (includeSampleData && !await context.Departments.AnyAsync())
        {
            await SeedDemoDataAsync(context, userManager, admin);
        }
    }

    // ---------------- Roles ----------------
    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { AppRoles.Admin, AppRoles.Employee };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // ---------------- Admin ----------------
    private static async Task<Employee> SeedAdminAsync(
        UserManager<Employee> userManager, IConfiguration configuration)
    {
        var name = configuration["SeedAdmin:Name"] ?? "System Admin";
        var email = configuration["SeedAdmin:Email"]
            ?? throw new InvalidOperationException("SeedAdmin:Email is missing in appsettings.json");
        var password = configuration["SeedAdmin:Password"]
            ?? throw new InvalidOperationException("SeedAdmin:Password is missing in appsettings.json");

        return await CreateUserAsync(userManager, name, email, password, "ADM-001",
            AppRoles.Admin, departmentId: null, positionId: null);
    }

    // ---------------- Sample certificate file ----------------
    // A small PDF used by all demo uploads, so the admin can open them.
    private static void EnsureSampleCertificateFile(IWebHostEnvironment environment)
    {
        var folder = Path.Combine(environment.ContentRootPath, AppConstants.CertificateStorageFolder);
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, AppConstants.SampleCertificateFileName);
        if (!File.Exists(filePath))
        {
            File.WriteAllBytes(filePath, Convert.FromBase64String(SampleCertificatePdfBase64));
        }
    }

    // ---------------- Demo data ----------------
    private static async Task SeedDemoDataAsync(
        AppDbContext context, UserManager<Employee> userManager, Employee admin)
    {
        // ===== Departments =====
        var development = new Department { Name = "Development" };
        var qualityAssurance = new Department { Name = "Quality Assurance" };
        var humanResources = new Department { Name = "Human Resources" };
        var infrastructure = new Department { Name = "Infrastructure" };
        var finance = new Department { Name = "Finance" };
        context.Departments.AddRange(development, qualityAssurance, humanResources, infrastructure, finance);

        // ===== Career paths =====
        // Software Development forks (after Senior Developer) into Management or Architecture
        var softwarePath = NewPath("Software Development", "Building and maintaining software applications.");
        var managementPath = NewPath("Engineering Management", "Leading development teams and delivery.", softwarePath);
        var architecturePath = NewPath("Software Architecture", "Designing large-scale technical solutions.", softwarePath);
        var qaPath = NewPath("Quality Assurance", "Ensuring software quality through testing.");
        var hrPath = NewPath("Human Resources", "Supporting people, hiring and employee development.");
        var infraPath = NewPath("Infrastructure", "Running and securing the company's IT systems.");
        var financePath = NewPath("Finance", "Managing accounts, reporting and financial planning.");
        context.CareerPaths.AddRange(softwarePath, managementPath, architecturePath, qaPath, hrPath, infraPath, financePath);

        // ===== Positions =====
        var juniorDeveloper = NewPosition("Junior Developer", 1, softwarePath);
        var softwareDeveloper = NewPosition("Software Developer", 2, softwarePath);
        var seniorDeveloper = NewPosition("Senior Developer", 3, softwarePath, isFork: true);
        var teamLead = NewPosition("Team Lead", 1, managementPath);
        var engineeringManager = NewPosition("Engineering Manager", 2, managementPath);
        var solutionArchitect = NewPosition("Solution Architect", 1, architecturePath);
        var enterpriseArchitect = NewPosition("Enterprise Architect", 2, architecturePath);

        var qaTester = NewPosition("QA Tester", 1, qaPath);
        var qaEngineer = NewPosition("QA Engineer", 2, qaPath);
        var seniorQaEngineer = NewPosition("Senior QA Engineer", 3, qaPath);

        var hrAssistant = NewPosition("HR Assistant", 1, hrPath);
        var hrSpecialist = NewPosition("HR Specialist", 2, hrPath);
        var hrManager = NewPosition("HR Manager", 3, hrPath);

        var supportTechnician = NewPosition("IT Support Technician", 1, infraPath);
        var systemsEngineer = NewPosition("Systems Engineer", 2, infraPath);
        var seniorSystemsEngineer = NewPosition("Senior Systems Engineer", 3, infraPath);

        var juniorAccountant = NewPosition("Junior Accountant", 1, financePath);
        var accountant = NewPosition("Accountant", 2, financePath);
        var seniorAccountant = NewPosition("Senior Accountant", 3, financePath);

        context.Positions.AddRange(
            juniorDeveloper, softwareDeveloper, seniorDeveloper, teamLead, engineeringManager,
            solutionArchitect, enterpriseArchitect, qaTester, qaEngineer, seniorQaEngineer,
            hrAssistant, hrSpecialist, hrManager, supportTechnician, systemsEngineer,
            seniorSystemsEngineer, juniorAccountant, accountant, seniorAccountant);

        // ===== Certificates =====
        var csharp = NewCertificate("C# Fundamentals", "Technical", "Core C# syntax, types and object-oriented programming.");
        var htmlCss = NewCertificate("HTML & CSS Basics", "Technical", "Building and styling responsive web pages.");
        var git = NewCertificate("Git & Version Control", "Technical", "Branching, merging and collaborative workflows.");
        var sql = NewCertificate("SQL Fundamentals", "Technical", "Queries, joins, aggregation and database design basics.");
        var javascript = NewCertificate("JavaScript Essentials", "Technical", "Core language concepts, async code and modern syntax.");
        var systemDesign = NewCertificate("System Design Foundations", "Technical", "Build the system design skills for your target role.");
        var scrum = NewCertificate("Agile & Scrum Master", "Management", "Running agile teams with Scrum.");
        var leadership = NewCertificate("Leadership Essentials", "Management", "Motivating and guiding people and teams.");
        var projectManagement = NewCertificate("Project Management Basics", "Management", "Planning, scheduling and delivering projects.");
        var pmp = NewCertificate("Project Management Professional (PMP)", "Management", "Advanced project management certification.");
        var peopleManagement = NewCertificate("People Management", "Management", "Hiring, coaching and growing engineers.");
        var cloud = NewCertificate("Cloud Computing Basics", "Technical", "Cloud service models, deployment and core services.");
        var microservices = NewCertificate("Microservices Architecture", "Technical", "Designing distributed, service-based systems.");
        var togaf = NewCertificate("TOGAF Enterprise Architecture", "Technical", "Enterprise architecture framework and methods.");
        var cloudArchitect = NewCertificate("Cloud Architect Professional", "Technical", "Designing secure, scalable cloud solutions.");
        var istqb = NewCertificate("ISTQB Foundation Level", "Technical", "Software testing principles and terminology.");
        var testingBasics = NewCertificate("Software Testing Basics", "Technical", "Test cases, bug reports and testing techniques.");
        var automation = NewCertificate("Test Automation with Selenium", "Technical", "Automating web application tests.");
        var istqbAdvanced = NewCertificate("ISTQB Advanced Test Analyst", "Technical", "Advanced test analysis and design.");
        var performance = NewCertificate("Performance Testing", "Technical", "Load, stress and performance testing.");
        var hrOperations = NewCertificate("HR & People Operations", "Business & People", "HR processes, policies and employee lifecycle.");
        var recruitment = NewCertificate("Recruitment Fundamentals", "Business & People", "Sourcing, interviewing and hiring.");
        var laborLaw = NewCertificate("Labor Law Basics", "Business & People", "Employment law essentials.");
        var shrm = NewCertificate("SHRM Certified Professional", "Business & People", "Professional HR certification.");
        var performanceManagement = NewCertificate("Performance Management", "Business & People", "Goals, reviews and employee growth.");
        var comptiaA = NewCertificate("CompTIA A+", "Technical", "Hardware, operating systems and troubleshooting.");
        var comptiaNetwork = NewCertificate("CompTIA Network+", "Technical", "Networking concepts and infrastructure.");
        var windowsServer = NewCertificate("Windows Server Administration", "Technical", "Managing Windows Server environments.");
        var ccna = NewCertificate("Cisco CCNA", "Technical", "Routing, switching and network security.");
        var azureAdmin = NewCertificate("Azure Administrator", "Technical", "Managing Microsoft Azure resources.");
        var linux = NewCertificate("Linux Administration", "Technical", "Managing Linux servers.");
        var accounting = NewCertificate("Financial Accounting Basics", "Business & People", "Recording transactions and financial statements.");
        var excel = NewCertificate("Excel for Finance", "Business & People", "Spreadsheets, formulas and financial models.");
        var tax = NewCertificate("Tax Fundamentals", "Business & People", "Corporate and income tax basics.");
        var cma = NewCertificate("Certified Management Accountant (CMA)", "Business & People", "Management accounting certification.");
        var ifrs = NewCertificate("IFRS Certificate", "Business & People", "International financial reporting standards.");
        var analysis = NewCertificate("Financial Analysis", "Business & People", "Analyzing financial performance.");

        context.Certificates.AddRange(
            csharp, htmlCss, git, sql, javascript, systemDesign, scrum, leadership, projectManagement, pmp,
            peopleManagement, cloud, microservices, togaf, cloudArchitect, istqb, testingBasics, automation,
            istqbAdvanced, performance, hrOperations, recruitment, laborLaw, shrm, performanceManagement,
            comptiaA, comptiaNetwork, windowsServer, ccna, azureAdmin, linux, accounting, excel, tax,
            cma, ifrs, analysis);

        // ===== Roadmaps: certificates required to REACH each position (in order) =====
        // Note: some certificates appear in more than one roadmap (e.g. Git, System Design),
        // so an employee keeps that achievement when moving to the next roadmap.
        AddRoadmap(context, softwareDeveloper, csharp, htmlCss, git);
        AddRoadmap(context, seniorDeveloper, sql, javascript, git, systemDesign);
        AddRoadmap(context, teamLead, scrum, leadership, projectManagement);
        AddRoadmap(context, engineeringManager, pmp, peopleManagement);
        AddRoadmap(context, solutionArchitect, cloud, systemDesign, microservices);
        AddRoadmap(context, enterpriseArchitect, togaf, cloudArchitect);
        AddRoadmap(context, qaEngineer, istqb, testingBasics, sql);
        AddRoadmap(context, seniorQaEngineer, automation, istqbAdvanced, performance);
        AddRoadmap(context, hrSpecialist, hrOperations, recruitment, laborLaw);
        AddRoadmap(context, hrManager, shrm, leadership, performanceManagement);
        AddRoadmap(context, systemsEngineer, comptiaA, comptiaNetwork, windowsServer);
        AddRoadmap(context, seniorSystemsEngineer, ccna, azureAdmin, linux);
        AddRoadmap(context, accountant, accounting, excel, tax);
        AddRoadmap(context, seniorAccountant, cma, ifrs, analysis);

        // ===== AI self-assessment exams =====
        // Each exam is linked to the positions whose employees see it (current role or next role)
        var sqlExam = NewExam("SQL Fundamentals", "Technical", "Test your SQL querying and database design skills.", 30, 20,
            softwareDeveloper, seniorDeveloper, qaEngineer);
        var restExam = NewExam("REST API Design", "Technical", "Resources, HTTP methods, status codes and API best practices.", 30, 20,
            softwareDeveloper, seniorDeveloper);
        var designExam = NewExam("System Design Foundations", "Technical", "Scalability, caching, databases and system trade-offs.", 30, 15,
            seniorDeveloper, solutionArchitect, enterpriseArchitect);
        var testingExam = NewExam("Software Testing Basics", "Technical", "Testing levels, techniques and defect management.", 30, 15,
            qaTester, qaEngineer, seniorQaEngineer);
        var jsExam = NewExam("JavaScript Essentials", "Technical", "Core JavaScript concepts, async code and the DOM.", 30, 20,
            juniorDeveloper, softwareDeveloper, seniorDeveloper);
        var dataExam = NewExam("Data Structures", "Technical", "Arrays, lists, trees, hash tables and complexity.", 30, 15,
            juniorDeveloper, softwareDeveloper);
        var cloudExam = NewExam("Cloud Computing Basics", "Technical", "Cloud models, core services and deployment.", 30, 15,
            solutionArchitect, enterpriseArchitect, systemsEngineer, seniorSystemsEngineer);
        var aiExam = NewExam("AI Fundamentals", "Technical", "Machine learning basics and AI concepts.", 30, 15,
            seniorDeveloper, solutionArchitect);
        var architectureExam = NewExam("Software Architecture Patterns", "Technical", "Layered, event-driven and microservice architectures.", 30, 15,
            solutionArchitect, enterpriseArchitect);
        var networkExam = NewExam("Networking Fundamentals", "Technical", "IP addressing, routing, DNS and network troubleshooting.", 30, 15,
            supportTechnician, systemsEngineer, seniorSystemsEngineer);
        var pmExam = NewExam("Project Management Basics", "Management", "Planning, risk, scheduling and stakeholder management.", 30, 15,
            teamLead, engineeringManager, hrManager);
        var scrumExam = NewExam("Agile & Scrum", "Management", "Scrum roles, events and artifacts.", 30, 15,
            teamLead, engineeringManager, seniorQaEngineer);
        var hrExam = NewExam("HR & People Operations", "Business & People", "HR processes, policies and the employee lifecycle.", 30, 15,
            hrAssistant, hrSpecialist, hrManager);
        var accountingExam = NewExam("Financial Accounting Basics", "Business & People", "Accounting principles and financial statements.", 30, 15,
            juniorAccountant, accountant, seniorAccountant);

        context.Exams.AddRange(sqlExam, restExam, designExam, testingExam, jsExam, dataExam,
            cloudExam, aiExam, architectureExam, networkExam, pmExam, scrumExam, hrExam, accountingExam);

        await context.SaveChangesAsync();

        // ===== Demo employees (password: Employee@123) =====
        var omar = await CreateEmployeeAsync(userManager, "Omar Haddad", "omar@itg.com", "EMP-001", development, softwareDeveloper);
        var sara = await CreateEmployeeAsync(userManager, "Sara Nasser", "sara@itg.com", "EMP-002", qualityAssurance, qaTester);
        var lina = await CreateEmployeeAsync(userManager, "Lina Saleh", "lina@itg.com", "EMP-003", humanResources, hrAssistant);
        var kareem = await CreateEmployeeAsync(userManager, "Kareem Musa", "kareem@itg.com", "EMP-004", infrastructure, supportTechnician);
        var noor = await CreateEmployeeAsync(userManager, "Noor Adel", "noor@itg.com", "EMP-005", finance, juniorAccountant);
        var ali = await CreateEmployeeAsync(userManager, "Ali Mansour", "ali@itg.com", "EMP-006", development, seniorDeveloper);
        var yousef = await CreateEmployeeAsync(userManager, "Yousef Khalil", "yousef@itg.com", "EMP-007", development, seniorDeveloper);
        var rana = await CreateEmployeeAsync(userManager, "Rana Odeh", "rana@itg.com", "EMP-008", development, juniorDeveloper);
        var ahmad = await CreateEmployeeAsync(userManager, "Ahmad Ali", "ahmad@itg.com", "EMP-009", development, softwareDeveloper);
        var mona = await CreateEmployeeAsync(userManager, "Mona Fares", "mona@itg.com", "EMP-010", finance, accountant);

        // Fork demo (both are Senior Developers):
        // - Yousef: HR assigned him the Architecture path (locked, only HR can change it)
        // - Ali: reached the fork and waits for HR to set his next path
        yousef.SelectedCareerPathId = architecturePath.Id;
        await userManager.UpdateAsync(yousef);

        // Rana's skills assessment was re-opened by the admin
        rana.CanRetakeSkillsAssessment = true;
        await userManager.UpdateAsync(rana);

        // ===== Certificate uploads =====
        var verified = VerificationStatus.Verified;
        var pending = VerificationStatus.Pending;
        var rejected = VerificationStatus.Rejected;

        // Omar: Software Developer -> Senior Developer = 2 of 4 verified (50%), Git waiting for review
        AddUpload(context, omar, sql, verified, 40, admin);
        AddUpload(context, omar, javascript, verified, 25, admin);
        AddUpload(context, omar, git, pending, 2, admin);

        // Sara: QA Tester -> QA Engineer = 3 of 3 verified (100%, ready for promotion)
        AddUpload(context, sara, istqb, verified, 60, admin);
        AddUpload(context, sara, testingBasics, verified, 45, admin);
        AddUpload(context, sara, sql, verified, 20, admin);

        // Lina: 1 verified, 1 rejected (must upload again)
        AddUpload(context, lina, hrOperations, verified, 30, admin);
        AddUpload(context, lina, recruitment, rejected, 6, admin,
            "The uploaded file is not clear. Please upload a readable copy of the certificate.");

        // Kareem: only one upload, still waiting for review (0%)
        AddUpload(context, kareem, comptiaA, pending, 1, admin);

        // Noor: new employee, nothing uploaded yet

        // Ali: reached Senior Developer (fork) with his full previous roadmap verified
        AddUpload(context, ali, sql, verified, 200, admin);
        AddUpload(context, ali, javascript, verified, 190, admin);
        AddUpload(context, ali, git, verified, 180, admin);
        AddUpload(context, ali, systemDesign, verified, 170, admin);

        // Yousef: chose Architecture. System Design (from his previous roadmap) still counts.
        AddUpload(context, yousef, systemDesign, verified, 150, admin);
        AddUpload(context, yousef, cloud, pending, 3, admin);

        // Rana: Junior -> Software Developer = 2 of 3 verified
        AddUpload(context, rana, csharp, verified, 35, admin);
        AddUpload(context, rana, htmlCss, verified, 15, admin);

        // Ahmad: promoted last month; Git from his old roadmap counts in the new one (1 of 4)
        AddUpload(context, ahmad, csharp, verified, 120, admin);
        AddUpload(context, ahmad, htmlCss, verified, 100, admin);
        AddUpload(context, ahmad, git, verified, 80, admin);

        // Mona: Accountant -> Senior Accountant = 3 of 3 verified (100%)
        AddUpload(context, mona, cma, verified, 50, admin);
        AddUpload(context, mona, ifrs, verified, 35, admin);
        AddUpload(context, mona, analysis, verified, 10, admin);

        // ===== Promotion history (spread over the last months for the Analytics chart) =====
        AddPromotion(context, ali, softwareDeveloper, seniorDeveloper, admin, 160);
        AddPromotion(context, yousef, softwareDeveloper, seniorDeveloper, admin, 140);
        AddPromotion(context, mona, juniorAccountant, accountant, admin, 100);
        AddPromotion(context, ahmad, juniorDeveloper, softwareDeveloper, admin, 30);

        // ===== Soft-skills assessments (overall = average of the four skills) =====
        // Omar took the assessment twice (HR re-opened it), so Home shows the comparison
        AddSkillAssessment(context, omar, 72, 78, 80, 55, 120);
        AddSkillAssessment(context, omar, 85, 85, 75, 67, 20);
        AddSkillAssessment(context, sara, 90, 92, 85, 85, 40);
        AddSkillAssessment(context, lina, 80, 65, 78, 65, 15);
        AddSkillAssessment(context, ali, 80, 90, 82, 84, 60);
        AddSkillAssessment(context, yousef, 78, 88, 80, 78, 55);
        AddSkillAssessment(context, rana, 70, 68, 62, 60, 25);
        AddSkillAssessment(context, mona, 88, 84, 86, 82, 30);

        // ===== Exam attempts (score, days ago) =====
        AddExamAttempts(context, omar, sqlExam, (70, 30), (85, 12));
        AddExamAttempts(context, omar, jsExam, (78, 20));
        AddExamAttempts(context, omar, aiExam, (88, 8));
        AddExamAttempts(context, omar, dataExam, (52, 4));
        AddExamAttempts(context, sara, testingExam, (90, 25));
        AddExamAttempts(context, sara, sqlExam, (81, 18));
        AddExamAttempts(context, lina, hrExam, (55, 10), (72, 5));
        AddExamAttempts(context, ali, designExam, (92, 50));
        AddExamAttempts(context, ali, dataExam, (74, 40));
        AddExamAttempts(context, ali, restExam, (86, 9));
        AddExamAttempts(context, yousef, cloudExam, (60, 30), (72, 20), (82, 6));
        AddExamAttempts(context, yousef, designExam, (88, 45));
        AddExamAttempts(context, rana, jsExam, (64, 7));
        AddExamAttempts(context, mona, accountingExam, (94, 22));

        // ===== Notifications (bell menu) =====
        AddNotification(context, omar, "Your certificate \"{0}\" was approved.", "bi-patch-check-fill", "/Roadmap", 39, true, "SQL Fundamentals");
        AddNotification(context, omar, "Your certificate \"{0}\" was approved.", "bi-patch-check-fill", "/Roadmap", 24, false, "JavaScript Essentials");
        AddNotification(context, lina, "Your certificate \"{0}\" was rejected. See the reason and upload it again.", "bi-x-octagon", "/Roadmap", 6, false, "Recruitment Fundamentals");
        AddNotification(context, yousef, "HR set your next career path: {0}.", "bi-signpost-split", "/Roadmap", 140, false, "Solution Architect · Software Architecture");
        AddNotification(context, rana, "HR re-opened your skills assessment. You can take it again.", "bi-unlock", "/SkillsAssessment", 2, false);
        AddNotification(context, ahmad, "Congratulations! You were promoted to {0}.", "bi-trophy", "/Home", 30, true, "Software Developer");

        AddNotification(context, admin, "{0} completed all requirements for {1}.", "bi-award", $"/Admin/Employees/Details/{sara.Id}", 20, true, sara.Name, "QA Engineer");
        AddNotification(context, admin, "{0} completed all requirements for {1}.", "bi-award", $"/Admin/Employees/Details/{mona.Id}", 10, false, mona.Name, "Senior Accountant");
        AddNotification(context, admin, "{0} completed the current career stage. Please set the next career path.", "bi-signpost-split", $"/Admin/Employees/Details/{ali.Id}", 5, false, ali.Name);
        AddNotification(context, admin, "{0} uploaded \"{1}\" for review.", "bi-file-earmark-arrow-up", "/Admin/Reviews", 3, false, yousef.Name, "Cloud Computing Basics");
        AddNotification(context, admin, "{0} uploaded \"{1}\" for review.", "bi-file-earmark-arrow-up", "/Admin/Reviews", 2, false, omar.Name, "Git & Version Control");
        AddNotification(context, admin, "{0} uploaded \"{1}\" for review.", "bi-file-earmark-arrow-up", "/Admin/Reviews", 1, false, kareem.Name, "CompTIA A+");

        // ===== Omar's AI development plan (ready-made, so the demo works even without the AI key) =====
        context.DevelopmentPlans.Add(new DevelopmentPlan
        {
            EmployeeId = omar.Id,
            Language = "en",
            CreatedAt = DateTime.UtcNow.AddDays(-9),
            ContentJson = JsonSerializer.Serialize(DemoPlan(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        });

        await context.SaveChangesAsync();
    }

    private static PlanContent DemoPlan() => new()
    {
        Summary = "You are halfway to Senior Developer. Finish Git and System Design, and keep practising leadership, your lowest soft skill.",
        Focus = new()
        {
            new() { Title = "Git & Version Control", Reason = "It is your next roadmap certificate and it is already waiting for review." },
            new() { Title = "System Design", Reason = "The last certificate for Senior Developer and a key exam for the role." },
            new() { Title = "Leadership", Reason = "Your lowest soft-skill score (67), important for a senior role." }
        },
        Weeks = new()
        {
            new()
            {
                Title = "Close the open items",
                Tasks = new()
                {
                    new() { Text = "Follow up on your Git & Version Control certificate review with HR.", Type = "certificate", Done = true },
                    new() { Text = "Retake the Data Structures exam after reviewing trees and hash tables.", Type = "exam", Done = true },
                    new() { Text = "Lead one short stand-up meeting for your team.", Type = "skill", Done = false }
                }
            },
            new()
            {
                Title = "Start System Design",
                Tasks = new()
                {
                    new() { Text = "Study caching, load balancing and database scaling (3 hours).", Type = "certificate", Done = true },
                    new() { Text = "Take the System Design Foundations practice exam.", Type = "exam", Done = false },
                    new() { Text = "Draw the architecture of one of your team's services.", Type = "practice", Done = false }
                }
            },
            new()
            {
                Title = "Deepen and practise",
                Tasks = new()
                {
                    new() { Text = "Complete the System Design Foundations course and book the certificate exam.", Type = "certificate", Done = false },
                    new() { Text = "Take the REST API Design exam and aim for 80+.", Type = "exam", Done = false },
                    new() { Text = "Mentor a junior developer in one code review.", Type = "skill", Done = false }
                }
            },
            new()
            {
                Title = "Prepare for the next role",
                Tasks = new()
                {
                    new() { Text = "Upload your System Design Foundations certificate for HR review.", Type = "certificate", Done = false },
                    new() { Text = "Take the AI Fundamentals exam again to keep your score above 85.", Type = "exam", Done = false },
                    new() { Text = "Present a short technical design to your team and ask for feedback.", Type = "skill", Done = false }
                }
            }
        }
    };

    private static void AddPromotion(AppDbContext context, Employee employee, Position from, Position to, Employee admin, int daysAgo) =>
        context.Promotions.Add(new Promotion
        {
            EmployeeId = employee.Id,
            FromPositionId = from.Id,
            ToPositionId = to.Id,
            PromotedById = admin.Id,
            PromotedAt = DateTime.UtcNow.AddDays(-daysAgo),
            Note = "Completed all required certificates."
        });

    private static void AddNotification(AppDbContext context, Employee employee, string messageKey, string icon,
        string url, int daysAgo, bool isRead, params string[] args) =>
        context.Notifications.Add(new Notification
        {
            EmployeeId = employee.Id,
            MessageKey = messageKey,
            Arguments = args.Length == 0 ? null : JsonSerializer.Serialize(args),
            Icon = icon,
            Url = url,
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow.AddDays(-daysAgo).AddMinutes(-daysAgo * 7)
        });

    // ---------------- Helpers ----------------
    private static CareerPath NewPath(string name, string description, CareerPath? parent = null) =>
        new() { Name = name, Description = description, ParentCareerPath = parent };

    private static Position NewPosition(string title, int level, CareerPath path, bool isFork = false) =>
        new() { Title = title, Level = level, CareerPath = path, IsFork = isFork };

    private static Certificate NewCertificate(string title, string category, string description) =>
        new() { Title = title, Category = category, Description = description };

    private static Exam NewExam(string title, string category, string description,
        int durationMinutes, int questionCount, params Position[] positions) =>
        new()
        {
            Title = title,
            Category = category,
            Description = description,
            DurationMinutes = durationMinutes,
            QuestionCount = questionCount,
            ExamPositions = positions.Select(p => new ExamPosition { Position = p }).ToList()
        };

    private static void AddRoadmap(AppDbContext context, Position position, params Certificate[] certificates)
    {
        for (var i = 0; i < certificates.Length; i++)
        {
            context.PositionCertificates.Add(new PositionCertificate
            {
                Position = position,
                Certificate = certificates[i],
                SortOrder = i + 1
            });
        }
    }

    private static void AddUpload(AppDbContext context, Employee employee, Certificate certificate,
        VerificationStatus status, int daysAgo, Employee admin, string? rejectionReason = null)
    {
        var submittedAt = DateTime.UtcNow.AddDays(-daysAgo);
        var reviewed = status != VerificationStatus.Pending;

        context.EmployeeCertificates.Add(new EmployeeCertificate
        {
            EmployeeId = employee.Id,
            CertificateId = certificate.Id,
            FilePath = AppConstants.SampleCertificateFileName,
            OriginalFileName = certificate.Title + ".pdf",
            ContentType = "application/pdf",
            IssuedOn = submittedAt.Date.AddDays(-3),
            Status = status,
            RejectionReason = rejectionReason,
            SubmittedAt = submittedAt,
            ReviewedAt = reviewed ? submittedAt.AddDays(1) : null,
            ReviewedById = reviewed ? admin.Id : null
        });
    }

    private static void AddSkillAssessment(AppDbContext context, Employee employee,
        int teamwork, int problemSolving, int communication, int leadership, int daysAgo)
    {
        context.SkillAssessments.Add(new SkillAssessment
        {
            EmployeeId = employee.Id,
            TeamworkScore = teamwork,
            ProblemSolvingScore = problemSolving,
            CommunicationScore = communication,
            LeadershipScore = leadership,
            OverallScore = (int)Math.Round((teamwork + problemSolving + communication + leadership) / 4.0),
            Summary = "Demo result: solid soft skills with room to grow in leadership.",
            CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
        });
    }

    private static void AddExamAttempts(AppDbContext context, Employee employee, Exam exam,
        params (int Score, int DaysAgo)[] attempts)
    {
        for (var i = 0; i < attempts.Length; i++)
        {
            var startedAt = DateTime.UtcNow.AddDays(-attempts[i].DaysAgo);
            var total = exam.QuestionCount;

            context.ExamAttempts.Add(new ExamAttempt
            {
                ExamId = exam.Id,
                EmployeeId = employee.Id,
                AttemptNumber = i + 1,
                Status = ExamAttemptStatus.Completed,
                StartedAt = startedAt,
                ExpiresAt = startedAt.AddMinutes(exam.DurationMinutes),
                CompletedAt = startedAt.AddMinutes(exam.DurationMinutes - 5),
                Score = attempts[i].Score,
                TotalQuestions = total,
                CorrectAnswers = (int)Math.Round(attempts[i].Score / 100.0 * total)
            });
        }
    }

    private static Task<Employee> CreateEmployeeAsync(UserManager<Employee> userManager,
        string name, string email, string employeeNumber, Department department, Position position) =>
        CreateUserAsync(userManager, name, email, SampleEmployeePassword, employeeNumber,
            AppRoles.Employee, department.Id, position.Id);

    private static async Task<Employee> CreateUserAsync(
        UserManager<Employee> userManager,
        string name, string email, string password, string employeeNumber,
        string role, int? departmentId, int? positionId)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            return existing;
        }

        var user = new Employee
        {
            Name = name,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            EmployeeNumber = employeeNumber,
            DepartmentId = departmentId,
            CurrentPositionId = positionId
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user {email}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    // Tiny one-page PDF ("Certificate of Completion") used for demo uploads
    private const string SampleCertificatePdfBase64 =
        "JVBERi0xLjQKMSAwIG9iago8PC9UeXBlL0NhdGFsb2cvUGFnZXMgMiAwIFI+PgplbmRvYmoKMiAwIG9iago8PC9UeXBlL1BhZ2VzL0tpZHNbMyAwIFJdL0NvdW50IDE+PgplbmRvYmoKMyAwIG9iago8PC9UeXBlL1BhZ2UvUGFyZW50IDIgMCBSL01lZGlhQm94WzAgMCA3OTIgMzk2XS9Db250ZW50cyA0IDAgUi9SZXNvdXJjZXM8PC9Gb250PDwvRjEgNSAwIFI+Pj4+Pj4KZW5kb2JqCjQgMCBvYmoKPDwvTGVuZ3RoIDE3MD4+CnN0cmVhbQpCVCAvRjEgMzAgVGYgMTcwIDIzMCBUZCAoQ2VydGlmaWNhdGUgb2YgQ29tcGxldGlvbikgVGogRVQKQlQgL0YxIDE0IFRmIDIyNSAxOTAgVGQgKElURyBMZWFybmluZyBQbGF0Zm9ybSkgVGogRVQKQlQgL0YxIDExIFRmIDIzMCAxNTAgVGQgKFNhbXBsZSBmaWxlIGZvciBkZW1vIGRhdGEpIFRqIEVUCmVuZHN0cmVhbQplbmRvYmoKNSAwIG9iago8PC9UeXBlL0ZvbnQvU3VidHlwZS9UeXBlMS9CYXNlRm9udC9IZWx2ZXRpY2EtQm9sZD4+CmVuZG9iagp4cmVmCjAgNgowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMDkgMDAwMDAgbiAKMDAwMDAwMDA1NCAwMDAwMCBuIAowMDAwMDAwMTA1IDAwMDAwIG4gCjAwMDAwMDAyMTcgMDAwMDAgbiAKMDAwMDAwMDQzNSAwMDAwMCBuIAp0cmFpbGVyCjw8L1NpemUgNi9Sb290IDEgMCBSPj4Kc3RhcnR4cmVmCjUwMwolJUVPRgo=";
}
