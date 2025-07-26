using System.Diagnostics;
using InternProject.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Data;
using Microsoft.AspNetCore.Http;

namespace InternProject.Controllers
{
    public class HomeController : BaseController
    {

        public static string connectionStr = "Server=OMER\\SQLEXPRESS;Database=InternProject;Trusted_Connection=True;";

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult Login()
        {
            return View();
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult EditApplication()
        {
            return View();
        }

        public IActionResult Map()
        {
            return View();
        }


        [HttpPost]
        public IActionResult FormSave([FromBody] ApplicationForm model)
        {

            DateTime Durum_Tarihi = Convert.ToDateTime(model.stateDate);
            string formattedDate = Durum_Tarihi.ToString("yyyy-MM-dd");
            DateTime Basvuru_tarihi = Convert.ToDateTime(model.applicationDate);
            string formattedDate2 = Basvuru_tarihi.ToString("yyyy-MM-dd");
            System.Data.SqlClient.SqlConnection sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr);

            System.Data.SqlClient.SqlCommand query = new System.Data.SqlClient.SqlCommand();
            query.CommandType = System.Data.CommandType.Text;
            query.CommandText = "INSERT INTO Basvurular (ProjeAdi,BasvurulanBirim,BasvuruYapilanProje,BasvuruYapilanTur,KatilimciTuru,BasvuruDonemi,BasvuruTarihi,BasvuruDurumu,DurumTarihi,HibeTutari) VALUES ('" + model.projectName + "','" + model.applicantUnit + "','" + model.appliedProject + "','" + model.appliedType + "','" + model.participantType + "','" + model.applicationPeriod + "','" + formattedDate2 + "','" + model.applicationState + "','" + formattedDate + "','" + model.grantAmount + "')";
            query.Connection = sqlConnection1;

            sqlConnection1.Open();
            query.ExecuteNonQuery();
            sqlConnection1.Close();

            LogAction(HttpContext.Session.GetString("Username"),
            $"Created application: {model.projectName}",
            "Home/FormSave", HttpContext.Connection.RemoteIpAddress?.ToString());


            return Json(new { success = true, message = "Veri kayıt oldu!" });

        }


        [HttpGet]
        public JsonResult GetApplicationsPaged(
        int page = 1,
        int pageSize = 10,
        string? projectName = null,
        int? applicantUnit = null,
        int? appliedProject = null,
        int? appliedType = null,
        int? participantType = null,
        int? applicationPeriod = null,
        DateTime? applicationDate = null,
        DateTime? stateDate = null,
        string? grantAmount = null
)
        {
            List<ApplicationForm> basvuruList = new List<ApplicationForm>();
            int totalCount = 0;

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection1.Open();

                string where = "WHERE ISNULL(b.[Delete], 0) = 0 ";
                // burada [Delete] = 0 kontrolü de eklendi

                if (!string.IsNullOrWhiteSpace(projectName))
                    where += " AND b.ProjeAdi LIKE @projectName ";
                if (applicantUnit.HasValue && applicantUnit.Value > 0)
                    where += " AND b.BasvurulanBirim = @applicantUnit ";
                if (appliedProject.HasValue && appliedProject.Value > 0)
                    where += " AND b.BasvuruYapilanProje = @appliedProject ";
                if (appliedType.HasValue && appliedType.Value > 0)
                    where += " AND b.BasvuruYapilanTur = @appliedType ";
                if (participantType.HasValue && participantType.Value > 0)
                    where += " AND b.KatilimciTuru = @participantType ";
                if (applicationPeriod.HasValue && applicationPeriod.Value > 0)
                    where += " AND b.BasvuruDonemi = @applicationPeriod ";
                if (applicationDate.HasValue)
                    where += " AND CAST(b.BasvuruTarihi AS DATE) = @applicationDate ";
                if (stateDate.HasValue)
                    where += " AND CAST(b.DurumTarihi AS DATE) = @stateDate ";
                if (!string.IsNullOrWhiteSpace(grantAmount))
                    where += " AND b.HibeTutari LIKE @grantAmount ";

                // toplam kayıt sayısı
                var countSql = $@"
            SELECT COUNT(*) 
            FROM Basvurular b 
            {where}";

                var countCmd = new System.Data.SqlClient.SqlCommand(countSql, sqlConnection1);

                if (!string.IsNullOrWhiteSpace(projectName))
                    countCmd.Parameters.AddWithValue("@projectName", $"%{projectName}%");
                if (applicantUnit.HasValue && applicantUnit.Value > 0)
                    countCmd.Parameters.AddWithValue("@applicantUnit", applicantUnit.Value);
                if (appliedProject.HasValue && appliedProject.Value > 0)
                    countCmd.Parameters.AddWithValue("@appliedProject", appliedProject.Value);
                if (appliedType.HasValue && appliedType.Value > 0)
                    countCmd.Parameters.AddWithValue("@appliedType", appliedType.Value);
                if (participantType.HasValue && participantType.Value > 0)
                    countCmd.Parameters.AddWithValue("@participantType", participantType.Value);
                if (applicationPeriod.HasValue && applicationPeriod.Value > 0)
                    countCmd.Parameters.AddWithValue("@applicationPeriod", applicationPeriod.Value);
                if (applicationDate.HasValue)
                    countCmd.Parameters.AddWithValue("@applicationDate", applicationDate.Value.Date);
                if (stateDate.HasValue)
                    countCmd.Parameters.AddWithValue("@stateDate", stateDate.Value.Date);
                if (!string.IsNullOrWhiteSpace(grantAmount))
                    countCmd.Parameters.AddWithValue("@grantAmount", $"%{grantAmount}%");

                totalCount = (int)countCmd.ExecuteScalar();

                int offset = (page - 1) * pageSize;

                var sql = $@"
            SELECT * FROM (
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY b.Id ASC) AS RowNum,
                    b.Id,
                    b.ProjeAdi,
                    u.Subtype AS BasvurulanBirimAd,
                    p.Subtype AS BasvuruYapilanProjeAd,
                    t.Subtype AS BasvuruYapilanTurAd,
                    pt.Subtype AS KatilimciTuruAd,
                    pd.Subtype AS BasvuruDonemiAd,
                    s.Subtype AS BasvuruDurumuAd,
                    b.BasvuruTarihi,
                    b.DurumTarihi,
                    b.HibeTutari
                FROM Basvurular b
                LEFT JOIN DropdownOptions u ON b.BasvurulanBirim = u.Id AND u.Type = 'Başvuran Birim'
                LEFT JOIN DropdownOptions p ON b.BasvuruYapilanProje = p.Id AND p.Type = 'Başvuru Yapılan Proje'
                LEFT JOIN DropdownOptions t ON b.BasvuruYapilanTur = t.Id AND t.Type = 'Başvuru Yapılan Tür'
                LEFT JOIN DropdownOptions pt ON b.KatilimciTuru = pt.Id AND pt.Type = 'Katılımcı Türü'
                LEFT JOIN DropdownOptions pd ON b.BasvuruDonemi = pd.Id AND pd.Type = 'Başvuru Dönemi'
                LEFT JOIN DropdownOptions s ON b.BasvuruDurumu = s.Id AND s.Type = 'Başvuru Durumu'
                {where}
            ) AS Temp
            WHERE RowNum BETWEEN @StartRow AND @EndRow";

                var query = new System.Data.SqlClient.SqlCommand(sql, sqlConnection1);

                if (!string.IsNullOrWhiteSpace(projectName))
                    query.Parameters.AddWithValue("@projectName", $"%{projectName}%");
                if (applicantUnit.HasValue && applicantUnit.Value > 0)
                    query.Parameters.AddWithValue("@applicantUnit", applicantUnit.Value);
                if (appliedProject.HasValue && appliedProject.Value > 0)
                    query.Parameters.AddWithValue("@appliedProject", appliedProject.Value);
                if (appliedType.HasValue && appliedType.Value > 0)
                    query.Parameters.AddWithValue("@appliedType", appliedType.Value);
                if (participantType.HasValue && participantType.Value > 0)
                    query.Parameters.AddWithValue("@participantType", participantType.Value);
                if (applicationPeriod.HasValue && applicationPeriod.Value > 0)
                    query.Parameters.AddWithValue("@applicationPeriod", applicationPeriod.Value);
                if (applicationDate.HasValue)
                    query.Parameters.AddWithValue("@applicationDate", applicationDate.Value.Date);
                if (stateDate.HasValue)
                    query.Parameters.AddWithValue("@stateDate", stateDate.Value.Date);
                if (!string.IsNullOrWhiteSpace(grantAmount))
                    query.Parameters.AddWithValue("@grantAmount", $"%{grantAmount}%");

                query.Parameters.AddWithValue("@StartRow", offset + 1);
                query.Parameters.AddWithValue("@EndRow", offset + pageSize);

                using (var reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        basvuruList.Add(new ApplicationForm
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            projectName = reader["ProjeAdi"].ToString(),
                            applicantUnitName = reader["BasvurulanBirimAd"].ToString(),
                            appliedProjectName = reader["BasvuruYapilanProjeAd"].ToString(),
                            appliedTypeName = reader["BasvuruYapilanTurAd"].ToString(),
                            participantTypeName = reader["KatilimciTuruAd"].ToString(),
                            applicationPeriodName = reader["BasvuruDonemiAd"].ToString(),
                            applicationStateName = reader["BasvuruDurumuAd"].ToString(),
                            applicationDate = reader["BasvuruTarihi"] != DBNull.Value ? Convert.ToDateTime(reader["BasvuruTarihi"]) : (DateTime?)null,
                            stateDate = reader["DurumTarihi"] != DBNull.Value ? Convert.ToDateTime(reader["DurumTarihi"]) : (DateTime?)null,
                            grantAmount = reader["HibeTutari"]?.ToString()
                        });
                    }
                }
            }

            return Json(new
            {
                data = basvuruList,
                totalCount = totalCount
            });
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Listele()
        {
            return View();
        }

        public IActionResult TipEkle()
        {
            return View();
        }


        public IActionResult ExportToExcel(
    string? projectName = null,
    int? applicantUnit = null,
    int? appliedProject = null,
    int? appliedType = null,
    int? participantType = null,
    int? applicationPeriod = null,
    DateTime? applicationDate = null,
    DateTime? stateDate = null,
    string? grantAmount = null
)
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("Omer Faruk Gurun");

            List<ApplicationForm> dataList = new List<ApplicationForm>();

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection1.Open();

                string where = "WHERE 1=1 ";

                if (!string.IsNullOrWhiteSpace(projectName))
                    where += "AND b.ProjeAdi LIKE @projectName ";
                if (applicantUnit.HasValue && applicantUnit.Value > 0)
                    where += "AND b.BasvurulanBirim = @applicantUnit ";
                if (appliedProject.HasValue && appliedProject.Value > 0)
                    where += "AND b.BasvuruYapilanProje = @appliedProject ";
                if (appliedType.HasValue && appliedType.Value > 0)
                    where += "AND b.BasvuruYapilanTur = @appliedType ";
                if (participantType.HasValue && participantType.Value > 0)
                    where += "AND b.KatilimciTuru = @participantType ";
                if (applicationPeriod.HasValue && applicationPeriod.Value > 0)
                    where += "AND b.BasvuruDonemi = @applicationPeriod ";
                if (applicationDate.HasValue)
                    where += "AND CAST(b.BasvuruTarihi AS DATE) = @applicationDate ";
                if (stateDate.HasValue)
                    where += "AND CAST(b.DurumTarihi AS DATE) = @stateDate ";
                if (!string.IsNullOrWhiteSpace(grantAmount))
                    where += "AND b.HibeTutari LIKE @grantAmount ";

                var query = new System.Data.SqlClient.SqlCommand($@"
            SELECT 
                b.ProjeAdi,
                u.Subtype AS BasvurulanBirimAd,
                p.Subtype AS BasvuruYapilanProjeAd,
                t.Subtype AS BasvuruYapilanTurAd,
                pt.Subtype AS KatilimciTuruAd,
                pd.Subtype AS BasvuruDonemiAd,
                s.Subtype AS BasvuruDurumuAd,
                b.BasvuruTarihi,
                b.DurumTarihi,
                b.HibeTutari
            FROM Basvurular b
            LEFT JOIN DropdownOptions u ON b.BasvurulanBirim = u.Id AND u.Type = 'Başvuran Birim'
            LEFT JOIN DropdownOptions p ON b.BasvuruYapilanProje = p.Id AND p.Type = 'Başvuru Yapılan Proje'
            LEFT JOIN DropdownOptions t ON b.BasvuruYapilanTur = t.Id AND t.Type = 'Başvuru Yapılan Tür'
            LEFT JOIN DropdownOptions pt ON b.KatilimciTuru = pt.Id AND pt.Type = 'Katılımcı Türü'
            LEFT JOIN DropdownOptions pd ON b.BasvuruDonemi = pd.Id AND pd.Type = 'Başvuru Dönemi'
            LEFT JOIN DropdownOptions s ON b.BasvuruDurumu = s.Id AND s.Type = 'Başvuru Durumu'
            {where}
            ORDER BY b.Id", sqlConnection1);

                // parametreler
                if (!string.IsNullOrWhiteSpace(projectName))
                    query.Parameters.AddWithValue("@projectName", $"%{projectName}%");
                if (applicantUnit.HasValue && applicantUnit.Value > 0)
                    query.Parameters.AddWithValue("@applicantUnit", applicantUnit.Value);
                if (appliedProject.HasValue && appliedProject.Value > 0)
                    query.Parameters.AddWithValue("@appliedProject", appliedProject.Value);
                if (appliedType.HasValue && appliedType.Value > 0)
                    query.Parameters.AddWithValue("@appliedType", appliedType.Value);
                if (participantType.HasValue && participantType.Value > 0)
                    query.Parameters.AddWithValue("@participantType", participantType.Value);
                if (applicationPeriod.HasValue && applicationPeriod.Value > 0)
                    query.Parameters.AddWithValue("@applicationPeriod", applicationPeriod.Value);
                if (applicationDate.HasValue)
                    query.Parameters.AddWithValue("@applicationDate", applicationDate.Value.Date);
                if (stateDate.HasValue)
                    query.Parameters.AddWithValue("@stateDate", stateDate.Value.Date);
                if (!string.IsNullOrWhiteSpace(grantAmount))
                    query.Parameters.AddWithValue("@grantAmount", $"%{grantAmount}%");

                using (var reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dataList.Add(new ApplicationForm
                        {
                            projectName = reader["ProjeAdi"].ToString(),
                            applicantUnitName = reader["BasvurulanBirimAd"].ToString(),
                            appliedProjectName = reader["BasvuruYapilanProjeAd"].ToString(),
                            appliedTypeName = reader["BasvuruYapilanTurAd"].ToString(),
                            participantTypeName = reader["KatilimciTuruAd"].ToString(),
                            applicationPeriodName = reader["BasvuruDonemiAd"].ToString(),
                            applicationStateName = reader["BasvuruDurumuAd"].ToString(),
                            applicationDate = reader["BasvuruTarihi"] != DBNull.Value ? Convert.ToDateTime(reader["BasvuruTarihi"]) : (DateTime?)null,
                            stateDate = reader["DurumTarihi"] != DBNull.Value ? Convert.ToDateTime(reader["DurumTarihi"]) : (DateTime?)null,
                            grantAmount = reader["HibeTutari"]?.ToString()
                        });
                    }
                }
            }

            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Basvurular");

                worksheet.Cells[1, 1].Value = "Proje Adı";
                worksheet.Cells[1, 2].Value = "Başvuran Birim";
                worksheet.Cells[1, 3].Value = "Başvuru Yapılan Proje";
                worksheet.Cells[1, 4].Value = "Başvuru Yapılan Tür";
                worksheet.Cells[1, 5].Value = "Katılımcı Türü";
                worksheet.Cells[1, 6].Value = "Başvuru Dönemi";
                worksheet.Cells[1, 7].Value = "Başvuru Tarihi";
                worksheet.Cells[1, 8].Value = "Başvuru Durumu";
                worksheet.Cells[1, 9].Value = "Durum Tarihi";
                worksheet.Cells[1, 10].Value = "Hibe Tutarı";

                for (int i = 0; i < dataList.Count; i++)
                {
                    var item = dataList[i];
                    worksheet.Cells[i + 2, 1].Value = item.projectName;
                    worksheet.Cells[i + 2, 2].Value = item.applicantUnitName;
                    worksheet.Cells[i + 2, 3].Value = item.appliedProjectName;
                    worksheet.Cells[i + 2, 4].Value = item.appliedTypeName;
                    worksheet.Cells[i + 2, 5].Value = item.participantTypeName;
                    worksheet.Cells[i + 2, 6].Value = item.applicationPeriodName;
                    worksheet.Cells[i + 2, 7].Value = item.applicationDate?.ToString("yyyy-MM-dd") ?? "";
                    worksheet.Cells[i + 2, 8].Value = item.applicationStateName;
                    worksheet.Cells[i + 2, 9].Value = item.stateDate?.ToString("yyyy-MM-dd") ?? "";
                    worksheet.Cells[i + 2, 10].Value = item.grantAmount;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = $"Basvurular_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                return File(stream, contentType, fileName);
            }
        }

        [HttpGet]
        public JsonResult GetDropdownOptions([FromQuery] string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return Json(new List<object>());

            var options = new List<DropdownOption>();

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                var query = new System.Data.SqlClient.SqlCommand();
                query.Connection = sqlConnection1;
                query.CommandText = "SELECT Id, Subtype FROM DropdownOptions WHERE Type = @type AND [Delete] = 0";
                query.Parameters.Add("@type", SqlDbType.NVarChar).Value = type.Trim();

                sqlConnection1.Open();
                var reader = query.ExecuteReader();

                while (reader.Read())
                {
                    options.Add(new DropdownOption
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Subtype = reader["Subtype"].ToString()
                    });
                }
            }

            return Json(options.Select(o => new {
                Id = o.Id,
                Subtype = o.Subtype
            }));
        }

        

        [HttpPost]
        public JsonResult AddNewTip([FromBody] DropdownOption model)
        {
            if (string.IsNullOrWhiteSpace(model.Type) || string.IsNullOrWhiteSpace(model.Subtype))
            {
                return Json(new { success = false, message = "Geçersiz veri." });
            }

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                var cmd = new System.Data.SqlClient.SqlCommand();
                cmd.Connection = sqlConnection1;
                cmd.CommandText = "INSERT INTO DropdownOptions (Type, Subtype, [Delete]) VALUES (@type, @subtype, 0)";
                cmd.Parameters.AddWithValue("@type", model.Type.Trim());
                cmd.Parameters.AddWithValue("@subtype", model.Subtype.Trim());

                sqlConnection1.Open();
                cmd.ExecuteNonQuery();
            }
            LogAction(HttpContext.Session.GetString("Username"),
            $"Added new dropdown type: {model.Type}, value: {model.Subtype}",
            "Home/AddNewTip", HttpContext.Connection.RemoteIpAddress?.ToString());


            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetDistinctTypes()
        {
            var types = new List<string>();

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection1.Open();

                var cmd = new System.Data.SqlClient.SqlCommand("SELECT DISTINCT [Type] FROM DropdownOptions WHERE [Delete] = 0", sqlConnection1);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    types.Add(reader["Type"].ToString());
                }
            }

            return Json(types);
        }

        [HttpPost]
        public JsonResult DeleteTip([FromBody] DeleteRequest req)
        {
            if (req.id <= 0)
            {
                return Json(new { success = false, message = "Geçersiz ID." });
            }

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection1.Open();

                var cmd = new System.Data.SqlClient.SqlCommand();
                cmd.Connection = sqlConnection1;
                cmd.CommandText = "UPDATE DropdownOptions SET [Delete] = 1 WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", req.id);

                int affected = cmd.ExecuteNonQuery();

                LogAction(HttpContext.Session.GetString("Username"),
                $"Deleted dropdown option Id={req.id}",
                "Home/DeleteTip", HttpContext.Connection.RemoteIpAddress?.ToString());


                if (affected > 0)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
            }
        }



    

        [HttpGet]
        public JsonResult GetTipsByType(string type)
        {
            var list = new List<DropdownOption>();

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection1.Open();

                var cmd = new System.Data.SqlClient.SqlCommand(
                    "SELECT Id, Subtype FROM DropdownOptions WHERE Type = @type AND [Delete] = 0 ORDER BY Subtype",
                    sqlConnection1
                );
                cmd.Parameters.AddWithValue("@type", type);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new DropdownOption
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Subtype = reader["Subtype"].ToString()
                    });
                }
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetApplicationById(int id)
        {
            ApplicationForm model = new();

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection1.Open();

                var cmd = new System.Data.SqlClient.SqlCommand("SELECT * FROM Basvurular WHERE Id = @id", sqlConnection1);
                cmd.Parameters.AddWithValue("@id", id);

                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.Id = id;
                    model.projectName = reader["ProjeAdi"].ToString();
                    model.applicantUnit = Convert.ToInt32(reader["BasvurulanBirim"]);
                    model.appliedProject = Convert.ToInt32(reader["BasvuruYapilanProje"]);
                    model.appliedType = Convert.ToInt32(reader["BasvuruYapilanTur"]);
                    model.participantType = Convert.ToInt32(reader["KatilimciTuru"]);
                    model.applicationPeriod = Convert.ToInt32(reader["BasvuruDonemi"]);
                    model.applicationDate = reader["BasvuruTarihi"] != DBNull.Value ? Convert.ToDateTime(reader["BasvuruTarihi"]) : null;
                    model.stateDate = reader["DurumTarihi"] != DBNull.Value ? Convert.ToDateTime(reader["DurumTarihi"]) : null;
                    model.grantAmount = reader["HibeTutari"].ToString();
                    model.applicationState = Convert.ToInt32(reader["BasvuruDurumu"]);
                }
            }

            return PartialView("_EditApplication", model);
        }


        [HttpPost]
        public JsonResult UpdateApplication([FromBody] ApplicationForm updated)
        {
            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection1.Open();

                // Eski kaydı sil
                var cmdDelete = new System.Data.SqlClient.SqlCommand("UPDATE Basvurular SET [Delete] = 1 WHERE Id = @id", sqlConnection1);
                cmdDelete.Parameters.AddWithValue("@id", updated.Id);
                cmdDelete.ExecuteNonQuery();

                // Yeni kayıt ekle
                var cmdInsert = new System.Data.SqlClient.SqlCommand(@"
            INSERT INTO Basvurular 
            (ProjeAdi, BasvurulanBirim, BasvuruYapilanProje, BasvuruYapilanTur, KatilimciTuru, BasvuruDonemi, BasvuruTarihi, BasvuruDurumu, DurumTarihi, HibeTutari, [Delete])
            VALUES 
            (@ProjeAdi, @BasvurulanBirim, @BasvuruYapilanProje, @BasvuruYapilanTur, @KatilimciTuru, @BasvuruDonemi, @BasvuruTarihi, @BasvuruDurumu, @DurumTarihi, @HibeTutari, 0)", sqlConnection1);

                cmdInsert.Parameters.AddWithValue("@ProjeAdi", updated.projectName ?? (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@BasvurulanBirim",
                    updated.applicantUnit > 0 ? updated.applicantUnit : (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@BasvuruYapilanProje",
                    updated.appliedProject > 0 ? updated.appliedProject : (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@BasvuruYapilanTur",
                    updated.appliedType > 0 ? updated.appliedType : (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@KatilimciTuru",
                    updated.participantType > 0 ? updated.participantType : (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@BasvuruDonemi",
                    updated.applicationPeriod > 0 ? updated.applicationPeriod : (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@BasvuruTarihi",
                    updated.applicationDate ?? (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@BasvuruDurumu",
                    updated.applicationState > 0 ? updated.applicationState : (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@DurumTarihi",
                    updated.stateDate ?? (object)DBNull.Value);

                cmdInsert.Parameters.AddWithValue("@HibeTutari",
                    string.IsNullOrEmpty(updated.grantAmount) ? (object)DBNull.Value : Convert.ToDecimal(updated.grantAmount));


                cmdInsert.ExecuteNonQuery();
            }

            LogAction(HttpContext.Session.GetString("Username"),
            $"Updated application Id={updated.Id}, new projectName={updated.projectName}",
            "Home/UpdateApplication", HttpContext.Connection.RemoteIpAddress?.ToString());


            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult DeleteApplication([FromBody] DeleteRequest req)
        {
            if (req.id <= 0)
            {
                return Json(new { success = false, message = "Geçersiz ID." });
            }

            using (var sqlConnection1 = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection1.Open();

                var cmd = new System.Data.SqlClient.SqlCommand(
                    "UPDATE Basvurular SET [Delete] = 1 WHERE Id = @id", sqlConnection1);
                cmd.Parameters.AddWithValue("@id", req.id);

                int affectedRows = cmd.ExecuteNonQuery();

                if (affectedRows > 0)
                {
                    LogAction(HttpContext.Session.GetString("Username"),
                    $"Deleted application Id={req.id}",
                    "Home/DeleteApplication", HttpContext.Connection.RemoteIpAddress?.ToString());


                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı." });
                }
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(); // Register.cshtml açılır
        }

        [HttpPost]
        public IActionResult Register(string username, string password, string email)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Kullanıcı adı, şifre ve e-posta zorunlu." });
            }

            using (var sqlConnection = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection.Open();

                var cmdCheck = new System.Data.SqlClient.SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE Username=@username", sqlConnection);
                cmdCheck.Parameters.AddWithValue("@username", username);

                int exists = (int)cmdCheck.ExecuteScalar();

                if (exists > 0)
                {
                    return Json(new { success = false, message = "Bu kullanıcı adı zaten mevcut." });
                }

                var cmdInsert = new System.Data.SqlClient.SqlCommand(
                    "INSERT INTO Users (Username, PasswordHash, Email) VALUES (@username, @password, @email)", sqlConnection);

                cmdInsert.Parameters.AddWithValue("@username", username);

                string hashedPassword = ComputeSha256Hash(password);
                cmdInsert.Parameters.AddWithValue("@password", hashedPassword);

                cmdInsert.Parameters.AddWithValue("@email", email);

                cmdInsert.ExecuteNonQuery();
            }

            LogAction(username,
            $"Registered new user: {username}, email: {email}",
            "Home/Register", HttpContext.Connection.RemoteIpAddress?.ToString());


            return Json(new { success = true, message = "Kayıt başarılı." });
        }



        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Lütfen geçerli bir kullanıcı adı ve şifre giriniz." });
            }

            using (var sqlConnection = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection.Open();

                string hashedPassword = ComputeSha256Hash(password);

                string query = @"
            SELECT Role 
            FROM Users 
            WHERE Username=@username AND PasswordHash=@password";

                var cmd = new System.Data.SqlClient.SqlCommand(query, sqlConnection);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", hashedPassword);

                var result = cmd.ExecuteScalar();

                if (result != null)
                {
                    HttpContext.Session.SetString("Username", username);
                    HttpContext.Session.SetString("UserRole", result.ToString());

                    LogAction(username,
                    $"Logged in",
                    "Home/Login", HttpContext.Connection.RemoteIpAddress?.ToString());


                    return Json(new { success = true, redirect = Url.Action("Index", "Home") });
                }
                else
                {
                    return Json(new { success = false, message = "Geçersiz kullanıcı adı veya şifre." });
                }
            }
        }


        public IActionResult Logout()
        {
            LogAction(HttpContext.Session.GetString("Username"),
                $"Logged out",
                "Home/Logout", HttpContext.Connection.RemoteIpAddress?.ToString());


            HttpContext.Session.Remove("Username");
           
            return RedirectToAction("Login");
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                var builder = new System.Text.StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
        public IActionResult Authorize()
        {
            var users = new List<User>();

            using (var conn = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                conn.Open();

                var cmd = new System.Data.SqlClient.SqlCommand(
                    "SELECT Id, Username, Role FROM Users", conn);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = (int)reader["Id"],
                        Username = reader["Username"].ToString(),
                        Role = reader["Role"]?.ToString()
                    });
                }
            }

            return View(users);
        }



        [HttpPost]
        public IActionResult UpdateAuthorization(int userId, string role)
        {
            var currentUsername = HttpContext.Session.GetString("Username");

            // Önce güncellenmek istenen kullanıcının adını al
            string targetUsername = null;

            using (var conn = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                conn.Open();

                var checkCmd = new System.Data.SqlClient.SqlCommand(
                    "SELECT Username FROM Users WHERE Id=@id", conn);
                checkCmd.Parameters.AddWithValue("@id", userId);

                var result = checkCmd.ExecuteScalar();
                if (result != null)
                    targetUsername = result.ToString();
            }

            if (targetUsername == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            if (targetUsername == currentUsername)
            {
                return Json(new { success = false, message = "Kendi yetkinizi değiştiremezsiniz." });
            }

            // Yetkisini güncelle
            using (var conn = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                conn.Open();

                var cmd = new System.Data.SqlClient.SqlCommand(
                    "UPDATE Users SET Role=@role WHERE Id=@id", conn);

                cmd.Parameters.AddWithValue("@role", role);
                cmd.Parameters.AddWithValue("@id", userId);

                cmd.ExecuteNonQuery();
            }

            LogAction(HttpContext.Session.GetString("Username"),
            $"Changed role of userId={userId} to {role}",
            "Home/UpdateAuthorization", HttpContext.Connection.RemoteIpAddress?.ToString());


            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteUser(int userId)
        {
            var currentUsername = HttpContext.Session.GetString("Username");

            using (var conn = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                conn.Open();

                
                var cmdCheck = new System.Data.SqlClient.SqlCommand(
                    "SELECT Username FROM Users WHERE Id=@id", conn);
                cmdCheck.Parameters.AddWithValue("@id", userId);

                var usernameToDelete = cmdCheck.ExecuteScalar()?.ToString();

                if (usernameToDelete == currentUsername)
                {
                    return Json(new { success = false, message = "Kendi hesabınızı silemezsiniz!" });
                }

                var cmd = new System.Data.SqlClient.SqlCommand(
                    "DELETE FROM Users WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@id", userId);

                cmd.ExecuteNonQuery();
            }
            LogAction(HttpContext.Session.GetString("Username"),
            $"Deleted user userId={userId}",
            "Home/DeleteUser", HttpContext.Connection.RemoteIpAddress?.ToString());


            return Json(new { success = true });
        }

        public IActionResult UserTransactions()
        {
            var users = new List<User>();
            var currentUsername = HttpContext.Session.GetString("Username");

            using (var conn = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                conn.Open();

                var cmd = new System.Data.SqlClient.SqlCommand(
                    "SELECT Id, Username FROM Users", conn);

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = (int)reader["Id"],
                        Username = reader["Username"].ToString()
                        // CanAccessAllPages yok, çünkü gerek yok
                    });
                }
            }

            return View(users);
        }

        [HttpPost]
        public IActionResult UpdateProfile(int id, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "E-posta boş bırakılamaz." });
            }

            using (var sqlConnection = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection.Open();

                var sql = "UPDATE Users SET Email=@Email";
                if (!string.IsNullOrWhiteSpace(password))
                {
                    sql += ", PasswordHash=@Password";
                }
                sql += " WHERE Id=@Id";

                var cmdUpdate = new System.Data.SqlClient.SqlCommand(sql, sqlConnection);
                cmdUpdate.Parameters.AddWithValue("@Email", email);
                cmdUpdate.Parameters.AddWithValue("@Id", id);

                if (!string.IsNullOrWhiteSpace(password))
                {
                    string hashedPassword = ComputeSha256Hash(password);
                    cmdUpdate.Parameters.AddWithValue("@Password", hashedPassword);
                }

                cmdUpdate.ExecuteNonQuery();
            }

            LogAction(HttpContext.Session.GetString("Username"),
            $"Updated profile for userId={id}, new email={email}",
            "Home/UpdateProfile", HttpContext.Connection.RemoteIpAddress?.ToString());


            return Json(new { success = true, message = "Profil güncellendi." });
        }

        public IActionResult EditProfile(int? id)
        {
            User model = new();

            using (var sqlConnection = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection.Open();

                System.Data.SqlClient.SqlCommand cmd;

                if (id.HasValue) 
                {
                    cmd = new System.Data.SqlClient.SqlCommand(
                        "SELECT Id, Username, Email FROM Users WHERE Id=@id", sqlConnection);
                    cmd.Parameters.AddWithValue("@id", id.Value);
                }
                else 
                {
                    string username = HttpContext.Session.GetString("Username");

                    cmd = new System.Data.SqlClient.SqlCommand(
                        "SELECT Id, Username, Email FROM Users WHERE Username=@username", sqlConnection);
                    cmd.Parameters.AddWithValue("@username", username);
                }

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    model.Id = (int)reader["Id"];
                    model.Username = reader["Username"].ToString();
                    model.Email = reader["Email"].ToString();
                }
            }
            LogAction(
                        HttpContext.Session.GetString("Username"),
                        id.HasValue
                            ? $"Opened EditProfile for userId={id.Value}"
                            : $"Opened EditProfile for own profile",
                        "Home/EditProfile",
                        HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

            return PartialView("_EditProfile", model);
        }




        public static void LogAction(string username, string action, string page, string ipAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(page))
                    throw new ArgumentException("action ve page boş olamaz.");

                if (string.IsNullOrWhiteSpace(username))
                    username = "Anonim";

                if (string.IsNullOrWhiteSpace(ipAddress))
                    ipAddress = "Bilinmiyor";

                using (var conn = new System.Data.SqlClient.SqlConnection(connectionStr))
                {
                    conn.Open();

                    var cmd = new System.Data.SqlClient.SqlCommand(@"
                INSERT INTO UserLogs (Username, Action, Page, IpAddress) 
                VALUES (@Username, @Action, @Page, @IpAddress)", conn);

                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@Page", page);
                    cmd.Parameters.AddWithValue("@IpAddress", ipAddress);

                    cmd.ExecuteNonQuery();
                }

                System.Diagnostics.Debug.WriteLine($"[LOG] {username} - {action} - {page} - {ipAddress}");
            }
            catch (Exception ex)
            {
                
                var logPath = @"C:\logs\error.log";

                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                    System.IO.File.AppendAllText(logPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - LOG HATASI: {ex}\n");
                }
                catch {  }

                throw;
            }
        }


        [HttpGet]
        public IActionResult AddUser()
        {
            return PartialView("_AddUser", new User());
        }

        [HttpPost]
        public JsonResult AddUser(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password) || string.IsNullOrWhiteSpace(user.Email))
            {
                return Json(new { success = false, message = "Tüm alanlar zorunludur." });
            }

            using (var sqlConnection = new System.Data.SqlClient.SqlConnection(connectionStr))
            {
                sqlConnection.Open();

                var cmdCheck = new System.Data.SqlClient.SqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE Username=@username", sqlConnection);
                cmdCheck.Parameters.AddWithValue("@username", user.Username);

                int exists = (int)cmdCheck.ExecuteScalar();

                if (exists > 0)
                {
                    return Json(new { success = false, message = "Bu kullanıcı adı zaten mevcut." });
                }

                var cmdInsert = new System.Data.SqlClient.SqlCommand(
                    "INSERT INTO Users (Username, PasswordHash, Email) VALUES (@username, @password, @email)", sqlConnection);

                cmdInsert.Parameters.AddWithValue("@username", user.Username);
                cmdInsert.Parameters.AddWithValue("@password", ComputeSha256Hash(user.Password));
                cmdInsert.Parameters.AddWithValue("@email", user.Email);

                cmdInsert.ExecuteNonQuery();
            }

            LogAction(HttpContext.Session.GetString("Username"),
                      $"Added new user: {user.Username}",
                      "Home/AddUser", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { success = true });
        }

        public class SaveDrawingRequest
        {
            public List<string> WktList { get; set; }
        }

        [HttpPost]
        public JsonResult SaveDrawings([FromBody] SaveDrawingRequest req)
        {
            if (req?.WktList == null || !req.WktList.Any())
            {
                return Json(new { success = false, message = "Geçerli çizim verisi yok." });
            }

            try
            {
                using (var sqlConnection = new System.Data.SqlClient.SqlConnection(connectionStr))
                {
                    sqlConnection.Open();

                    foreach (var wkt in req.WktList)
                    {
                        var cmdInsert = new System.Data.SqlClient.SqlCommand(
                            "INSERT INTO MapDrawings (Cizim, Geometry) VALUES (@name, geometry::STGeomFromText(@wkt, 4326))", sqlConnection);

                        cmdInsert.Parameters.AddWithValue("@name", "Çizim");
                        cmdInsert.Parameters.AddWithValue("@wkt", wkt);

                        cmdInsert.ExecuteNonQuery();
                    }
                }

                LogAction(HttpContext.Session.GetString("Username"),
                          $"Saved {req.WktList.Count} drawings",
                          "Home/SaveDrawings", HttpContext.Connection.RemoteIpAddress?.ToString());

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



    }
}

