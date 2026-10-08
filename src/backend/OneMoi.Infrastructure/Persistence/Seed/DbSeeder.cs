using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneMoi.Application.Common;
using OneMoi.Domain.Entities;
using OneMoi.Domain.Enums;

namespace OneMoi.Infrastructure.Persistence.Seed;

/// <summary>
/// Demo data so every login has something to show. Runs only when the database is empty.
///
///  LOGIN                         HOW                                   SEES
///  Super admin                   admin@onemoi.in / Admin@123           all vendors, users, OTP + notification logs
///  JD Moi Tech owner             jd@jdmoi.in / Vendor@123              3 functions today, 9 operators, entries
///  JD Moi Tech manager           9000000002 / Vendor@123
///  Meenakshi Moi owner           meena@meenakshimoi.in / Vendor@123    "Arun weds Divya" etc.
///  Sri Murugan Moi (pending)     murugan@srimurugan.in / Vendor@123    waiting for approval
///  Operators                     JDMOI + OP-101…OP-109, PIN 1234       their own counter
///                                MEENAMOI + OP-101…OP-103, PIN 1234
///  Individual "Ram"              mobile 9876543210 + OTP               ₹6,000 across JD Moi + Meenakshi Moi
///  Function host "Manikandan"    mobile 9876500001 + OTP               his Keda Vettu function report
/// </summary>
public class DbSeeder(AppDbContext db, IPasswordHasher hasher, ILogger<DbSeeder> log)
{
    private readonly Random _rnd = new(42);

    public async Task SeedAsync()
    {
        await SeedMastersAsync();
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync()) return;
        log.LogInformation("Seeding demo data…");

        // ── Plans ──
        var starter = new Plan { Name = "Starter", PriceMonthly = 499, PricePerFunction = 0, MaxOperators = 3, MaxFunctionsPerMonth = 5 };
        var pro = new Plan { Name = "Pro", PriceMonthly = 1499, PricePerFunction = 0, MaxOperators = 20, MaxFunctionsPerMonth = 60 };
        var perFn = new Plan { Name = "Per function", PriceMonthly = 0, PricePerFunction = 299, MaxOperators = 6, MaxFunctionsPerMonth = 999 };
        db.Plans.AddRange(starter, pro, perFn);

        // ── Super admin ──
        var vendorPwd = hasher.Hash("Vendor@123");
        db.Users.Add(new AppUser { FullName = "OneMoi Admin", Email = "admin@onemoi.in", Mobile = "9000000000", PasswordHash = hasher.Hash("Admin@123"),
            UserType = UserType.SuperAdmin, IsEmailVerified = true, IsMobileVerified = true });

        // ── Vendors (tenants) ──
        var jd = new Tenant
        {
            Code = "JDMOI", Name = "JD Moi Tech", NameTa = "ஜேடி மொய் டெக்", Tagline = "Digital Moi services since 2015", OwnerName = "Jayadev K",
            Email = "jd@jdmoi.in", Mobile = "9000000001", AddressLine = "12, Avinashi Road", City = "Coimbatore", District = "Coimbatore", Pincode = "641018",
            Gstin = "33ABCDE1234F1Z5", PrimaryColor = "#8E1B3A", ReceiptHeader = "JD Moi Tech · 90000 00001", ReceiptFooter = "நன்றி! Thank you for your blessings",
            Status = TenantStatus.Active, Plan = pro
        };
        var meena = new Tenant
        {
            Code = "MEENAMOI", Name = "Meenakshi Moi", NameTa = "மீனாட்சி மொய்", Tagline = "மொய் கணக்கு எளிதாக", OwnerName = "Meenakshi S",
            Email = "meena@meenakshimoi.in", Mobile = "9000000011", AddressLine = "45, West Masi Street", City = "Madurai", District = "Madurai", Pincode = "625001",
            PrimaryColor = "#1F8A5B", ReceiptHeader = "Meenakshi Moi · Madurai", ReceiptFooter = "வாழ்த்துக்கள்", Status = TenantStatus.Active, Plan = pro
        };
        var murugan = new Tenant
        {
            Code = "MURUGAN", Name = "Sri Murugan Moi Services", NameTa = "ஸ்ரீ முருகன் மொய் சேவை", OwnerName = "Murugan P",
            Email = "murugan@srimurugan.in", Mobile = "9000000021", City = "Palani", Status = TenantStatus.Pending, Plan = starter,
            TrialEndsAt = DateTime.UtcNow.AddDays(14)
        };
        db.Tenants.AddRange(jd, meena, murugan);

        AddStaff(jd, "Jayadev K", "9000000001", "jd@jdmoi.in", TenantRole.Owner, vendorPwd);
        AddStaff(jd, "Prakash R", "9000000002", "manager@jdmoi.in", TenantRole.Manager, vendorPwd);
        AddStaff(jd, "Revathi A", "9000000003", "accounts@jdmoi.in", TenantRole.Accountant, vendorPwd);
        AddStaff(meena, "Meenakshi S", "9000000011", "meena@meenakshimoi.in", TenantRole.Owner, vendorPwd);
        AddStaff(murugan, "Murugan P", "9000000021", "murugan@srimurugan.in", TenantRole.Owner, vendorPwd);
        await db.SaveChangesAsync();

        // ── Operators (PIN 1234 for all demo operators) ──
        var pin = hasher.Hash("1234");
        string[] jdNames = { "Arun Raj", "Bala Murugan", "Chitra Selvi", "Dinesh N", "Esther S", "Faizal Z", "Ganesh N", "Hari R", "Indhu N", "Jothi L" };
        string[] jdNamesTa = { "அருண் ராஜ்", "பாலா முருகன்", "சித்ரா செல்வி", "தினேஷ்", "எஸ்தர்", "ஃபைசல்", "கணேஷ்", "ஹரி", "இந்து", "ஜோதி" };
        var jdOps = jdNames.Select((n, i) => new Operator { TenantId = jd.Id, Code = $"OP-{101 + i}", Name = n, NameTa = jdNamesTa[i], Mobile = $"95000001{i:00}", PinHash = pin }).ToList();
        var meOps = new[] { "Karthi V", "Latha M", "Mohan S" }.Select((n, i) => new Operator { TenantId = meena.Id, Code = $"OP-{101 + i}", Name = n, Mobile = $"95000002{i:00}", PinHash = pin }).ToList();
        db.Operators.AddRange(jdOps); db.Operators.AddRange(meOps);

        // ── Individuals (have logins) ──
        var ramPerson = new Person { Mobile = "9876543210", Initial = "N.", Name = "Ram", NameTa = "ராம்", SpouseInitial = "R.", SpouseName = "Geetha", SpouseNameTa = "கீதா",
            Work = "Teacher", City = "Pollachi", CityTa = "பொள்ளாச்சி", IsVerified = true };
        var maniPerson = new Person { Mobile = "9876500001", Initial = "K.", Name = "Manikandan", NameTa = "மணிகண்டன்", Work = "Farmer", City = "Kinathukadavu", CityTa = "கிணத்துக்கடவு", IsVerified = true };
        db.Persons.AddRange(ramPerson, maniPerson);
        db.Users.Add(new AppUser { FullName = "N. Ram", FullNameTa = "ராம்", Mobile = "9876543210", Email = "ram@example.com", UserType = UserType.Individual, IsMobileVerified = true, Person = ramPerson });
        db.Users.Add(new AppUser { FullName = "K. Manikandan", FullNameTa = "மணிகண்டன்", Mobile = "9876500001", UserType = UserType.Individual, IsMobileVerified = true, Person = maniPerson });
        await db.SaveChangesAsync();

        // ── Masters needed below ──
        var types = await db.FunctionTypes.IgnoreQueryFilters().ToDictionaryAsync(t => t.Name, t => t.Id);
        var cats = await db.MoiCategories.IgnoreQueryFilters().ToDictionaryAsync(c => c.Name, c => c);
        var gifts = await db.GiftItemTypes.IgnoreQueryFilters().ToDictionaryAsync(g => g.Name, g => g.Id);
        var expCats = await db.ExpenseCategories.IgnoreQueryFilters().ToDictionaryAsync(g => g.Name, g => g.Id);

        var today = DateTime.Today;

        // ── JD Moi Tech: 3 functions TODAY, 3 counters each, 3 operators each (the 3×3 flow) ──
        var keda = NewFunction(jd, "MKV7Q2", types["Keda Vettu"], "Manikandan Keda Vettu", "மணிகண்டன் கெடா வெட்டு", "K. Manikandan", "மணிகண்டன்", "9876500001",
            "Kinathukadavu Thottam", "கிணத்துக்கடவு தோட்டம்", "Kinathukadavu", today, 9, 16, 400, FunctionStatus.Live);
        var surya = NewFunction(jd, "SPW4M8", types["Marriage"], "Surya weds Priya", "சூர்யா - பிரியா திருமணம்", "R. Surya", "சூர்யா", "9876500002",
            "KPR Mahal", "கே.பி.ஆர் மஹால்", "Coimbatore", today, 6, 14, 800, FunctionStatus.Live);
        var kavya = NewFunction(jd, "KEP3L9", types["Ear Piercing"], "Kavya Ear Piercing", "காவ்யா காதணி விழா", "Ravi & Meena", "ரவி & மீனா", "9876500003",
            "Palani Temple Hall", "பழனி கோயில் மண்டபம்", "Palani", today, 10, 13, 250, FunctionStatus.Scheduled);
        var ramVilla = NewFunction(jd, "RIV8K5", types["Housewarming"], "Ram illa villa", "ராம் இல்லா வில்லா", "N. Ram", "ராம்", "9876543210",
            "Ram illa villa, Gandhi Nagar", "ராம் இல்லா வில்லா, காந்தி நகர்", "Pollachi", today.AddDays(5), 7, 13, 300, FunctionStatus.Scheduled);
        var ramesh = NewFunction(jd, "RKD6F1", types["Keda Vettu"], "Ramesh Keda Vettu", "ரமேஷ் கெடா வெட்டு", "Ramesh", "ரமேஷ்", "9876500004",
            "Udumalpet", "உடுமலைப்பேட்டை", "Udumalpet", today.AddDays(-9), 9, 16, 300, FunctionStatus.Closed);

        // ── Meenakshi Moi ──
        var arun = NewFunction(meena, "ADW2H7", types["Marriage"], "Arun weds Divya", "அருண் - திவ்யா திருமணம்", "Arun", "அருண்", "9876500005",
            "Sri Vari Mahal", "ஸ்ரீ வாரி மஹால்", "Pollachi", today.AddDays(-42), 6, 13, 600, FunctionStatus.Closed);
        var senthilHw = NewFunction(meena, "SHW9P4", types["Housewarming"], "Senthil Housewarming", "செந்தில் புதுமனை புகுவிழா", "Senthil V", "செந்தில்", "9876500006",
            "Anna Nagar", "அண்ணா நகர்", "Madurai", today, 7, 12, 200, FunctionStatus.Live);
        db.Functions.AddRange(keda, surya, kavya, ramVilla, ramesh, arun, senthilHw);
        await db.SaveChangesAsync();

        // Operator assignments (today's 3 JD functions × 3 counters)
        var jdToday = new[] { surya, keda, kavya };
        for (var f = 0; f < 3; f++)
        {
            var counters = jdToday[f].Counters.OrderBy(c => c.Number).ToList();
            for (var c = 0; c < 3; c++) Assign(jdToday[f], counters[c], jdOps[f * 3 + c]);
        }
        Assign(senthilHw, senthilHw.Counters.OrderBy(c => c.Number).First(), meOps[0]);
        await db.SaveChangesAsync();

        // ── Moi entries ──
        var kedaC = keda.Counters.OrderBy(c => c.Number).ToList();
        // Ram's ₹1,000 at JD Moi (500 × 2) — part of the ₹6,000 example
        AddEntry(keda, kedaC[0], jdOps[3], 1, ramPerson, "N.", "Ram", "ராம்", "R.", "Geetha", "கீதா", "Teacher", "Pollachi", "பொள்ளாச்சி",
            cats["General"], 1000, PaymentMode.Cash, today.AddHours(9.6), new[] { (500, 2) });
        // Thaimaman moi — highlighted, with gold
        var thaimaman = AddEntry(keda, kedaC[1], jdOps[4], 2, null, "K.", "Murugesan", "முருகேசன்", "M.", "Valli", "வள்ளி", "Business", "Kinathukadavu", "கிணத்துக்கடவு",
            cats["Thaimaman Moi"], 25001, PaymentMode.Cash, today.AddHours(9.8), new[] { (500, 50), (1, 1) }, mobile: "9786500101");
        thaimaman.Gifts.Add(new MoiEntryGift { GiftItemTypeId = gifts["Gold"], Description = "Gold ring 1 sovereign", DescriptionTa = "தங்க மோதிரம் 1 பவுன்", Quantity = 1, EstimatedValue = 58000 });
        thaimaman.Gifts.Add(new MoiEntryGift { GiftItemTypeId = gifts["Dress"], Description = "Silk veshti & saree", DescriptionTa = "பட்டு வேட்டி, சேலை", Quantity = 1 });
        // ₹1,00,000 counted by machine: 500 × 150 + 100 × 250
        AddEntry(keda, kedaC[2], jdOps[5], 3, null, "S.", "Palanisamy", "பழனிசாமி", "P.", "Kamala", "கமலா", "Contractor", "Pollachi", "பொள்ளாச்சி",
            cats["Relative"], 100000, PaymentMode.Cash, today.AddHours(10.1), new[] { (500, 150), (100, 250) }, mobile: "9786500102");
        // Same name "Ram" in same city but a different initial — shows why initial matters
        AddEntry(keda, kedaC[0], jdOps[3], 4, null, "K.", "Ram", "ராம்", "K.", "Saroja", "சரோஜா", "Driver", "Pollachi", "பொள்ளாச்சி",
            cats["Friend"], 501, PaymentMode.Upi, today.AddHours(10.3), null, mobile: "9786500103");
        var guestNames = new (string i, string n, string ta, string city, string cityTa)[]
        {
            ("L.", "Lakshmi", "லட்சுமி", "Tiruppur", "திருப்பூர்"), ("V.", "Senthil", "செந்தில்", "Udumalpet", "உடுமலைப்பேட்டை"),
            ("R.", "Meena", "மீனா", "Coimbatore", "கோயம்புத்தூர்"), ("P.", "Karthik", "கார்த்திக்", "Erode", "ஈரோடு"),
            ("M.", "Saranya", "சரண்யா", "Palani", "பழனி"), ("K.", "Vignesh", "விக்னேஷ்", "Salem", "சேலம்"),
            ("J.", "Anitha", "அனிதா", "Karur", "கரூர்"), ("B.", "Kavitha", "கவிதா", "Coimbatore", "கோயம்புத்தூர்")
        };
        int[] amounts = { 501, 1001, 1001, 2001, 501, 5001, 1001, 3001 };
        var serial = 5;
        foreach (var (g, k) in guestNames.Select((g, k) => (g, k)))
            AddEntry(keda, kedaC[k % 3], jdOps[3 + k % 3], serial++, null, g.i, g.n, g.ta, null, null, null, null, g.city, g.cityTa, cats["General"],
                amounts[k], k % 3 == 2 ? PaymentMode.Upi : PaymentMode.Cash, today.AddHours(10.5 + k * 0.2), null, mobile: $"97865002{k:00}");

        var suryaC = surya.Counters.OrderBy(c => c.Number).ToList();
        serial = 1;
        foreach (var (g, k) in guestNames.Select((g, k) => (g, k)))
            AddEntry(surya, suryaC[k % 3], jdOps[k % 3], serial++, null, g.i, g.n, g.ta, null, null, null, null, g.city, g.cityTa,
                k == 0 ? cats["Seer Varisai"] : cats["General"], amounts[(k + 3) % 8] * 2, k % 4 == 0 ? PaymentMode.Upi : PaymentMode.Cash, today.AddHours(7 + k * 0.3), null, mobile: $"97865002{k:00}");

        // Meenakshi Moi: Ram's ₹5,000 by UPI at his friend's marriage — the other part of ₹6,000
        var arunC = arun.Counters.OrderBy(c => c.Number).ToList();
        AddEntry(arun, arunC[0], meOps[1], 1, ramPerson, "N.", "Ram", "ராம்", "R.", "Geetha", "கீதா", "Teacher", "Pollachi", "பொள்ளாச்சி",
            cats["Friend"], 5000, PaymentMode.Upi, today.AddDays(-42).AddHours(8), null, payRef: "UPI627814523301");
        AddEntry(arun, arunC[0], meOps[1], 2, null, "A.", "Selvam", "செல்வம்", null, null, null, "Farmer", "Pollachi", "பொள்ளாச்சி",
            cats["Thaimaman Moi"], 10001, PaymentMode.Cash, today.AddDays(-42).AddHours(8.5), new[] { (500, 20), (1, 1) }, mobile: "9786500301");
        AddEntry(senthilHw, senthilHw.Counters.First(), meOps[0], 1, null, "R.", "Muthu", "முத்து", null, null, null, null, "Madurai", "மதுரை",
            cats["General"], 1001, PaymentMode.Cash, today.AddHours(8), null, mobile: "9786500302");

        // ── Function expenses: money taken during the function ──
        db.FunctionExpenses.Add(new FunctionExpense { TenantId = jd.Id, Function = keda, ExpenseCategoryId = expCats["Food"], TakenByName = "Saravanan", TakenByNameTa = "சரவணன்",
            Relation = "Host's brother", TakenByMobile = "9876500011", Purpose = "Extra biryani for 50 guests", Amount = 5000, EntryAt = today.AddHours(11).ToUniversalTime(), OperatorId = jdOps[3].Id });
        db.FunctionExpenses.Add(new FunctionExpense { TenantId = jd.Id, Function = keda, ExpenseCategoryId = expCats["Transport"], TakenByName = "Kumar", TakenByNameTa = "குமார்",
            Relation = "Host's cousin", Purpose = "Van rent", Amount = 1500, EntryAt = today.AddHours(11.5).ToUniversalTime() });

        await db.SaveChangesAsync();

        db.AuditLogs.Add(new AuditLog { Action = "SEED", Details = "Demo data created", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        log.LogInformation("Demo data ready.");
    }

    // ─────────────────────────── masters (always ensured) ───────────────────────────

    private async Task SeedMastersAsync()
    {
        if (!await db.FunctionTypes.IgnoreQueryFilters().AnyAsync())
        {
            var ft = new (string, string)[]
            {
                ("Marriage", "திருமணம்"), ("Engagement", "நிச்சயதார்த்தம்"), ("Keda Vettu", "கெடா வெட்டு"), ("Housewarming", "புதுமனை புகுவிழா"),
                ("Ear Piercing", "காதணி விழா"), ("Puberty Function", "மஞ்சள் நீராட்டு விழா"), ("Baby Shower", "வளைகாப்பு"),
                ("Birthday", "பிறந்தநாள் விழா"), ("60th Wedding (Shashtiapthapoorthi)", "மணிவிழா"), ("Temple Festival", "கோயில் திருவிழா"), ("Other", "மற்றவை")
            };
            db.FunctionTypes.AddRange(ft.Select((x, i) => new FunctionType { Name = x.Item1, NameTa = x.Item2, SortOrder = i + 1 }));
        }
        if (!await db.MoiCategories.IgnoreQueryFilters().AnyAsync())
        {
            db.MoiCategories.AddRange(
                new MoiCategory { Name = "General", NameTa = "பொது மொய்", SortOrder = 1 },
                new MoiCategory { Name = "Thaimaman Moi", NameTa = "தாய்மாமன் மொய்", SortOrder = 2, IsHighlighted = true, Color = "#F2A516" },
                new MoiCategory { Name = "Seer Varisai", NameTa = "சீர் வரிசை", SortOrder = 3, IsHighlighted = true, Color = "#8E1B3A" },
                new MoiCategory { Name = "Relative", NameTa = "உறவினர்", SortOrder = 4 },
                new MoiCategory { Name = "Friend", NameTa = "நண்பர்", SortOrder = 5 },
                new MoiCategory { Name = "Office / Colleague", NameTa = "அலுவலகம்", SortOrder = 6 },
                new MoiCategory { Name = "Return Moi", NameTa = "திருப்பு மொய்", SortOrder = 7, IsHighlighted = true, Color = "#2563EB" });
        }
        if (!await db.GiftItemTypes.IgnoreQueryFilters().AnyAsync())
        {
            db.GiftItemTypes.AddRange(
                new GiftItemType { Name = "Gold", NameTa = "தங்கம்", Unit = "sovereign", SortOrder = 1 },
                new GiftItemType { Name = "Silver", NameTa = "வெள்ளி", Unit = "grams", SortOrder = 2 },
                new GiftItemType { Name = "Vessels", NameTa = "பாத்திரம்", Unit = "pieces", SortOrder = 3 },
                new GiftItemType { Name = "Dress", NameTa = "ஆடை", Unit = "pieces", SortOrder = 4 },
                new GiftItemType { Name = "Household item", NameTa = "வீட்டு உபயோகப் பொருள்", Unit = "pieces", SortOrder = 5 },
                new GiftItemType { Name = "Others", NameTa = "மற்றவை", SortOrder = 6 });
        }
        if (!await db.ExpenseCategories.IgnoreQueryFilters().AnyAsync())
        {
            var ec = new (string, string)[] { ("Food", "சாப்பாடு"), ("Transport", "போக்குவரத்து"), ("Decoration", "அலங்காரம்"), ("Priest / Pooja", "அர்ச்சகர் / பூஜை"),
                ("Return gifts", "தாம்பூலம்"), ("Music / Band", "மேளம்"), ("Others", "மற்றவை") };
            db.ExpenseCategories.AddRange(ec.Select((x, i) => new ExpenseCategory { Name = x.Item1, NameTa = x.Item2, SortOrder = i + 1 }));
        }
        if (!await db.Denominations.AnyAsync())
        {
            int[] notes = { 500, 200, 100, 50, 20, 10 };
            int[] coins = { 5, 2, 1 };
            db.Denominations.AddRange(notes.Select((v, i) => new Denomination { Value = v, SortOrder = i + 1 }));
            db.Denominations.AddRange(coins.Select((v, i) => new Denomination { Value = v, IsCoin = true, SortOrder = 10 + i }));
        }
        await db.SaveChangesAsync();
    }

    // ─────────────────────────── helpers ───────────────────────────

    private void AddStaff(Tenant t, string name, string mobile, string email, TenantRole role, string pwdHash)
    {
        var u = new AppUser { FullName = name, Mobile = mobile, Email = email, PasswordHash = pwdHash, UserType = UserType.TenantUser, IsMobileVerified = true, IsEmailVerified = true };
        db.Users.Add(u);
        db.TenantMembers.Add(new TenantMember { Tenant = t, User = u, Role = role });
    }

    private static Function NewFunction(Tenant t, string code, int typeId, string name, string nameTa, string owner, string ownerTa, string ownerMobile,
        string location, string locationTa, string city, DateTime date, int startHour, int endHour, int guests, FunctionStatus status)
    {
        var f = new Function
        {
            TenantId = t.Id, Code = code, FunctionTypeId = typeId, Name = name, NameTa = nameTa, OwnerName = owner, OwnerNameTa = ownerTa, OwnerMobile = ownerMobile,
            Location = location, LocationTa = locationTa, City = city, FunctionDate = date.Date, StartTime = TimeSpan.FromHours(startHour), EndTime = TimeSpan.FromHours(endHour),
            ExpectedGuests = guests, Status = status, OtherDetails = "Lunch arranged. Return gift: thamboolam bag."
        };
        for (var i = 1; i <= 3; i++) f.Counters.Add(new Counter { TenantId = t.Id, Number = i, Name = $"Counter {i}" });
        return f;
    }

    private void Assign(Function f, Counter c, Operator op) => db.OperatorAssignments.Add(new OperatorAssignment
    {
        TenantId = f.TenantId, Function = f, Counter = c, OperatorId = op.Id,
        ValidFrom = f.FunctionDate.Add(f.StartTime ?? TimeSpan.Zero).AddHours(-3),
        ValidTo = f.FunctionDate.AddDays(1).AddHours(2)      // demo: open the whole day so testing is easy
    });

    private MoiEntry AddEntry(Function f, Counter c, Operator op, int serial, Person? person, string? initial, string name, string? nameTa,
        string? spouseInitial, string? spouse, string? spouseTa, string? work, string? city, string? cityTa, MoiCategory cat, decimal amount,
        PaymentMode mode, DateTime localTime, (int note, int count)[]? notes, string? mobile = null, string? payRef = null)
    {
        var tenantCode = f.TenantId == db.Tenants.Local.First(t => t.Code == "JDMOI").Id ? "JDMOI" : "MEENAMOI";
        Person? p = person;
        if (p == null && mobile != null)
        {
            p = db.Persons.Local.FirstOrDefault(x => x.Mobile == mobile);
            if (p == null) { p = new Person { Mobile = mobile, Initial = initial, Name = name, NameTa = nameTa, SpouseInitial = spouseInitial, SpouseName = spouse, SpouseNameTa = spouseTa, Work = work, City = city, CityTa = cityTa }; db.Persons.Add(p); }
        }
        var e = new MoiEntry
        {
            TenantId = f.TenantId, Function = f, Counter = c, OperatorId = op.Id, Person = p, SerialNo = serial,
            ReceiptNo = $"{tenantCode}-{f.Code}-{serial:0000}", Mobile = person?.Mobile ?? mobile,
            Initial = initial, Name = name, NameTa = nameTa, SpouseInitial = spouseInitial, SpouseName = spouse, SpouseNameTa = spouseTa,
            Work = work, City = city, CityTa = cityTa, MoiCategory = cat, IsHighlighted = cat.IsHighlighted,
            Amount = amount, PaymentMode = mode, PaymentRef = payRef, EntryAt = localTime.ToUniversalTime(), CreatedAt = localTime.ToUniversalTime()
        };
        if (notes != null)
            foreach (var (note, count) in notes) e.Denominations.Add(new MoiEntryDenomination { NoteValue = note, Count = count, Total = note * count });
        db.MoiEntries.Add(e);
        return e;
    }
}
