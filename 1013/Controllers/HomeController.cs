using _1013.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System.Web.WebPages;



namespace _1013.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly tryContext _tryContext;

        public HomeController(ILogger<HomeController> logger, tryContext tryContext)
        {
            _logger = logger;
            _tryContext = tryContext;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            var model = _tryContext.member.ToList();
            return View(model);
        }

        // ��ܽs�譶��
        public IActionResult Edit(string account)
        {

            var memberToEdit = _tryContext.member.Find(account);
            if (memberToEdit == null)
            {
                return NotFound();
            }
            return View(memberToEdit);
        }

        // �B�z�s���޿�
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(member model)
        {

            if (ModelState.IsValid)
            {
                var existingMember = _tryContext.member.Find(model.account);
                if (existingMember == null)
                {
                    return NotFound();
                }

                // ��s�즳�|�����ݩ�
                existingMember.name = model.name;
                existingMember.phone = model.phone;
                existingMember.mail = model.mail;

                // �p�G�K�X���ܧ�A���s�p��[�Q�᪺�K�X
                if (model.password != existingMember.password)
                {
                    var hashedPassword = HashPassword(model.password, existingMember.salt);
                    existingMember.password = hashedPassword;
                }

                _tryContext.SaveChanges();

                return RedirectToAction("Privacy");
            }
            return View(model);
        }



        // �B�z�R���޿�
        public IActionResult Delete(string account)
        {

            var memberToDelete = _tryContext.member.Find(account);
            if (memberToDelete != null)
            {
                _tryContext.member.Remove(memberToDelete);
                _tryContext.SaveChanges();
            }
            return RedirectToAction("Privacy");
        }


        // ��ܵ��U����
        public IActionResult Register()
        {

            return View();
        }

        // �B�z���U�޿�
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(member model)
        {
            if (ModelState.IsValid)
            {
                // 檢查帳號是否已存在
                if (_tryContext.member.Any(m => m.account == model.account))
                {
                    ModelState.AddModelError("account", "該帳號無法註冊，因為已經存在。");
                    return View(model);
                }


                // �ͦ��Q��
                var salt = GenerateSalt();
                // �K�X�[�Q�B�z
                var hashedPassword = HashPassword(model.password, salt);

                // �s�W�|�����
                var newMember = new member
                {
                    account = model.account,
                    password = hashedPassword,
                    salt = salt,
                    name = model.name,
                    phone = model.phone,
                    mail = model.mail
                };

                _tryContext.member.Add(newMember);
                _tryContext.SaveChanges();

                // ���U���\�᭫�w�V�ܭ���
                return RedirectToAction("SignIn");
            }

            // �p�G�ҫ����ҥ��ѡA�^����U����
            return View(model);

        }

        [HttpGet]
        public JsonResult IsAccountAvailable(string account)
        {
            bool isAvailable = !_tryContext.member.Any(m => m.account == account);
            return Json(isAvailable);
        }

        private string GenerateSalt()
        {
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var saltedPasswordBytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hashBytes = sha256.ComputeHash(saltedPasswordBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        //更新10.29
        public IActionResult SignIn()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Loging", "Home"); // 重定向到首頁
            }
            // 生成隨機數字驗證碼
            var random = new Random();
            var verificationCode = random.Next(1000, 9999).ToString();
            HttpContext.Session.SetString("VerificationCode", verificationCode);

            // 將驗證碼傳遞到 View
            ViewBag.VerificationCode = verificationCode;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(string username, string password, string inputVerificationCode)
        {
            // 從 Session 中獲取生成的驗證碼
            var storedVerificationCode = HttpContext.Session.GetString("VerificationCode");

            if (storedVerificationCode != inputVerificationCode)
            {
                // 驗證碼錯誤，重新生成新的驗證碼
                var newVerificationCode = GenerateVerificationCode();
                HttpContext.Session.SetString("VerificationCode", newVerificationCode);
                ViewBag.VerificationCode = newVerificationCode;
                ViewBag.ErrorMessage = "驗證碼錯誤，請重新輸入。";
                return View();
            }

            var user = await _tryContext.member
                             .FirstOrDefaultAsync(u => u.account == username);

            if (user != null)
            {
                // 將密碼進行雜湊並與資料庫中進行比對
                var hashedPassword = HashPassword(password, user.salt);

                if (hashedPassword == user.password) // 驗證密碼是否正確
                {
                    // 建立使用者的 claims 資料
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.account),
                new Claim(ClaimTypes.Name, user.name)
            };

                    // 如果 RecurringExpenseId 和 VehicleNumber 可用，則可附加相關信息
                    if (user.RecurringExpenseId.HasValue)
                    {
                        claims.Add(new Claim("RecurringExpenseId", user.RecurringExpenseId.Value.ToString()));
                    }
                    if (!string.IsNullOrEmpty(user.VehicleNumber))
                    {
                        claims.Add(new Claim("VehicleNumber", user.VehicleNumber));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,  // 設置持久性 cookie
                    };

                    // 使用 Cookie 進行登入
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                  new ClaimsPrincipal(claimsIdentity),
                                                  authProperties);

                    return RedirectToAction("Loging", "Home");
                }
                else
                {
                    // 密碼錯誤處理
                    _logger.LogWarning("登入失敗：密碼不正確");
                }
            }
            else
            {
                // 帳號不存在處理
                _logger.LogWarning("登入失敗：使用者帳號不存在");
            }

            // 驗證碼正確但登入失敗，重新生成新的驗證碼
            var newCode = GenerateVerificationCode();
            HttpContext.Session.SetString("VerificationCode", newCode);
            ViewBag.VerificationCode = newCode;
            ViewBag.ErrorMessage = "登入失敗，請檢查帳號和密碼。";
            return View();
        }

        private string GenerateVerificationCode()
        {
            // 生成隨機驗證碼的邏輯
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }


        [Authorize]
        public IActionResult Loging()
        {
            SetLeftPaneData();
            // 取得當前登入的帳號名稱
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier); // 獲取使用者帳號
            var member = _tryContext.member.FirstOrDefault(m => m.account == account);
            ViewBag.UserName = member?.name ?? "使用者"; // 如果名稱不存在，預設顯示"使用者"
            return View();
        }


        private void SetLeftPaneData(DateTime? startDate = null, DateTime? endDate = null, int? budgetLimit = null)
        {
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 設置預設日期範圍為當月
            startDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            endDate ??= startDate.Value.AddMonths(1).AddDays(-1);

            // 查找當月的預算
            if (budgetLimit == null)
            {
                var currentBudget = _tryContext.Budget
                    .Where(b => b.account == account && b.startdate <= endDate && b.enddate >= startDate)
                    .FirstOrDefault();
                budgetLimit = currentBudget != null ? currentBudget.amount : 0;
            }

            int totalIncome = _tryContext.income
                .Where(i => i.account == account && i.date >= startDate && i.date <= endDate)
                .Sum(i => i.amount);

            int totalExpense = _tryContext.expenses
                .Where(e => e.account == account && e.date >= startDate && e.date <= endDate)
                .Sum(e => e.amount);

            int remainingBudget = budgetLimit.Value - totalExpense;
            if (remainingBudget < 0) remainingBudget = 0;

            float incomePercentage = budgetLimit > 0 ? (float)totalIncome / budgetLimit.Value * 100 : 0;
            float expensePercentage = budgetLimit > 0 ? (float)totalExpense / budgetLimit.Value * 100 : 0;
            float remainingBudgetPercentage = budgetLimit > 0 ? (float)remainingBudget / budgetLimit.Value * 100 : 0;

            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.BudgetLimit = budgetLimit;
            ViewBag.IncomePercentage = incomePercentage;
            ViewBag.ExpensePercentage = expensePercentage;
            ViewBag.RemainingBudgetPercentage = remainingBudgetPercentage;
        }

        [Authorize]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear(); // 清除会话数据
            HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            HttpContext.Response.Headers["Pragma"] = "no-cache";
            HttpContext.Response.Headers["Expires"] = "0";
            return RedirectToAction("Index", "Home");
        }


        // �ӤH��ƭק�
        [Authorize]
        public IActionResult MyEdit()
        {
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier); // ���]�Τ�W�O�ߤ@��
            var member = _tryContext.member.FirstOrDefault(m => m.account == account);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult MyEdit(member member)
        {
            if (ModelState.IsValid)
            {
                var existingMember = _tryContext.member.FirstOrDefault(m => m.account == member.account);
                if (existingMember == null)
                {
                    return NotFound();
                }

                existingMember.name = member.name;
                existingMember.phone = member.phone;
                existingMember.mail = member.mail;

                if (member.password != existingMember.password)
                {
                    var hashedPassword = HashPassword(member.password, existingMember.salt);
                    existingMember.password = hashedPassword;
                }

                _tryContext.SaveChanges();
                return RedirectToAction("Loging", "Home"); // �ק令�\�᭫�w�V�쭺��
            }
            return View(member);
        }

        [Authorize]
        public IActionResult EditInvoiceCarrier()
        {

            var account = User.FindFirstValue(ClaimTypes.NameIdentifier); // 獲取使用者帳號
            var member = _tryContext.member.FirstOrDefault(m => m.account == account);

            if (member == null)
            {
                return NotFound();
            }

            // 檢查載具資訊是否為空
            if (string.IsNullOrEmpty(member.VehicleNumber))
            {
                TempData["Message"] = "尚未綁定載具資訊";
                return RedirectToAction("BindInvoiceCarrier", "Home"); // 導向到首頁或其他頁面
            }

            return View(member);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult EditInvoiceCarrier(member member)
        {
            if (ModelState.IsValid)
            {
                var existingMember = _tryContext.member.FirstOrDefault(m => m.account == member.account);
                if (existingMember == null)
                {
                    return NotFound();
                }

                existingMember.VehicleNumber = member.VehicleNumber;

                _tryContext.SaveChanges();
                return RedirectToAction("Loging", "Home"); // �ק令�\�᭫�w�V�쭺��
            }
            return View(member);
        }

        [Authorize]
        public IActionResult AddIncome(DateTime date, int amount, string category, string note)
        {
            SetLeftPaneData();
            if (date < new DateTime(1753, 1, 1) || date > new DateTime(9999, 12, 31))
            {
                ModelState.AddModelError("date", "��������b 1753 �~ 1 �� 1 ��M 9999 �~ 12 �� 31 �餧��");
                return View(); // �^���歶���A���ܿ��~
            }

            // �����e�ϥΪ̪��b��
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (account == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Account being added: {Account}", account);

            // �إߦ��J�O��
            var incomeRecord = new income
            {
                account = account, // �ϥαb���@�� account
                date = date,
                amount = amount,
                category = category,
                note = note
            };

            // �x�s�ܸ�Ʈw
            _tryContext.income.Add(incomeRecord);
            _tryContext.SaveChanges();

            // ���榨�\�᭫�w�V�ܰO�b�����Ψ�L����
            return RedirectToAction("Sucess");
        }

        [Authorize]
        public IActionResult Sucess()
        {
            SetLeftPaneData();
            return View();
        }

        //��ܳѾl�w����B
        [Authorize]
        public IActionResult Create()
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            DateTime startDate = new DateTime(2024, 10, 1); // �]�w�}�l���
            DateTime endDate = new DateTime(2024, 10, 31); // �]�w������

            // ����ӱb�����w��
            var budget = _tryContext.Budget
                .FirstOrDefault(b => b.account == account &&
                                     b.startdate <= endDate &&
                                     b.enddate >= startDate);

            int totalExpenses = 0;

            if (budget != null)
            {
                // ����ӱb���b���w����d�򤺪���X�`�B
                totalExpenses = _tryContext.expenses
                    .Where(e => e.account == account &&
                                e.date >= startDate &&
                                e.date <= endDate)
                    .Sum(e => e.amount);
            }

            // �p��w��l�B
            int remainingBudget = budget?.amount ?? 0 - totalExpenses;

            // �N�w��l�B�ǻ������
            ViewBag.RemainingBudget = remainingBudget;

            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult Create(expenses newExpense)
        {
            SetLeftPaneData();
            if (ModelState.IsValid)
            {
                _tryContext.expenses.Add(newExpense);
                _tryContext.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(newExpense);
        }


        // �C�X�ϥΪ̪����J�O�b�M��
        [Authorize]
        public IActionResult IncomeList(DateTime? startDate, DateTime? endDate)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 設定預設查詢範圍為當月
            startDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // 當月初
            endDate ??= startDate.Value.AddMonths(1).AddDays(-1); // 當月結束

            // 查詢符合條件的收入清單
            var query = _tryContext.income
                        .Where(i => i.account == account);

            // 處理區間查詢
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(i => i.date >= startDate.Value && i.date <= endDate.Value);
            }
            else if (startDate.HasValue) // 單月查詢或開始日期查詢
            {
                query = query.Where(i => i.date >= startDate.Value && i.date < startDate.Value.AddMonths(1));
            }

            // 依時間前後排序
            var incomeList = query.OrderBy(i => i.date).ToList();

            // 計算金額總和
            ViewBag.TotalAmount = incomeList.Sum(i => i.amount);

            return View(incomeList);
        }


        // ��ܽs�覬�J����
        [Authorize]
        public IActionResult EditIncome(int id)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var incomeRecord = _tryContext.income.Find(id);
            if (incomeRecord == null || incomeRecord.account != account)
            {
                return NotFound();
            }
            return View(incomeRecord); // ��^�s�譶��
        }

        // �B�z�s�覬�J�޿�
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult EditIncome(income model)
        {
            SetLeftPaneData();
            if (ModelState.IsValid)
            {
                var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingRecord = _tryContext.income.Find(model.id);
                if (existingRecord == null || existingRecord.account != account)
                {
                    return NotFound();
                }

                // ��s���J�O�����ݩ�
                existingRecord.date = model.date;
                existingRecord.amount = model.amount;
                existingRecord.category = model.category;
                existingRecord.note = model.note;

                _tryContext.SaveChanges(); // �x�s�ܧ�
                return RedirectToAction("IncomeList"); // ���w�V�^���J�M��
            }
            return View(model); // �p�G���ҥ��ѡA��^�s�譶��
        }

        // �B�z�R�����J�޿�
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteIncome(int id)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var incomeRecord = _tryContext.income.Find(id);
            if (incomeRecord != null && incomeRecord.account == account)
            {
                _tryContext.income.Remove(incomeRecord);
                _tryContext.SaveChanges(); // �R�����J�O��
            }
            return RedirectToAction("IncomeList"); // ���w�V�^���J�M��
        }

        //�W�[����϶�<10/22�[�J�b�������[�`>
        [Authorize]
        public IActionResult ichoose(DateTime? startDate, DateTime? endDate)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // �d�ߵn�J�Τ᪺���J�ƾ�
            var income = _tryContext.income
                .Where(i => i.account == account) // �Ȭd�߷�e�n�J�Τ᪺���
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                income = income.Where(e => e.date >= startDate && e.date <= endDate);
            }

            var totalIAmount = income.Sum(e => e.amount);
            ViewBag.TotalIAmount = totalIAmount;

            return View("IncomeList", income.ToList());
        }

        //��X�O�b
        [Authorize]
        public IActionResult AddExpenses(DateTime date, int amount, string category, string note)
        {
            SetLeftPaneData();
            if (date < new DateTime(1753, 1, 1) || date > new DateTime(9999, 12, 31))
            {
                ModelState.AddModelError("date", "��������b 1753 �~ 1 �� 1 ��M 9999 �~ 12 �� 31 �餧��");
                return View(); // �^���歶���A���ܿ��~
            }

            // �����e�ϥΪ̪��b��
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (account == null)
            {
                return Unauthorized();
            }

            _logger.LogInformation("Account being added: {Account}", account);

            // �إߦ��J�O��
            var expensesRecord = new expenses
            {
                account = account, // �ϥαb���@�� account
                date = date,
                amount = amount,
                category = category,
                note = note,

            };

            // 獲取當月的支出總額
            var startOfMonth = new DateTime(date.Year, date.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var totalMonthlyExpenses = _tryContext.expenses
                .Where(e => e.account == account && e.date >= startOfMonth && e.date <= endOfMonth)
                .Sum(e => e.amount) + amount;  // 加上新提交的支出

            // 獲取使用者的預算設定
            var monthlyBudget = GetMonthlyBudgetForAccount(account);

            // 檢查是否超出預算
            if (monthlyBudget > 0 && totalMonthlyExpenses > monthlyBudget)
            {
                TempData["BudgetWarning"] = "本月已超出預算";
            }



            // �x�s�ܸ�Ʈw
            _tryContext.expenses.Add(expensesRecord);
            _tryContext.SaveChanges();

            // ���榨�\�᭫�w�V�ܰO�b�����Ψ�L����
            return RedirectToAction("Sucess1");
        }



        private int GetMonthlyBudgetForAccount(string account)
        {
            var today = DateTime.Today;

            // 從 Budget 表中找到當前帳號且在當前月份的預算範圍
            var budget = _tryContext.Budget
                .FirstOrDefault(b => b.account == account
                                     && b.startdate <= today
                                     && b.enddate >= today);

            // 如果找到預算記錄，則返回其金額，否則返回 0 或預設預算
            return budget?.amount ?? 0;
        }

        [Authorize]
        public IActionResult Sucess1()
        {
            SetLeftPaneData();
            return View();
        }

        [Authorize]
        public IActionResult AddRecurringExpense()
        {
            SetLeftPaneData();
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult AddRecurringExpense(RecurringExpense recurringExpenses, string Category, int Amount)
        {
            SetLeftPaneData();
            // 取得當前登入的使用者 ID
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (account != null)
            {
                // 查找當前登入的使用者
                var user = _tryContext.member.FirstOrDefault(u => u.account == account);

                if (user != null)
                {
                    recurringExpenses.account = account;
                    //將 recurringExpense 物件添加到 RecurringExpense 資料表
                    _tryContext.RecurringExpense.Add(recurringExpenses);
                    _tryContext.SaveChanges(); // 儲存此時的 RecurringExpense 以取得其 ID

                    //// 將 recurringExpense 的 Id 綁定到 Member (使用者) 資料表
                    user.RecurringExpenseId = recurringExpenses.Id;

                    // 計算重複次數，若 RepeatCount 為 0，則假設為 30 次
                    int repeatCount = recurringExpenses.RepeatCount == 0 ? 30 : recurringExpenses.RepeatCount;

                    for (int i = 0; i < repeatCount; i++)
                    {
                        DateTime expenseDate;
                        if (recurringExpenses.FrequencyType == "Weekly")
                        {
                            // 計算最近的指定「星期幾」
                            expenseDate = GetNextWeekday(recurringExpenses.StartDate, recurringExpenses.DayOfWeek, i);
                        }

                        else if (recurringExpenses.FrequencyType == "Monthly")
                        {
                            // 計算每月的指定幾號
                            expenseDate = GetNextMonthDate(recurringExpenses.StartDate, recurringExpenses.DayOfMonth, i);
                        }
                        else
                        {
                            expenseDate = recurringExpenses.StartDate; // 如果未選擇類型，使用起始日期
                        }
                        // 創建新的支出物件
                        var expense = new expenses
                        {
                            amount = Convert.ToInt32(Amount), // 確保金額為整數
                            category = Category,
                            date = expenseDate,
                            note = "固定支出", // 附加註解
                            account = account // 綁定支出到當前登入的使用者
                        };

                        // 將支出物件添加到支出清單中
                        _tryContext.expenses.Add(expense);
                    }

                    // 儲存變更到資料庫
                    _tryContext.SaveChanges();

                    return RedirectToAction("ExpensesList"); // 導向支出列表頁面
                }

                return View("Error"); // 若找不到使用者，返回錯誤頁面            
            }

            return View("Error"); // 若未登入，返回錯誤頁面
        }

        private DateTime GetNextWeekday(DateTime StartDate, string dayOfWeek, int weeksToAdd)
        {

            DayOfWeek targetDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayOfWeek);
            DateTime result = StartDate.AddDays((int)targetDay - (int)StartDate.DayOfWeek);

            if (result < StartDate)
            {
                result = result.AddDays(7); // 如果結果日期早於起始日期，則加一週
            }

            return result.AddDays(weeksToAdd * 7); // 加上相應的週數
        }
        // 計算每月的指定日期
        private DateTime GetNextMonthDate(DateTime StartDate, int dayOfMonth, int monthsToAdd)
        {
            // 增加月份
            DateTime targetDate = StartDate.AddMonths(monthsToAdd);

            // 計算當月的天數
            int daysInMonth = DateTime.DaysInMonth(targetDate.Year, targetDate.Month);

            // 確保指定的日期不超過當月的天數
            int targetDay = Math.Min(dayOfMonth, daysInMonth);

            // 返回最終的日期
            return new DateTime(targetDate.Year, targetDate.Month, targetDay);
        }



        //新增清單分頁功能

        [Authorize]
        [HttpGet]
        public IActionResult ExpensesList(DateTime? startDate, DateTime? endDate, int page = 1)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 設定預設查詢範圍為當月
            startDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // 當月初
            endDate ??= startDate.Value.AddMonths(1).AddDays(-1); // 當月結束

            // 查詢當前使用者的支出清單
            var query = _tryContext.expenses
                        .Where(e => e.account == account);

            // 如果指定了日期範圍，則應用篩選條件
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(e => e.date >= startDate.Value && e.date <= endDate.Value);
            }
            else if (startDate.HasValue) // 單月查詢
            {
                query = query.Where(e => e.date >= startDate.Value && e.date < startDate.Value.AddMonths(1));
            }

            // 設定每頁顯示的資料數量
            int pageSize = 10;

            // 總記錄數
            int totalRecords = query.Count();

            // 計算範圍內的金額總和
            ViewBag.TotalAmount = query.Sum(e => e.amount);

            // 分頁處理
            var expenseList = query
                .OrderBy(e => e.date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 計算總頁數
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return View(expenseList);
        }


        // ��ܽs���X����
        [Authorize]
        public IActionResult EditExpenses(int id)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var expensesRecord = _tryContext.expenses.Find(id);
            if (expensesRecord == null || expensesRecord.account != account)
            {
                return NotFound();
            }
            return View(expensesRecord); // ��^�s�譶��
        }

        // �B�z�s���X�޿�
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult EditExpenses(expenses model)
        {
            SetLeftPaneData();
            if (ModelState.IsValid)
            {
                var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingRecord = _tryContext.expenses.Find(model.id);
                if (existingRecord == null || existingRecord.account != account)
                {
                    return NotFound();
                }

                // ��s���J�O�����ݩ�
                existingRecord.date = model.date;
                existingRecord.amount = model.amount;
                existingRecord.category = model.category;
                existingRecord.note = model.note;

                _tryContext.SaveChanges(); // �x�s�ܧ�
                return RedirectToAction("ExpensesList"); // ���w�V�^��X�M��
            }
            return View(model); // �p�G���ҥ��ѡA��^�s�譶��
        }

        // �B�z�R����X�޿�
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteExpenses(int id)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var expensesRecord = _tryContext.expenses.Find(id);
            if (expensesRecord != null && expensesRecord.account == account)
            {
                _tryContext.expenses.Remove(expensesRecord);
                _tryContext.SaveChanges(); // �R����X�O��
            }
            return RedirectToAction("ExpensesList"); // ���w�V�^��X�M��
        }

        //�W�[��X����[�`�϶�<10/22�[�J�b�������[�`>
        [Authorize]
        public IActionResult echoose(DateTime? startDate, DateTime? endDate)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // �d�ߵn�J�Τ᪺���J�ƾ�
            var expenses = _tryContext.expenses
                .Where(i => i.account == account) // �Ȭd�߷�e�n�J�Τ᪺���
                .AsQueryable();

            if (startDate.HasValue && endDate.HasValue)
            {
                expenses = expenses.Where(e => e.date >= startDate && e.date <= endDate);
            }

            var totalAmount = expenses.Sum(e => e.amount);
            ViewBag.TotalAmount = totalAmount;

            return View("ExpensesList", expenses.ToList());
        }


        //發票記帳頁面
        [Authorize]
        public IActionResult InvoiceAccounting()
        {
            SetLeftPaneData();
            return View();
        }

        [Authorize]
        public IActionResult BindInvoiceCarrier()
        {
            SetLeftPaneData();
            return View();
        }

        [Authorize]
        [HttpPost]//綁定載具      

        public IActionResult BindInvoiceCarrier(string vehicleNumber)
        {
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (account != null)
            {
                // 更新資料庫中的使用者資料
                var user = _tryContext.member.Find(account);
                if (user != null)
                {
                    user.VehicleNumber = vehicleNumber;
                    _tryContext.SaveChanges();
                    return RedirectToAction("BindSuccess", new { VehicleNumber = vehicleNumber });
                }
            }
            return View("Error");
        }


        [Authorize]
        public IActionResult BindSuccess(string vehicleNumber)
        {
            SetLeftPaneData();
            ViewBag.VehicleNumber = vehicleNumber;
            return View();
        }


        //更新日期分頁
        [Authorize]
        [HttpPost]
        public IActionResult GetInvoices(int? page)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (account != null)
            {
                var user = _tryContext.member.Find(account);

                if (user != null && !string.IsNullOrEmpty(user.VehicleNumber))
                {
                    var vehicleNumber = user.VehicleNumber;

                    // 獲取所有發票資料，按日期排序
                    var invoices = _tryContext.Invoice
                        .Where(i => i.VehicleNumber == vehicleNumber)
                        .OrderBy(i => i.InvoiceDate)
                        .Select(i => new Invoice
                        {
                            InvoiceNumber = i.InvoiceNumber,
                            InvoiceDate = i.InvoiceDate,
                            Category = i.Category,
                            Amount = i.Amount
                        }).ToList();

                    if (!invoices.Any())
                    {
                        return NotFound("未找到與該載具號碼相關的發票資料。");
                    }

                    // 設置每頁顯示的月份範圍（每頁顯示兩個月）
                    int itemsPerPage = 2;

                    // 當前日期
                    var currentDate = DateTime.Now;

                    // 如果頁碼為空，根據當前月份和年份自動設定頁碼
                    if (!page.HasValue)
                    {
                        page = ((currentDate.Year - currentDate.Year) * 6) + ((currentDate.Month + 1) / itemsPerPage);
                    }

                    // 計算年份和月份區間
                    var currentYear = currentDate.Year + (page.Value - 1) / 6;
                    var startMonth = ((page.Value - 1) % 6) * itemsPerPage + 1;
                    var endMonth = startMonth + 1;

                    // 篩選符合年份和月份範圍的發票
                    var pagedInvoices = invoices
                        .Where(i => i.InvoiceDate.Year == currentYear && i.InvoiceDate.Month >= startMonth && i.InvoiceDate.Month <= endMonth)
                        .ToList();

                    // 獲取用戶之前選取的發票資料
                    var selectedInvoices = _tryContext.UserSelectedInvoice
                        .Where(u => u.account == account)
                        .Select(u => u.InvoiceNumber)
                        .ToList();

                    // 將所需的變數傳遞到視圖中
                    ViewBag.SelectedInvoices = selectedInvoices;
                    ViewBag.CurrentPage = page.Value;
                    ViewBag.StartMonth = startMonth;
                    ViewBag.EndMonth = endMonth;
                    ViewBag.CurrentYear = currentYear;

                    return View("InvoiceList", pagedInvoices);
                }

                return RedirectToAction("BindInvoiceCarrier");
            }

            return View("Error", "無法辨識使用者，請先登入。");
        }

        [Authorize]
        [HttpPost]
        public IActionResult AddToExpenseList(string[] selectedInvoices)
        {
            SetLeftPaneData();
            if (selectedInvoices == null || selectedInvoices.Length == 0)
            {
                return BadRequest("請選擇至少一張發票進行記帳。");
            }

            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 儲存選中的發票到數據庫
            foreach (var invoiceData in selectedInvoices)
            {
                var selectedInvoiceParts = invoiceData.Split(',');

                // 確保資料格式正確
                if (selectedInvoiceParts.Length == 4)
                {
                    var invoiceNumber = selectedInvoiceParts[0];

                    // 儲存選中的發票編號
                    var selectedInvoice = new UserSelectedInvoice
                    {
                        account = account,
                        InvoiceNumber = invoiceNumber
                    };
                    _tryContext.UserSelectedInvoice.Add(selectedInvoice);
                }
            }

            // 逐個處理選中的發票，並新增至支出清單
            foreach (var invoiceData in selectedInvoices)
            {
                var invoiceParts = invoiceData.Split(',');

                // 確保資料格式正確
                if (invoiceParts.Length == 4)
                {
                    var invoiceNumber = invoiceParts[0];
                    var invoiceDate = DateTime.Parse(invoiceParts[1]);
                    var category = invoiceParts[2];
                    var amount = decimal.Parse(invoiceParts[3]);

                    var expense = new expenses
                    {
                        account = account,
                        amount = Convert.ToInt32(amount),
                        date = invoiceDate,
                        category = category,
                        note = "雲端發票"
                    };
                    _tryContext.expenses.Add(expense);
                }
            }

            // 單次保存所有變更到資料庫
            _tryContext.SaveChanges();

            return RedirectToAction("ExpensesList");  // 重定向至支出清單頁面
        }



        //�]�w�w��
        [Authorize]
        public IActionResult AddBudget(int amount, DateTime startdate, DateTime enddate)
        {
            SetLeftPaneData();

            if (amount < 0)
            {
                ModelState.AddModelError("amount", "預算金額不能為負數");
                return View();
            }


            if (startdate < new DateTime(1753, 1, 1) || startdate > new DateTime(9999, 12, 31))
            {
                ModelState.AddModelError("date", "提醒：預算日期不可重疊");
                return View();
            }

            if (enddate < new DateTime(1753, 1, 1) || enddate > new DateTime(9999, 12, 31))
            {
                ModelState.AddModelError("date", "提醒：預算日期不可重疊");
                return View();
            }

            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (account == null)
            {
                return Unauthorized();
            }

            // 檢查是否有重疊區間的預算
            bool overlapExists = _tryContext.Budget
                .Any(b => b.account == account &&
                          ((startdate <= b.enddate && enddate >= b.startdate)));

            if (overlapExists)
            {
                ModelState.AddModelError("overlap", $"{startdate:MM/dd} ~ {enddate:MM/dd} 與現有的預算區間重疊");
                return View();
            }

            var BudgetSet = new Budget
            {
                account = account,
                amount = amount,
                startdate = startdate,
                enddate = enddate
            };

            _tryContext.Budget.Add(BudgetSet);
            _tryContext.SaveChanges();

            // 傳遞新的預算資料給 SetLeftPaneData
            SetLeftPaneData(startdate, enddate, amount);

            return RedirectToAction("Sucess2");
        }

        [Authorize]
        public IActionResult Sucess2()
        {
            SetLeftPaneData();
            return View();
        }

        
        [Authorize]
        public IActionResult BudgetList(DateTime? startDate, DateTime? endDate)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 設定預設查詢範圍為當月
            startDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // 當月初
            endDate ??= startDate.Value.AddMonths(1).AddDays(-1); // 當月結束

            // 查詢當前使用者的支出清單
            var query = _tryContext.Budget
                                .Where(i => i.account == account); // �ھڱb���z���X�O��


            // 如果指定了日期範圍，則應用篩選條件
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(i => i.startdate >= startDate.Value && i.enddate <= endDate.Value);
            }
            else if (startDate.HasValue) // 單月查詢
            {
                query = query.Where(i => i.startdate >= startDate.Value && i.startdate < startDate.Value.AddMonths(1));
            }

            // 按時間前後排序
            var budgetList = query.OrderBy(i => i.startdate).ToList();

            return View(budgetList); // ��^��X�M�����

        }



        // ��ܽs��w�⭶��
        [Authorize]
        public IActionResult EditBudget(int id)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var budgetRecord = _tryContext.Budget.Find(id);
            if (budgetRecord == null || budgetRecord.account != account)
            {
                return NotFound();
            }
            return View(budgetRecord); // ��^�s�譶��
        }

        // �B�z�s��w���޿�
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult EditBudget(Budget model)
        {
            SetLeftPaneData();
            if (ModelState.IsValid)
            {
                var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingRecord = _tryContext.Budget.Find(model.id);
                if (existingRecord == null || existingRecord.account != account)
                {
                    return NotFound();
                }

                // ��s�w��O�����ݩ�
                existingRecord.amount = model.amount;
                existingRecord.startdate = model.startdate;
                existingRecord.enddate = model.enddate;

                _tryContext.SaveChanges(); // �x�s�ܧ�
                return RedirectToAction("BudgetList"); // ���w�V�^��X�M��
            }
            return View(model); // �p�G���ҥ��ѡA��^�s�譶��
        }

        // �B�z�R���w���޿�
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBudget(int id)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var budgetRecord = _tryContext.Budget.Find(id);
            if (budgetRecord != null && budgetRecord.account == account)
            {
                _tryContext.Budget.Remove(budgetRecord);
                _tryContext.SaveChanges(); // �R����X�O��
            }
            return RedirectToAction("BudgetList"); // ���w�V�^��X�M��
        }


        [Authorize]
        public IActionResult CategoryReport(DateTime? startDate, DateTime? endDate)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(account))
            {
                return RedirectToAction("SignIn", "Home"); // �p�G���n�J�A���w�V��n�J����
            }


            // 設定日期區間的篩選條件
            startDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);  // 若無提供起始日期，預設為當月月初
            endDate ??= startDate.Value.AddMonths(1).AddDays(-1); // 若無提供結束日期，預設為當月月底


            // �z��X�ݩ�ӱb������X���
            var expenses = _tryContext.expenses
                .Where(e => e.account == account && e.date >= startDate && e.date <= endDate)
                .GroupBy(e => e.category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalAmount = g.Sum(e => e.amount)
                })
                .ToList();

            TempData["BudgetWarning"] = $"查詢的區間為 {startDate.Value.ToString("MM/dd")} ~ {endDate.Value.ToString("MM/dd")}";

            return View(expenses);
        }


        [Authorize]
        public IActionResult DailyExpenseReport(DateTime? startDate, DateTime? endDate)
        {
            SetLeftPaneData();
            var account = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(account))
            {
                return RedirectToAction("SignIn", "Home");
            }

            startDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // 當月初
            endDate ??= DateTime.Now; // 當前日期

            var dailyExpenses = _tryContext.expenses
                .Where(e => e.account == account && e.date >= startDate && e.date <= endDate)
                .GroupBy(e => e.date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalAmount = g.Sum(e => e.amount)
                })
                .OrderBy(x => x.Date)
                .ToList();


            TempData["BudgetWarning"] = $"查詢的區間為 {startDate.Value.ToString("MM/dd")} ~ {endDate.Value.ToString("MM/dd")}";

            return View(dailyExpenses);
        }


        /// <summary>
        /// ///////////////////
        /// </summary>
        /// <returns></returns>


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}
