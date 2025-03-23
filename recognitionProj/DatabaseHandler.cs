using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Globalization;

namespace recognitionProj
{
    public class DatabaseHandler
    {
        private readonly string _connectionString;

        public DatabaseHandler(string connectionString)
        {
            _connectionString = connectionString;
        }

        private int? ParseNullableIntOrBool(object dbValue)
        {
            // If NULL in DB, return null
            if (dbValue == DBNull.Value) return null;

            // Convert to string
            string raw = dbValue.ToString()!.Trim();

            // If it's True/False, convert
            if (raw.Equals("True", StringComparison.OrdinalIgnoreCase)) return 1;
            if (raw.Equals("False", StringComparison.OrdinalIgnoreCase)) return 0;

            // Otherwise, assume it's numeric
            return int.Parse(raw); // can still throw if unexpected
        }

    
        public void Query(string query)
        {
            using (SqlConnection connection = new(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[Query] SQL EXCEPTION: {ex.ToString()}"); // Log full details
                    throw new Exception($"Database query failed {ex.Message}", ex);
                }
            }
        }
        public void ExecuteNonQuery(string query, string[] parameterNames, object[] parameterValues)
        {
            using (SqlConnection connection = new(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new(query, connection))
                    {
                        // Add parameters to the command
                        for (int i = 0; i < parameterNames.Length; i++)
                        {
                            command.Parameters.AddWithValue(parameterNames[i], parameterValues[i]);
                        }

                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[ExecuteNonQuery] SQL EXCEPTION: {ex.ToString()}"); // Log full details
                    throw new Exception($"Database query failed {ex.Message}", ex);
                }
            }
        }


        // Insert record using parameterized query
        // Insert record using parameterized query
        public void InsertRecord(string tableName, string columns, string[] parameterNames, object[] values)
        {
            // ------------------------------------------------
            // 1) Check for '@InsID' parameter
            // ------------------------------------------------
            string insidParamName = parameterNames.FirstOrDefault(p => p.Equals("@InsID", StringComparison.OrdinalIgnoreCase));
            string facultyNameParamName = parameterNames.FirstOrDefault(p => p.Equals("@FacultyName", StringComparison.OrdinalIgnoreCase));
            string typeParamName = parameterNames.FirstOrDefault(p => p.Equals("@Type", StringComparison.OrdinalIgnoreCase)); // For Specializations

            if (!string.IsNullOrEmpty(insidParamName))
            {
                // Find indexes in 'values'
                int insidIndex = Array.IndexOf(parameterNames, insidParamName);
                object insidObj = values[insidIndex];

                if (insidObj != null && insidObj != DBNull.Value && int.TryParse(insidObj.ToString(), out int insid) && insid != 0)
                {
                    using (SqlConnection con = new SqlConnection(_connectionString))
                    {
                        con.Open();

                        // ------------------------------------------------
                        // CASE 1: Specializations (Ensures InsID + Type Uniqueness)
                        // ------------------------------------------------
                        if (tableName.Equals("Specializations", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(typeParamName))
                        {
                            int typeIndex = Array.IndexOf(parameterNames, typeParamName);
                            object typeObj = values[typeIndex];

                            if (typeObj != null && typeObj != DBNull.Value)
                            {
                                string checkSql = $"SELECT COUNT(*) FROM {tableName} WHERE InsID = @CheckInsID AND [Type] = @CheckType";
                                using (SqlCommand cmdCheck = new SqlCommand(checkSql, con))
                                {
                                    cmdCheck.Parameters.AddWithValue("@CheckInsID", insid);
                                    cmdCheck.Parameters.AddWithValue("@CheckType", typeObj);
                                    int count = (int)cmdCheck.ExecuteScalar();

                                    if (count > 0)
                                    {
                                        // Delete existing row with same InsID and Type
                                        string delSql = $"DELETE FROM {tableName} WHERE InsID = @CheckInsID AND [Type] = @CheckType";
                                        using (SqlCommand cmdDel = new SqlCommand(delSql, con))
                                        {
                                            cmdDel.Parameters.AddWithValue("@CheckInsID", insid);
                                            cmdDel.Parameters.AddWithValue("@CheckType", typeObj);
                                            cmdDel.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }

                        // ------------------------------------------------
                        // CASE 2: Faculty (Ensures InsID + FacultyName Uniqueness)
                        // ------------------------------------------------
                        else if (tableName.Equals("Faculty", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(facultyNameParamName))
                        {
                            int facultyNameIndex = Array.IndexOf(parameterNames, facultyNameParamName);
                            object facultyNameObj = values[facultyNameIndex];

                            if (facultyNameObj != null && facultyNameObj != DBNull.Value)
                            {
                                string checkSql = $"SELECT COUNT(*) FROM {tableName} WHERE InsID = @CheckInsID AND FacultyName = @CheckFacultyName";
                                using (SqlCommand cmdCheck = new SqlCommand(checkSql, con))
                                {
                                    cmdCheck.Parameters.AddWithValue("@CheckInsID", insid);
                                    cmdCheck.Parameters.AddWithValue("@CheckFacultyName", facultyNameObj);
                                    int count = (int)cmdCheck.ExecuteScalar();

                                    if (count > 0)
                                    {
                                        // Delete existing row with same InsID and FacultyName
                                        string delSql = $"DELETE FROM {tableName} WHERE InsID = @CheckInsID AND FacultyName = @CheckFacultyName";
                                        using (SqlCommand cmdDel = new SqlCommand(delSql, con))
                                        {
                                            cmdDel.Parameters.AddWithValue("@CheckInsID", insid);
                                            cmdDel.Parameters.AddWithValue("@CheckFacultyName", facultyNameObj);
                                            cmdDel.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }
                        // ------------------------------------------------
                        // CASE 3: Hospitals (Ensures InsID + HospType + HospName + HospMajor Uniqueness)
                        // ------------------------------------------------
                        else if (tableName.Equals("Hospitals", StringComparison.OrdinalIgnoreCase))
                        {
                            int hospTypeIndex = Array.IndexOf(parameterNames, "@HospType");
                            int hospNameIndex = Array.IndexOf(parameterNames, "@HospName");
                            int hospMajorIndex = Array.IndexOf(parameterNames, "@HospMajor");

                            object hospTypeObj = values[hospTypeIndex];
                            object hospNameObj = values[hospNameIndex];
                            object hospMajorObj = values[hospMajorIndex];

                            if (hospTypeObj != null && hospTypeObj != DBNull.Value &&
                                hospNameObj != null && hospNameObj != DBNull.Value &&
                                hospMajorObj != null && hospMajorObj != DBNull.Value)
                            {
                                string checkSql = $"SELECT COUNT(*) FROM {tableName} WHERE InsID = @CheckInsID AND HospType = @CheckHospType AND HospName = @CheckHospName AND HospMajor = @CheckHospMajor";
                                using (SqlCommand cmdCheck = new SqlCommand(checkSql, con))
                                {
                                    cmdCheck.Parameters.AddWithValue("@CheckInsID", insid);
                                    cmdCheck.Parameters.AddWithValue("@CheckHospType", hospTypeObj);
                                    cmdCheck.Parameters.AddWithValue("@CheckHospName", hospNameObj);
                                    cmdCheck.Parameters.AddWithValue("@CheckHospMajor", hospMajorObj);

                                    int count = (int)cmdCheck.ExecuteScalar();

                                    if (count > 0)
                                    {
                                        // Delete existing row with the same InsID, HospType, HospName, and HospMajor
                                        string delSql = $"DELETE FROM {tableName} WHERE InsID = @CheckInsID AND HospType = @CheckHospType AND HospName = @CheckHospName AND HospMajor = @CheckHospMajor";
                                        using (SqlCommand cmdDel = new SqlCommand(delSql, con))
                                        {
                                            cmdDel.Parameters.AddWithValue("@CheckInsID", insid);
                                            cmdDel.Parameters.AddWithValue("@CheckHospType", hospTypeObj);
                                            cmdDel.Parameters.AddWithValue("@CheckHospName", hospNameObj);
                                            cmdDel.Parameters.AddWithValue("@CheckHospMajor", hospMajorObj);
                                            cmdDel.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }
                    
                
            
        

                        // ------------------------------------------------
                        // DEFAULT CASE: Handle Other Tables (Deletes Based on InsID Only)
                        // ------------------------------------------------
                        else
                        {
                            // Skip deletion for Meetings to preserve previous meeting data
                            if (!tableName.Equals("Meetings", StringComparison.OrdinalIgnoreCase))
                            {
                                string checkSql = $"SELECT COUNT(*) FROM {tableName} WHERE InsID = @CheckInsID";
                                using (SqlCommand cmdCheck = new SqlCommand(checkSql, con))
                                {
                                    cmdCheck.Parameters.AddWithValue("@CheckInsID", insid);
                                    int count = (int)cmdCheck.ExecuteScalar();

                                    if (count > 0)
                                    {
                                        // Delete existing row with same InsID
                                        string delSql = $"DELETE FROM {tableName} WHERE InsID = @CheckInsID";
                                        using (SqlCommand cmdDel = new SqlCommand(delSql, con))
                                        {
                                            cmdDel.Parameters.AddWithValue("@CheckInsID", insid);
                                            cmdDel.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // ------------------------------------------------
            // 2) Perform the INSERT operation
            // ------------------------------------------------
            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({string.Join(",", parameterNames)})";

            using (SqlConnection connection = new(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new(query, connection))
                    {
                        for (int i = 0; i < parameterNames.Length; i++)
                        {
                            command.Parameters.AddWithValue(parameterNames[i], values[i] ?? DBNull.Value);
                        }
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[InsertRecord] SQL EXCEPTION: {ex}");
                    throw new Exception($"Database insert operation failed: {ex.Message}", ex);
                }
            }
        }




        // Update record using parameterized query
        public void UpdateRecord(string tableName, string setValues, string condition, string[] parameterNames, object[] values)
        {
            string query = $"UPDATE {tableName} SET {setValues} WHERE {condition}";

            using (SqlConnection connection = new(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new(query, connection))
                    {
                        for (int i = 0; i < parameterNames.Length; i++)
                        {
                            command.Parameters.AddWithValue(parameterNames[i], values[i] ?? DBNull.Value);
                        }
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[UpdateRecord] SQL EXCEPTION: {ex.ToString()}"); // Log full details
                    throw new Exception($"Database update operation failed: {ex.Message}", ex);
                }
            }
        }


        // Delete record
        public int DeleteRecord(string tableName, Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                throw new ArgumentException("Delete condition parameters cannot be empty.");
            }

            // Construct WHERE clause with parameters
            string condition = string.Join(" AND ", parameters.Keys.Select(key => $"{key} = @{key}"));
            string query = $"DELETE FROM {tableName} WHERE {condition}";

            Console.WriteLine($"[DeleteRecord] SQL Query: {query}");
            Console.WriteLine("[DeleteRecord] Parameters:");
            foreach (var param in parameters)
            {
                Console.WriteLine($" - {param.Key}: {param.Value}");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue($"@{param.Key}", param.Value ?? DBNull.Value);
                        }

                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"[DeleteRecord] Rows Affected: {rowsAffected}");

                        return rowsAffected; // Return number of rows deleted
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[DeleteRecord] SQL EXCEPTION: {ex}");
                    throw new Exception($"Database delete operation failed: {ex.Message}", ex);
                }
            }
        }

        // Select records
        public DataTable SelectRecords(string query)
        {
            DataTable dataTable = new();

            using (SqlConnection connection = new(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[SelectRecords] SQL EXCEPTION: {ex.ToString()}"); // Log full details
                    throw new Exception($"Database select operation failed: {ex.Message}", ex);
                }
            }

            return dataTable;
        }

        public DataTable SelectRecords(string query, string[] parameterNames, object[] values)
        {
            DataTable dataTable = new();

            using (SqlConnection connection = new(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new(query, connection))
                    {
                        for (int i = 0; i < parameterNames.Length; i++)
                        {
                            command.Parameters.AddWithValue(parameterNames[i], values[i] ?? DBNull.Value);
                        }

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[SelectRecords] SQL EXCEPTION: {ex.ToString()}"); // Log full details
                    throw new Exception($"Database select operation failed: {ex.Message}", ex);
                }
            }

            return dataTable;
        }

       
        // Insert a new specialization record
        public void InsertSpecialization(Specialization specialization)
        {
            string tableName = "Specializations";
            string columns = "[InsID],[Type], [NumStu], [NumProf], [NumAssociative], [NumAssistant],[NumProfPractice], [NumberLecturers], [NumAssisLecturer], [NumOtherTeachers]," +
               
                " [Color],[Ratio], [NumPhdHolders], [PracticalHours] ,[TheoreticalHours]";

            string[] parameterNames = {"@InsID",
                "@Type", "@NumStu",  "@NumProf",//
                "@NumAssociative", "@NumAssistant", "@NumProfPractice",
                "@NumberLecturers", "@NumAssisLecturer", "@NumOtherTeachers",
               // "@SpecAttachName", "@SpecAttachDesc", 
                "@Color","@Ratio" ,"@NumPhdHolders", "@PracticalHours", "@TheoreticalHours"
            };

            object[] values = {specialization.InsID,
                specialization.Type, specialization.NumStu,
                specialization.NumProf, specialization.NumAssociative,
                specialization.NumAssistant,specialization.NumProfPractice, 
                specialization.NumberLecturers, specialization.NumAssisLecturer, specialization.NumOtherTeachers,
               // specialization.SpecAttachName, specialization.SpecAttachDesc,
                specialization.Color ,specialization.Ratio, specialization.NumPhdHolders, specialization.PracticalHours, specialization.TheoreticalHours
            };

            InsertRecord(tableName, columns, parameterNames, values);
        }

        // Fetch a specialization record by InsID
        public Specialization GetSpecialization(int insId, string type)
        {
            string query = $"SELECT * FROM Specializations WHERE [InsID] = @InsID AND [Type] = @Type";
            string[] parameterNames = { "@InsID", "@Type" };
            object[] values = { insId, type };

            DataTable dataTable = SelectRecords(query, parameterNames, values);

            if (dataTable.Rows.Count > 0)
            {
                DataRow row = dataTable.Rows[0];

                return new Specialization(
                    insId: int.Parse(row["InsID"].ToString()),
                    type: row["Type"].ToString(),
                    numStu: int.Parse(row["NumStu"].ToString()),
                    numProf: int.Parse(row["NumProf"].ToString()),
                    numAssociative: int.Parse(row["NumAssociative"].ToString()),
                    numAssistant: int.Parse(row["NumAssistant"].ToString()),
                    numProfPractice: row["NumProfPractice"] != DBNull.Value ? int.Parse(row["NumProfPractice"].ToString()) : 0,
                    numberLecturers: int.Parse(row["NumberLecturers"].ToString()),
                    numAssisLecturer: int.Parse(row["NumAssisLecturer"].ToString()),
                    numOtherTeachers: int.Parse(row["NumOtherTeachers"].ToString()),
                    color: int.Parse(row["Color"].ToString()),
                    ratio: double.Parse(row["Ratio"].ToString()),
                    numPhdHolders: int.Parse(row["NumPhdHolders"].ToString()),
                    practicalHours: row["PracticalHours"] != DBNull.Value ? (int?)int.Parse(row["PracticalHours"].ToString()) : null,
                    theoreticalHours: row["TheoreticalHours"] != DBNull.Value ? (int?)int.Parse(row["TheoreticalHours"].ToString()) : null
                );
            }
            else
            {
                return null;
            }
        }




        // Update specialization record
        public void UpdateSpecialization(Specialization specialization)
        {//
            string query = $"UPDATE Specializations SET [Type] = @Type, [NumStu] = @NumStu,  " +
                " [NumProf] = @NumProf, [NumAssociative] = @NumAssociative, [NumAssistant] = @NumAssistant,[NumProfPractice]=@NumProfPractice, " +
                "[NumberLecturers] = @NumberLecturers, [NumAssisLecturer] = @NumAssisLecturer, " +
                "[NumOtherTeachers] = @NumOtherTeachers," +
               
                " [Color] = @Color ,[Ratio]=@Ratio, [NumPhdHolders] = @NumPhdHolders, [PracticalHours] = @PracticalHours, [TheoreticalHours] = @TheoreticalHours WHERE [InsID] = @InsID";
            //
            string[] parameterNames = {
                "@Type", "@NumStu", "@NumProf", "@NumAssociative", "@NumAssistant","@NumProfPractice",
                 "@NumberLecturers", "@NumAssisLecturer", "@NumOtherTeachers",
                
                "@InsID", "@Color" ,"@Ratio", "@NumPhdHolders", "@PracticalHours", "@TheoreticalHours"
            };
            //
            object[] values = {
                specialization.Type, specialization.NumStu, 
                specialization.NumProf, specialization.NumAssociative, specialization.NumAssistant,specialization.NumProfPractice,
                specialization.NumberLecturers, specialization.NumAssisLecturer, specialization.NumOtherTeachers,
               
                specialization.InsID, specialization.Color ,specialization.Ratio, specialization.NumPhdHolders, specialization.PracticalHours, specialization.TheoreticalHours
            };
            //
            UpdateRecord("Specializations", "[Type] = @Type, [NumStu] = @NumStu,  " +
                " [NumProf] = @NumProf, [NumAssociative] = @NumAssociative, [NumAssistant] = @NumAssistant,[NumProfPractice]=@NumProfPractice,  " +
                "[NumberLecturers] = @NumberLecturers, [NumAssisLecturer] = @NumAssisLecturer, " +
                "[NumOtherTeachers] = @NumOtherTeachers," +
                
                ",[Color]=@Color ,[Ratio]=@Ratio,[NumPhdHolders]=@NumPhdHolders,[PracticalHours]=@PracticalHours,[TheoreticalHours]=@TheoreticalHours", "[InsID] = @InsID", parameterNames, values);
        }

        // Delete specialization record by InsID
        public void DeleteSpecialization(int insId)
        {
           // DeleteRecord("Specializations", $"[InsID] = {insId}");
        }

        // Additional methods for AcademicInfo and other records can follow the same pattern
        public void InsertAcademicInfo(AcademicInfo academicInfo)
        {
            string tableName = "AcademicInfo";
            string columns = "[InsID], [InsTypeID], [InsType], [HighEdu_Rec], [QualityDept_Rec], [StudyLangCitizen], [StudyLangInter], [JointClass], " +
                             "[StudySystem], [MinHours], [MaxHours], [ResearchScopus], [ResearchOthers], [Practicing], [StudyAttendance], " +
                             "[StudentsMove], [StudyAttendanceDesc], [StudentsMoveDesc], [DistanceLearning], [MaxHoursDL], [MaxYearsDL], " +
                             "[MaxSemsDL], [Diploma], [DiplomaTest], [HoursPercentage], [EducationType], [AvailableDegrees], [Speciality], " +
                             "[ARWURank], [THERank], [QSRank], [OtherRank], [NumOfScopusResearches], [ScopusFrom], [ScopusTo], " +
                             "[NumOfClarivateResearches], [ClarivateFrom], [ClarivateTo], [Accepted],[LocalARWURank],[LocalTHERank],[LocalQSRank] ";

            string[] parameterNames = {
        "@InsID", "@InsTypeID", "@InsType", "@HighEdu_Rec", "@QualityDept_Rec", "@StudyLangCitizen", "@StudyLangInter", "@JointClass",
        "@StudySystem", "@MinHours", "@MaxHours", "@ResearchScopus", "@ResearchOthers", "@Practicing", "@StudyAttendance", "@StudentsMove",
        "@StudyAttendanceDesc", "@StudentsMoveDesc", "@DistanceLearning", "@MaxHoursDL", "@MaxYearsDL", "@MaxSemsDL", "@Diploma",
        "@DiplomaTest", "@HoursPercentage", "@EducationType", "@AvailableDegrees", "@Speciality", "@ARWURank", "@THERank", "@QSRank", "@OtherRank",
        "@NumOfScopusResearches", "@ScopusFrom", "@ScopusTo", "@NumOfClarivateResearches", "@ClarivateFrom", "@ClarivateTo", "@Accepted", "@LocalARWURank" ,
        "@LocalTHERank" , "@LocalQSRank"
    };

            object[] values = {
        academicInfo.InsID, academicInfo.InsTypeID, academicInfo.InsType, academicInfo.HighEdu_Rec, academicInfo.QualityDept_Rec,
        academicInfo.StudyLangCitizen, academicInfo.StudyLangInter, academicInfo.JointClass, academicInfo.StudySystem,
        academicInfo.MinHours, academicInfo.MaxHours, academicInfo.ResearchScopus, academicInfo.ResearchOthers, academicInfo.Practicing,
        academicInfo.StudyAttendance, academicInfo.StudentsMove, academicInfo.StudyAttendanceDesc, academicInfo.StudentsMoveDesc,
        academicInfo.DistanceLearning, academicInfo.MaxHoursDL, academicInfo.MaxYearsDL, academicInfo.MaxSemsDL, academicInfo.Diploma,
        academicInfo.DiplomaTest, academicInfo.HoursPercentage, academicInfo.EducationType, academicInfo.AvailableDegrees, academicInfo.Speciality,
        academicInfo.ARWURank, academicInfo.THERank, academicInfo.QSRank, academicInfo.OtherRank,
        academicInfo.NumOfScopusResearches, academicInfo.ScopusFrom, academicInfo.ScopusTo,
        academicInfo.NumOfClarivateResearches, academicInfo.ClarivateFrom, academicInfo.ClarivateTo,
        academicInfo.Accepted,academicInfo.LocalARWURank, academicInfo.LocalTHERank,academicInfo.LocalQSRank 
    };

            InsertRecord(tableName, columns, parameterNames, values);
        }

        public AcademicInfo GetAcademicInfoById(int insID)
        {
            string query = "SELECT * FROM AcademicInfo WHERE [InsID] = @InsID";
            string[] parameterNames = { "@InsID" };
            object[] values = { insID };

            DataTable dataTable = SelectRecords(query, parameterNames, values);

            if (dataTable.Rows.Count == 0)
                return null;

            DataRow row = dataTable.Rows[0];

            // Convert to the proper types
            // If you had any date/time columns in "MM/dd/yyyy ..." format, you'd do the TryParse approach
            // but your code shows scopus as int fields, so no DateOnly parsing is needed.

            AcademicInfo ai = new AcademicInfo(
    InsID: int.Parse(row["InsID"].ToString()),
    InsTypeID: row["InsTypeID"] != DBNull.Value ? (int?)int.Parse(row["InsTypeID"].ToString()) : null,
    InsType: row["InsType"].ToString(),
    HighEdu_Rec: row["HighEdu_Rec"] != DBNull.Value ? (int?)int.Parse(row["HighEdu_Rec"].ToString()) : null,
    QualityDept_Rec: row["QualityDept_Rec"] != DBNull.Value ? (int?)int.Parse(row["QualityDept_Rec"].ToString()) : null,
    StudyLangCitizen: row["StudyLangCitizen"].ToString(),
    StudyLangInter: row["StudyLangInter"].ToString(),
    JointClass: row["JointClass"] != DBNull.Value ? (int?)int.Parse(row["JointClass"].ToString()) : null,
    StudySystem: row["StudySystem"].ToString(),
    MinHours: row["MinHours"] != DBNull.Value ? (int?)int.Parse(row["MinHours"].ToString()) : null,
    MaxHours: row["MaxHours"] != DBNull.Value ? (int?)int.Parse(row["MaxHours"].ToString()) : null,
    ResearchScopus: row["ResearchScopus"].ToString(),
    ResearchOthers: row["ResearchOthers"].ToString(),
    Practicing: ParseNullableIntOrBool(row["Practicing"]),
    StudyAttendance: ParseNullableIntOrBool(row["StudyAttendance"]),
    StudentsMove: ParseNullableIntOrBool(row["StudentsMove"]),
    StudyAttendanceDesc: row["StudyAttendanceDesc"].ToString(),
    StudentsMoveDesc: row["StudentsMoveDesc"].ToString(),
    DistanceLearning: ParseNullableIntOrBool(row["DistanceLearning"]),
    MaxHoursDL: row["MaxHoursDL"] != DBNull.Value ? (int?)int.Parse(row["MaxHoursDL"].ToString()) : null,
    MaxYearsDL: row["MaxYearsDL"] != DBNull.Value ? (int?)int.Parse(row["MaxYearsDL"].ToString()) : null,
    MaxSemsDL: row["MaxSemsDL"] != DBNull.Value ? (int?)int.Parse(row["MaxSemsDL"].ToString()) : null,
    Diploma: row["Diploma"] != DBNull.Value ? (int?)int.Parse(row["Diploma"].ToString()) : null,
    DiplomaTest: row["DiplomaTest"] != DBNull.Value ? (int?)int.Parse(row["DiplomaTest"].ToString()) : null,
    HoursPercentage: row["HoursPercentage"] != DBNull.Value ? (int?)int.Parse(row["HoursPercentage"].ToString()) : null,
    EducationType: row["EducationType"].ToString(),
    AvailableDegrees: row["AvailableDegrees"].ToString(),
    Speciality: row["Speciality"].ToString(),
    ARWURank: row["ARWURank"] != DBNull.Value ? int.Parse(row["ARWURank"].ToString() ?? "0") : 0,
    THERank: row["THERank"] != DBNull.Value ? int.Parse(row["THERank"].ToString() ?? "0") : 0,
    QSRank: row["QSRank"] != DBNull.Value ? int.Parse(row["QSRank"].ToString() ?? "0") : 0,
    OtherRank: row["OtherRank"].ToString(),
    NumOfScopusResearches: row["NumOfScopusResearches"] != DBNull.Value ? int.Parse(row["NumOfScopusResearches"].ToString() ?? "0") : 0,
    ScopusFrom: row["ScopusFrom"] != DBNull.Value ? int.Parse(row["ScopusFrom"].ToString() ?? "0") : 0,
    ScopusTo: row["ScopusTo"] != DBNull.Value ? int.Parse(row["ScopusTo"].ToString() ?? "0") : 0,
    NumOfClarivateResearches: row["NumOfClarivateResearches"] != DBNull.Value ? int.Parse(row["NumOfClarivateResearches"].ToString() ?? "0") : 0,
    ClarivateFrom: row["ClarivateFrom"] != DBNull.Value ? int.Parse(row["ClarivateFrom"].ToString() ?? "0") : 0,
    ClarivateTo: row["ClarivateTo"] != DBNull.Value ? int.Parse(row["ClarivateTo"].ToString() ?? "0") : 0,
    Accepted: ParseNullableIntOrBool(row["Accepted"]),
     LocalARWURank: row["LocalARWURank"] != DBNull.Value ? int.Parse(row["LocalARWURank"].ToString() ?? "0") : 0,
     LocalTHERank: row["LocalTHERank"] != DBNull.Value ? int.Parse(row["LocalTHERank"].ToString() ?? "0") : 0,
    LocalQSRank:row["LocalQSRank"]!= DBNull.Value ? int.Parse(row["LocalQSRank"].ToString() ?? "0") : 0
    
   
);


            return ai;
        }
        public void UpdateAcademicInfo(AcademicInfo academicInfo)
        {
            string tableName = "AcademicInfo";

            // Set values for the columns that need to be updated
            string setValues = "[InsTypeID] = @InsTypeID, [InsType] = @InsType, [HighEdu_Rec] = @HighEdu_Rec, [QualityDept_Rec] = @QualityDept_Rec, " +
                               "[StudyLangCitizen] = @StudyLangCitizen, [StudyLangInter] = @StudyLangInter, [JointClass] = @JointClass, [StudySystem] = @StudySystem, " +
                               "[MinHours] = @MinHours, [MaxHours] = @MaxHours, [ResearchScopus] = @ResearchScopus, [ResearchOthers] = @ResearchOthers, " +
                               "[Practicing] = @Practicing, [StudyAttendance] = @StudyAttendance, [StudentsMove] = @StudentsMove, [StudyAttendanceDesc] = @StudyAttendanceDesc, " +
                               "[StudentsMoveDesc] = @StudentsMoveDesc, [DistanceLearning] = @DistanceLearning, [MaxHoursDL] = @MaxHoursDL, [MaxYearsDL] = @MaxYearsDL, " +
                               "[MaxSemsDL] = @MaxSemsDL, [Diploma] = @Diploma, [DiplomaTest] = @DiplomaTest, [HoursPercentage] = @HoursPercentage, [EducationType] = @EducationType, " +
                               "[AvailableDegrees] = @AvailableDegrees,[Speciality]=@Speciality, [ARWURank] = @ARWURank, [THERank] = @THERank, [QSRank] = @QSRank, " +
                               "[OtherRank] = @OtherRank, [NumOfScopusResearches] = @NumOfScopusResearches, [ScopusFrom] = @ScopusFrom, [ScopusTo] = @ScopusTo, " +
                               "[Accepted] = @Accepted";

            // Define the condition (which record to update)
            string condition = "[InsID] = @InsID";

            // Define the parameters to be used in the SQL query
            string[] parameterNames = {
        "@InsTypeID", "@InsType", "@HighEdu_Rec", "@QualityDept_Rec", "@StudyLangCitizen", "@StudyLangInter", "@JointClass",
        "@StudySystem", "@MinHours", "@MaxHours", "@ResearchScopus", "@ResearchOthers", "@Practicing", "@StudyAttendance",
        "@StudentsMove", "@StudyAttendanceDesc", "@StudentsMoveDesc", "@DistanceLearning", "@MaxHoursDL", "@MaxYearsDL",
        "@MaxSemsDL", "@Diploma", "@DiplomaTest", "@HoursPercentage", "@EducationType", "@AvailableDegrees","@Speciality", "@Faculties",
        "@ARWURank", "@THERank", "@QSRank", "@OtherRank", "@NumOfScopusResearches", "@ScopusFrom", "@ScopusTo", "@Infrastructure", "@Accepted", "@InsID"
    };

            // Set the values for each parameter
            object[] values = {
        academicInfo.InsTypeID, academicInfo.InsType, academicInfo.HighEdu_Rec, academicInfo.QualityDept_Rec, academicInfo.StudyLangCitizen,
        academicInfo.StudyLangInter, academicInfo.JointClass, academicInfo.StudySystem, academicInfo.MinHours, academicInfo.MaxHours,
        academicInfo.ResearchScopus, academicInfo.ResearchOthers, academicInfo.Practicing, academicInfo.StudyAttendance,
        academicInfo.StudentsMove, academicInfo.StudyAttendanceDesc, academicInfo.StudentsMoveDesc, academicInfo.DistanceLearning,
        academicInfo.MaxHoursDL, academicInfo.MaxYearsDL, academicInfo.MaxSemsDL, academicInfo.Diploma, academicInfo.DiplomaTest,
        academicInfo.HoursPercentage, academicInfo.EducationType,  academicInfo.AvailableDegrees,academicInfo.Speciality,
        academicInfo.ARWURank, academicInfo.THERank, academicInfo.QSRank, academicInfo.OtherRank, academicInfo.NumOfScopusResearches,
        academicInfo.ScopusFrom, academicInfo.ScopusTo, academicInfo.Accepted, academicInfo.InsID
    };

            // Call the UpdateRecord method with the appropriate parameters
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }

       
        public void InsertHospital(Hospitals hospital)
        {
            string tableName = "Hospitals";
            string columns = "[InsID], [HospType], [HospName], [HospMajor]";

            string[] parameterNames = { "@InsID", "@HospType", "@HospName", "@HospMajor" };
            object[] values = { hospital.InsID, hospital.HospType, hospital.HospName, hospital.HospMajor };

            // Call the InsertRecord method of the DatabaseHandler
            InsertRecord(tableName, columns, parameterNames, values);
        }
        public void UpdateHospital(Hospitals hospital)
        {
            string tableName = "Hospitals";

            // Columns to update
            string setValues = "[HospType] = @HospType, [HospName] = @HospName, [HospMajor] = @HospMajor";

            // Condition for which record to update (based on InsID and HospID)
            string condition = "[InsID] = @InsID AND [HospID] = @HospID";

            string[] parameterNames = { "@HospType", "@HospName", "@HospMajor", "@InsID" };
            object[] values = { hospital.HospType, hospital.HospName, hospital.HospMajor, hospital.InsID};

            // Call the UpdateRecord method of the DatabaseHandler
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }
        public void DeleteHospital(int insID, int hospID)
        {
            string tableName = "Hospitals";

            // Condition for which record to delete (based on InsID and HospID)
            string condition = "[InsID] = @InsID AND [HospID] = @HospID";

            // Call the DeleteRecord method of the DatabaseHandler
           // DeleteRecord(tableName, condition);
        }
        public List<Hospitals> GetAllHospitals()
        {
            string query = "SELECT [InsID], [HospType], [HospName], [HospMajor] FROM Hospitals";
            DataTable resultTable = SelectRecords(query);

            List<Hospitals> hospitalsList = new();

            foreach (DataRow row in resultTable.Rows)
            {
                int insID = Convert.ToInt32(row["InsID"]);
               
                string hospType = row["HospType"].ToString();
                string hospName = row["HospName"].ToString();
                string hospMajor = row["HospMajor"].ToString();

                hospitalsList.Add(new Hospitals(insID, hospType, hospName, hospMajor));
            }

            return hospitalsList;
        }
        public void InsertInfrastructure(Infrastructure infrastructure)
        {
            string tableName = "Infrastructure";
            string columns = "[InsID], [Area], [Sites], [Terr], [Halls], [Library]";

            string[] parameterNames = { "@InsID", "@Area", "@Sites", "@Terr", "@Halls", "@Library" };
            object[] values = { infrastructure.InsID, infrastructure.Area, infrastructure.Sites, infrastructure.Terr, infrastructure.Halls, infrastructure.Library };

            // Call the InsertRecord method of the DatabaseHandler
            InsertRecord(tableName, columns, parameterNames, values);
        }
        public void UpdateInfrastructure(Infrastructure infrastructure)
        {
            string tableName = "Infrastructure";

            // Columns to update
            string setValues = "[Area] = @Area, [Sites] = @Sites, [Terr] = @Terr, [Halls] = @Halls, [Library] = @Library";

            // Condition for which record to update (based on InsID)
            string condition = "[InsID] = @InsID";

            string[] parameterNames = { "@Area", "@Sites", "@Terr", "@Halls", "@Library", "@InsID" };
            object[] values = { infrastructure.Area, infrastructure.Sites, infrastructure.Terr, infrastructure.Halls, infrastructure.Library,  infrastructure.InsID };

            // Call the UpdateRecord method of the DatabaseHandler
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }
        public void DeleteInfrastructure(int insID)
        {
            string tableName = "Infrastructure";

            // Condition for which record to delete (based on InsID)
            string condition = "[InsID] = @InsID";

            // Call the DeleteRecord method of the DatabaseHandler
          //  DeleteRecord(tableName, condition);
        }
        public void InsertLibrary(Library library)
        {
            string tableName = "Library";
            string columns = "[InsID], [Area], [Capacity], [ArBooks], [EnBooks], [Papers], [EBooks], [ESubscription]";

            string[] parameterNames = { "@InsID", "@Area", "@Capacity", "@ArBooks", "@EnBooks", "@Papers", "@EBooks", "@ESubscription" };
            object[] values = { library.InsID, library.Area, library.Capacity, library.ArBooks, library.EnBooks, library.Papers, library.EBooks, library.ESubscription };

            // Call the InsertRecord method of the DatabaseHandler
            InsertRecord(tableName, columns, parameterNames, values);
        }
        public void UpdateLibrary(Library library)
        {
            string tableName = "Library";

            // Columns to update
            string setValues = "[Area] = @Area, [Capacity] = @Capacity, [ArBooks] = @ArBooks, [EnBooks] = @EnBooks, [Papers] = @Papers, [EBooks] = @EBooks, [ESubscription] = @ESubscription";

            // Condition for which record to update (based on InsID)
            string condition = "[InsID] = @InsID";

            string[] parameterNames = { "@Area", "@Capacity", "@ArBooks", "@EnBooks", "@Papers", "@EBooks", "@ESubscription", "@InsID" };
            object[] values = { library.Area, library.Capacity, library.ArBooks, library.EnBooks, library.Papers, library.EBooks, library.ESubscription, library.InsID };

            // Call the UpdateRecord method of the DatabaseHandler
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }
        public Library GetLibraryById(int insID)
        {
            string query = @"
    SELECT [InsID], [Area], [Capacity], [ArBooks], [EnBooks], [Papers], [EBooks], [ESubscription]
    FROM Library WHERE InsID = @InsID";

            string[] parameterNames = { "@InsID" };
            object[] values = { insID };

            DataTable dataTable = SelectRecords(query, parameterNames, values);
            if (dataTable.Rows.Count == 0)
                return null;

            DataRow row = dataTable.Rows[0];
            return new Library(
                insID: Convert.ToInt32(row["InsID"]),
                area: row["Area"] != DBNull.Value ? Convert.ToInt32(row["Area"]) : (int?)null,
                capacity: row["Capacity"] != DBNull.Value ? Convert.ToInt32(row["Capacity"]) : (int?)null,
                arBooks: row["ArBooks"] != DBNull.Value ? Convert.ToInt32(row["ArBooks"]) : (int?)null,
                enBooks: row["EnBooks"] != DBNull.Value ? Convert.ToInt32(row["EnBooks"]) : (int?)null,
                papers: row["Papers"] != DBNull.Value ? Convert.ToInt32(row["Papers"]) : (int?)null,
                eBooks: row["EBooks"] != DBNull.Value ? Convert.ToInt32(row["EBooks"]) : (int?)null,
                eSubscription: row["ESubscription"] != DBNull.Value ? Convert.ToInt32(row["ESubscription"]) : (int?)null
            );
        }


        public void DeleteLibrary(int insID)
        {
            string tableName = "Library";

            // Condition for which record to delete (based on InsID)
            string condition = "[InsID] = @InsID";

            // Call the DeleteRecord method of the DatabaseHandler
            //DeleteRecord(tableName, condition);
        }
        public List<Library> GetAllLibraries()
        {
            string query = "SELECT [InsID], [Area], [Capacity], [ArBooks], [EnBooks], [Papers], [EBooks], [ESubscription] FROM Library";
            DataTable resultTable = SelectRecords(query);

            List<Library> libraryList = new();

            foreach (DataRow row in resultTable.Rows)
            {
                int insID = Convert.ToInt32(row["InsID"]);
                int? area = row["Area"] != DBNull.Value ? Convert.ToInt32(row["Area"]) : (int?)null;
                int? capacity = row["Capacity"] != DBNull.Value ? Convert.ToInt32(row["Capacity"]) : (int?)null;
                int? arBooks = row["ArBooks"] != DBNull.Value ? Convert.ToInt32(row["ArBooks"]) : (int?)null;
                int? enBooks = row["EnBooks"] != DBNull.Value ? Convert.ToInt32(row["EnBooks"]) : (int?)null;
                int? papers = row["Papers"] != DBNull.Value ? Convert.ToInt32(row["Papers"]) : (int?)null;
                int? eBooks = row["EBooks"] != DBNull.Value ? Convert.ToInt32(row["EBooks"]) : (int?)null;
                int? eSubscription = row["ESubscription"] != DBNull.Value ? Convert.ToInt32(row["ESubscription"]) : (int?)null;

                libraryList.Add(new Library(insID, area, capacity, arBooks, enBooks, papers, eBooks, eSubscription));
            }

            return libraryList;
        }
       
        public PublicInfo GetPublicInfoById(int insID)
        {
            string query = @"
        SELECT [InsID]
              ,[InsName]
              ,[Provider]
              ,[StartDate]
              ,[SDateT]
              ,[SDateNT]
              ,[SupervisorID]
              ,[Supervisor]
              ,[PreName]
              ,[PreDegree]
              ,[PreMajor]
              ,[Postal]
              ,[Phone]
              ,[Fax]
              ,[Email]
              ,[Website]
              ,[Vision]
              ,[Mission]
              ,[Goals]
              ,[InsValues]
              ,[LastEditDate]
              ,[EntryDate]
              ,[Country]
              ,[City]
              ,[Address]
              ,[CreationDate]
              ,[StudentAcceptanceDate]
              ,[OtherInfo]
        FROM PublicInfo
        WHERE [InsID] = @InsID";

            string[] parameterNames = { "@InsID" };
            object[] values = { insID };

            DataTable dataTable = SelectRecords(query, parameterNames, values);

            if (dataTable.Rows.Count == 0)
                return null;

            DataRow row = dataTable.Rows[0];

            // Parse DateOnly fields or fall back to default(DateOnly) if null/empty
            // Adjust date formats as needed
            string rawDate = row["EntryDate"].ToString() ?? "";
            DateOnly entryDate = default;

            if (DateTime.TryParse(rawDate, out DateTime dt))
            {
                entryDate = DateOnly.FromDateTime(dt); // Convert DateTime -> DateOnly
            }
            else
            {
                // throw exception or log error with details
                throw new Exception($"Failed to parse date: {rawDate}");

            }


            DateOnly creationDate = default;
            if (row["CreationDate"] != DBNull.Value)
            {
                string rawCreationDate = row["CreationDate"].ToString()!;
                if (DateTime.TryParse(rawCreationDate, out DateTime dtCreation))
                {
                    creationDate = DateOnly.FromDateTime(dtCreation);
                }
                else
                {
                    Console.WriteLine($"Warning: Unable to parse CreationDate '{rawCreationDate}'");
                }
            }

            DateOnly studentAcceptanceDate = default;
            if (row["StudentAcceptanceDate"] != DBNull.Value)
            {
                string rawStudentDate = row["StudentAcceptanceDate"].ToString()!;
                if (DateTime.TryParse(rawStudentDate, out DateTime dtStudent))
                {
                    studentAcceptanceDate = DateOnly.FromDateTime(dtStudent);
                }
                else
                {
                    Console.WriteLine($"Warning: Unable to parse StudentAcceptanceDate '{rawStudentDate}'");
                }
            }


            // Use the full-args constructor with matching parameter order
            PublicInfo info = new PublicInfo(
                insID: Convert.ToInt32(row["InsID"]),
                insName: row["InsName"].ToString() ?? "",
                provider: row["Provider"].ToString() ?? "",
                startDate: row["StartDate"].ToString() ?? "",
                sDateT: row["SDateT"].ToString() ?? "",
                sDateNT: row["SDateNT"].ToString() ?? "",
                supervisorID: row["SupervisorID"] != DBNull.Value ? Convert.ToInt32(row["SupervisorID"]) : null,
                supervisor: row["Supervisor"].ToString() ?? "",
                preName: row["PreName"].ToString() ?? "",
                preDegree: row["PreDegree"].ToString() ?? "",
                preMajor: row["PreMajor"].ToString() ?? "",
                postal: row["Postal"].ToString() ?? "",
                phone: row["Phone"].ToString() ?? "",
                fax: row["Fax"].ToString() ?? "",
                email: row["Email"].ToString() ?? "",
                website: row["Website"].ToString() ?? "",
                vision: row["Vision"].ToString() ?? "",
                mission: row["Mission"].ToString() ?? "",
                goals: row["Goals"].ToString() ?? "",
                insValues: row["InsValues"].ToString() ?? "",
                lastEditDate: row["LastEditDate"].ToString() ?? "",
                entryDate: entryDate,
                country: row["Country"].ToString() ?? "",
                city: row["City"].ToString() ?? "",
                address: row["Address"].ToString() ?? "",
                creationDate: creationDate,
                studentAcceptanceDate: studentAcceptanceDate,
                otherInfo: row["OtherInfo"].ToString() ?? ""
            );

            return info;
        }
        public void InsertPublicInfo(PublicInfo publicInfo)
        {
            string tableName = "PublicInfo";
            string columns = "[InsID], [InsName], [Provider], [StartDate], [SDateT], [SDateNT], [SupervisorID], [Supervisor], " +
                             "[PreName], [PreDegree], [PreMajor], [Postal], [Phone], [Fax], [Email], [Website], [Vision], " +
                             "[Mission], [Goals], [InsValues], [LastEditDate], [EntryDate], [Country], [City], [Address], [CreationDate], [StudentAcceptanceDate], [OtherInfo]";

            string[] parameterNames = {
        "@InsID", "@InsName", "@Provider", "@StartDate", "@SDateT", "@SDateNT", "@SupervisorID", "@Supervisor",
        "@PreName", "@PreDegree", "@PreMajor", "@Postal", "@Phone", "@Fax", "@Email", "@Website", "@Vision",
        "@Mission", "@Goals", "@InsValues", "@LastEditDate", "@EntryDate", "@Country", "@City", "@Address", "@CreationDate", "@StudentAcceptanceDate", "@OtherInfo"
    };

            object[] values = {
        publicInfo.InsID, publicInfo.InsName, publicInfo.Provider, publicInfo.StartDate, publicInfo.SDateT,
        publicInfo.SDateNT, publicInfo.SupervisorID, publicInfo.Supervisor, publicInfo.PreName, publicInfo.PreDegree,
        publicInfo.PreMajor, publicInfo.Postal, publicInfo.Phone, publicInfo.Fax, publicInfo.Email, publicInfo.Website,
        publicInfo.Vision, publicInfo.Mission, publicInfo.Goals, publicInfo.InsValues, publicInfo.LastEditDate,
        publicInfo.EntryDate, publicInfo.Country, publicInfo.City, publicInfo.Address, publicInfo.CreationDate, publicInfo.StudentAcceptanceDate, publicInfo.OtherInfo
    };

            // Call the InsertRecord method of the DatabaseHandler
            InsertRecord(tableName, columns, parameterNames, values);
        }
        public void UpdatePublicInfo(PublicInfo publicInfo)
        {
            string tableName = "PublicInfo";

            // Note the comma added after "[LastEditDate] = @LastEditDate,"
            string setValues =
              "[InsName] = @InsName, " +
              "[Provider] = @Provider, " +
              "[StartDate] = @StartDate, " +
              "[SDateT] = @SDateT, " +
              "[SDateNT] = @SDateNT, " +
              "[SupervisorID] = @SupervisorID, " +
              "[Supervisor] = @Supervisor, " +
              "[PreName] = @PreName, " +
              "[PreDegree] = @PreDegree, " +
              "[PreMajor] = @PreMajor, " +
              "[Postal] = @Postal, " +
              "[Phone] = @Phone, " +
              "[Fax] = @Fax, " +
              "[Email] = @Email, " +
              "[Website] = @Website, " +
              "[Vision] = @Vision, " +
              "[Mission] = @Mission, " +
              "[Goals] = @Goals, " +
              "[InsValues] = @InsValues, " +
              "[LastEditDate] = @LastEditDate, " +   // <-- ensure comma here
              "[EntryDate] = @EntryDate, " +
              "[Country] = @Country, " +
              "[City] = @City, " +
              "[Address] = @Address, " +
              "[CreationDate] = @CreationDate, " +
              "[StudentAcceptanceDate] = @StudentAcceptanceDate, " +
              "[OtherInfo] = @OtherInfo";

            // Condition
            string condition = "[InsID] = @InsID";

            // Only ONE @InsID in array
            string[] parameterNames = {
        "@InsName", "@Provider", "@StartDate", "@SDateT", "@SDateNT",
        "@SupervisorID", "@Supervisor", "@PreName", "@PreDegree", "@PreMajor",
        "@Postal", "@Phone", "@Fax", "@Email", "@Website", "@Vision",
        "@Mission", "@Goals", "@InsValues", "@LastEditDate",
        "@EntryDate", "@Country", "@City", "@Address", "@CreationDate",
        "@StudentAcceptanceDate", "@OtherInfo", "@InsID" // only once
    };

            object[] values = {
        publicInfo.InsName,
        publicInfo.Provider,
        publicInfo.StartDate,
        publicInfo.SDateT,
        publicInfo.SDateNT,
        publicInfo.SupervisorID,
        publicInfo.Supervisor,
        publicInfo.PreName,
        publicInfo.PreDegree,
        publicInfo.PreMajor,
        publicInfo.Postal,
        publicInfo.Phone,
        publicInfo.Fax,
        publicInfo.Email,
        publicInfo.Website,
        publicInfo.Vision,
        publicInfo.Mission,
        publicInfo.Goals,
        publicInfo.InsValues,
        publicInfo.LastEditDate,
        publicInfo.EntryDate,
        publicInfo.Country,
        publicInfo.City,
        publicInfo.Address,
        publicInfo.CreationDate,
        publicInfo.StudentAcceptanceDate,
        publicInfo.OtherInfo,
        publicInfo.InsID // only once
    };

            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }

        public void DeletePublicInfo(int insID)
        {
            string tableName = "PublicInfo";

            // Condition for which record to delete (based on InsID)
            string condition = "[InsID] = @InsID";

            // Call the DeleteRecord method of the DatabaseHandler
          //  DeleteRecord(tableName, condition);
        }
        public List<PublicInfo> GetAllPublicInfo()
        {
            string query = "SELECT [InsID], [InsName], [Provider], [StartDate], [SDateT], [SDateNT], [SupervisorID], [Supervisor], " +
                           "[PreName], [PreDegree], [PreMajor], [Postal], [Phone], [Fax], [Email], [Website], [Vision], " +
                           "[Mission], [Goals], [InsValues], [LastEditDate] , [EntryDate], [Country], [City], [Address], [CreationDate], [StudentAcceptanceDate], [OtherInfo] FROM PublicInfo";
            DataTable resultTable = SelectRecords(query);

            List<PublicInfo> publicInfoList = new();

            foreach (DataRow row in resultTable.Rows)
            {
                int insID = Convert.ToInt32(row["InsID"]);
                string insName = row["InsName"].ToString();
                string provider = row["Provider"].ToString();
                string startDate = row["StartDate"].ToString();
                string sDateT = row["SDateT"].ToString();
                string sDateNT = row["SDateNT"].ToString();
                int? supervisorID = row["SupervisorID"] != DBNull.Value ? Convert.ToInt32(row["SupervisorID"]) : null;
                string supervisor = row["Supervisor"].ToString();
                string preName = row["PreName"].ToString();
                string preDegree = row["PreDegree"].ToString();
                string preMajor = row["PreMajor"].ToString();
                string postal = row["Postal"].ToString();
                string phone = row["Phone"].ToString();
                string fax = row["Fax"].ToString();
                string email = row["Email"].ToString();
                string website = row["Website"].ToString();
                string vision = row["Vision"].ToString();
                string mission = row["Mission"].ToString();
                string goals = row["Goals"].ToString();
                string insValues = row["InsValues"].ToString();
                string lastEditDate = row["LastEditDate"].ToString();

                //todo, when finishing, check if the date is being translated properly everywhere


                // Instead of this...
                // DateOnly entryDate = DateOnly.ParseExact(row["EntryDate"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

                // ...do this:
                DateTime entryDateTime = DateTime.Parse(
                    row["EntryDate"].ToString(),
                    CultureInfo.InvariantCulture
                );

                DateOnly entryDate = DateOnly.FromDateTime(entryDateTime);

                string country = row["Country"].ToString();
                    string city = row["City"].ToString();
                    string address = row["Address"].ToString();
                DateTime creationDateTime = DateTime.Parse(
       row["CreationDate"].ToString(),
       CultureInfo.InvariantCulture
   );
                DateOnly creationDate = DateOnly.FromDateTime(creationDateTime);

                // likewise for StudentAcceptanceDate
                DateTime acceptanceDateTime = DateTime.Parse(
                    row["StudentAcceptanceDate"].ToString(),
                    CultureInfo.InvariantCulture
                );
                DateOnly studentAcceptanceDate = DateOnly.FromDateTime(acceptanceDateTime);


                string otherInfo = row["OtherInfo"].ToString();

                publicInfoList.Add(new PublicInfo(insID, insName, provider, startDate, sDateT, sDateNT, supervisorID, supervisor,
                                                  preName, preDegree, preMajor, postal, phone, fax, email, website, vision,
                                                  mission, goals, insValues, lastEditDate, entryDate, country, city, address, creationDate, studentAcceptanceDate, otherInfo));
            }

            return publicInfoList;
        }
        public void InsertStudentsAndStuff(StudentsAndStuff studentsAndStuff)
        {
            string tableName = "StudentsAndStuff";
            string columns = "[InsID], [StudentCitizen], [StudentInter], [StudentJordan], [StudentOverall], [StaffProfessor], " +
                             "[StaffCoProfessor], [StaffAssistantProfessor], [StaffLabSupervisor], [StaffResearcher], " +
                             "[StaffTeacher], [StaffTeacherAssistant], [StaffOthers]";

            string[] parameterNames = {
        "@InsID", "@StudentCitizen", "@StudentInter", "@StudentJordan", "@StudentOverall", "@StaffProfessor",
        "@StaffCoProfessor", "@StaffAssistantProfessor", "@StaffLabSupervisor", "@StaffResearcher", "@StaffTeacher",
        "@StaffTeacherAssistant", "@StaffOthers"
    };

            object[] values = {
        studentsAndStuff.InsID, studentsAndStuff.StudentCitizen, studentsAndStuff.StudentInter, studentsAndStuff.StudentJordan,
        studentsAndStuff.StudentOverall, studentsAndStuff.StaffProfessor, studentsAndStuff.StaffCoProfessor,
        studentsAndStuff.StaffAssistantProfessor, studentsAndStuff.StaffLabSupervisor, studentsAndStuff.StaffResearcher,
        studentsAndStuff.StaffTeacher, studentsAndStuff.StaffTeacherAssistant, studentsAndStuff.StaffOthers
    };

            // Call the InsertRecord method of the DatabaseHandler
            InsertRecord(tableName, columns, parameterNames, values);
        }
        public void UpdateStudentsAndStuff(StudentsAndStuff studentsAndStuff)
        {
            string tableName = "StudentsAndStuff";

            // Columns to update
            string setValues = "[StudentCitizen] = @StudentCitizen, [StudentInter] = @StudentInter, [StudentJordan] = @StudentJordan, " +
                               "[StudentOverall] = @StudentOverall, [StaffProfessor] = @StaffProfessor, [StaffCoProfessor] = @StaffCoProfessor, " +
                               "[StaffAssistantProfessor] = @StaffAssistantProfessor, [StaffLabSupervisor] = @StaffLabSupervisor, " +
                               "[StaffResearcher] = @StaffResearcher, [StaffTeacher] = @StaffTeacher, [StaffTeacherAssistant] = @StaffTeacherAssistant, " +
                               "[StaffOthers] = @StaffOthers";

            // Condition for which record to update (based on InsID)
            string condition = "[InsID] = @InsID";

            string[] parameterNames = {
        "@StudentCitizen", "@StudentInter", "@StudentJordan", "@StudentOverall", "@StaffProfessor", "@StaffCoProfessor",
        "@StaffAssistantProfessor", "@StaffLabSupervisor", "@StaffResearcher", "@StaffTeacher", "@StaffTeacherAssistant",
        "@StaffOthers", "@InsID"
    };

            object[] values = {
        studentsAndStuff.StudentCitizen, studentsAndStuff.StudentInter, studentsAndStuff.StudentJordan, studentsAndStuff.StudentOverall,
        studentsAndStuff.StaffProfessor, studentsAndStuff.StaffCoProfessor, studentsAndStuff.StaffAssistantProfessor,
        studentsAndStuff.StaffLabSupervisor, studentsAndStuff.StaffResearcher, studentsAndStuff.StaffTeacher,
        studentsAndStuff.StaffTeacherAssistant, studentsAndStuff.StaffOthers, studentsAndStuff.InsID
    };

            // Call the UpdateRecord method of the DatabaseHandler
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }
        public void DeleteStudentsAndStuff(int insID)
        {
            string tableName = "StudentsAndStuff";

            // Condition for which record to delete (based on InsID)
            string condition = "[InsID] = @InsID";

            // Call the DeleteRecord method of the DatabaseHandler
           // DeleteRecord(tableName, condition);
        }
        public List<StudentsAndStuff> GetAllStudentsAndStuff()
        {
            string query = "SELECT [InsID], [StudentCitizen], [StudentInter], [StudentJordan], [StudentOverall], [StaffProfessor], " +
                           "[StaffCoProfessor], [StaffAssistantProfessor], [StaffLabSupervisor], [StaffResearcher], [StaffTeacher], " +
                           "[StaffTeacherAssistant], [StaffOthers] FROM StudentsAndStuff";
            DataTable resultTable = SelectRecords(query);

            List<StudentsAndStuff> studentsAndStuffList = new();

            foreach (DataRow row in resultTable.Rows)
            {
                int insID = Convert.ToInt32(row["InsID"]);
                int? studentCitizen = row["StudentCitizen"] != DBNull.Value ? Convert.ToInt32(row["StudentCitizen"]) : null;
                int? studentInter = row["StudentInter"] != DBNull.Value ? Convert.ToInt32(row["StudentInter"]) : null;
                int? studentJordan = row["StudentJordan"] != DBNull.Value ? Convert.ToInt32(row["StudentJordan"]) : null;
                int? studentOverall = row["StudentOverall"] != DBNull.Value ? Convert.ToInt32(row["StudentOverall"]) : null;
                int? staffProfessor = row["StaffProfessor"] != DBNull.Value ? Convert.ToInt32(row["StaffProfessor"]) : null;
                int? staffCoProfessor = row["StaffCoProfessor"] != DBNull.Value ? Convert.ToInt32(row["StaffCoProfessor"]) : null;
                int? staffAssistantProfessor = row["StaffAssistantProfessor"] != DBNull.Value ? Convert.ToInt32(row["StaffAssistantProfessor"]) : null;
                int? staffLabSupervisor = row["StaffLabSupervisor"] != DBNull.Value ? Convert.ToInt32(row["StaffLabSupervisor"]) : null;
                int? staffResearcher = row["StaffResearcher"] != DBNull.Value ? Convert.ToInt32(row["StaffResearcher"]) : null;
                int? staffTeacher = row["StaffTeacher"] != DBNull.Value ? Convert.ToInt32(row["StaffTeacher"]) : null;
                int? staffTeacherAssistant = row["StaffTeacherAssistant"] != DBNull.Value ? Convert.ToInt32(row["StaffTeacherAssistant"]) : null;
                int? staffOthers = row["StaffOthers"] != DBNull.Value ? Convert.ToInt32(row["StaffOthers"]) : null;

                studentsAndStuffList.Add(new StudentsAndStuff(insID, studentCitizen, studentInter, studentJordan, studentOverall,
                                                              staffProfessor, staffCoProfessor, staffAssistantProfessor, staffLabSupervisor,
                                                              staffResearcher, staffTeacher, staffTeacherAssistant, staffOthers));
            }

            return studentsAndStuffList;
        }
        public void InsertStudyDuration(StudyDuration studyDuration)
        {
            Console.WriteLine("[DatabaseHandler] InsertStudyDuration called.");

            string tableName = "StudyDuration";
            // Add the two new columns
            string columns = @"[InsID],
                      [DiplomaDegreeMIN], [DiplomaMIN],
                      [BSC_DegreeMIN], [BSC_MIN],
                      [HigherDiplomaDegreeMIN], [HigherDiplomaMIN],
                      [MasterDegreeMIN], [MasterMIN],
                      [PhD_DegreeMIN], [PhD_MIN],
                      [SemestersPerYear], [MonthsPerSemester]";

            // We'll use placeholders for each of these columns
            string insertSql = $"INSERT INTO {tableName} ({columns}) " +
                "VALUES (@InsID, @DiplomaDegreeMIN, @DiplomaMIN, @BSC_DegreeMIN, @BSC_MIN, " +
                "@HigherDiplomaDegreeMIN, @HigherDiplomaMIN, @MasterDegreeMIN, @MasterMIN, " +
                "@PhD_DegreeMIN, @PhD_MIN, @SemestersPerYear, @MonthsPerSemester)";

            // The parameter list
            string[] parameterNames = {
        "@InsID",
        "@DiplomaDegreeMIN", "@DiplomaMIN",
        "@BSC_DegreeMIN", "@BSC_MIN",
        "@HigherDiplomaDegreeMIN", "@HigherDiplomaMIN",
        "@MasterDegreeMIN", "@MasterMIN",
        "@PhD_DegreeMIN", "@PhD_MIN",
        "@SemestersPerYear", "@MonthsPerSemester"
    };

            object[] values = {
        studyDuration.InsID,
        studyDuration.DiplomaDegreeMIN ?? (object)DBNull.Value,
        studyDuration.DiplomaMIN ?? (object)DBNull.Value,
        studyDuration.BSC_DegreeMIN ?? (object)DBNull.Value,
        studyDuration.BSC_MIN ?? (object)DBNull.Value,
        studyDuration.HigherDiplomaDegreeMIN ?? (object)DBNull.Value,
        studyDuration.HigherDiplomaMIN ?? (object)DBNull.Value,
        studyDuration.MasterDegreeMIN ?? (object)DBNull.Value,
        studyDuration.MasterMIN ?? (object)DBNull.Value,
        studyDuration.PhD_DegreeMIN ?? (object)DBNull.Value,
        studyDuration.PhD_MIN ?? (object)DBNull.Value,
        studyDuration.SemestersPerYear.HasValue ? studyDuration.SemestersPerYear.Value : (object)DBNull.Value,
        studyDuration.MonthsPerSemester.HasValue ? studyDuration.MonthsPerSemester.Value : (object)DBNull.Value
    };
            InsertRecord(tableName, columns, parameterNames, values);
            /*// Then call InsertRecord or do it manually
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(insertSql, conn))
            {
                conn.Open();
                for (int i = 0; i < parameterNames.Length; i++)
                {
                    cmd.Parameters.AddWithValue(parameterNames[i], values[i]);
                }
                cmd.ExecuteNonQuery();
            }*/
        }


        public void UpdateStudyDuration(StudyDuration studyDuration)
        {
            string tableName = "StudyDuration";

            // Add the new fields to the set list
            string setValues = @"[DiplomaDegreeMIN] = @DiplomaDegreeMIN,
                         [DiplomaMIN] = @DiplomaMIN,
                         [BSC_DegreeMIN] = @BSC_DegreeMIN,
                         [BSC_MIN] = @BSC_MIN,
                         [HigherDiplomaDegreeMIN] = @HigherDiplomaDegreeMIN,
                         [HigherDiplomaMIN] = @HigherDiplomaMIN,
                         [MasterDegreeMIN] = @MasterDegreeMIN,
                         [MasterMIN] = @MasterMIN,
                         [PhD_DegreeMIN] = @PhD_DegreeMIN,
                         [PhD_MIN] = @PhD_MIN,
                         [SemestersPerYear] = @SemestersPerYear,
                         [MonthsPerSemester] = @MonthsPerSemester";

            string condition = "[InsID] = @InsID";

            string updateSql = $"UPDATE {tableName} SET {setValues} WHERE {condition}";

            // The parameter array
            string[] parameterNames = {
        "@DiplomaDegreeMIN", "@DiplomaMIN",
        "@BSC_DegreeMIN", "@BSC_MIN",
        "@HigherDiplomaDegreeMIN", "@HigherDiplomaMIN",
        "@MasterDegreeMIN", "@MasterMIN",
        "@PhD_DegreeMIN", "@PhD_MIN",
        "@SemestersPerYear", "@MonthsPerSemester",
        "@InsID"
    };

            object[] values = {
        studyDuration.DiplomaDegreeMIN ?? (object)DBNull.Value,
        studyDuration.DiplomaMIN ?? (object)DBNull.Value,
        studyDuration.BSC_DegreeMIN ?? (object)DBNull.Value,
        studyDuration.BSC_MIN ?? (object)DBNull.Value,
        studyDuration.HigherDiplomaDegreeMIN ?? (object)DBNull.Value,
        studyDuration.HigherDiplomaMIN ?? (object)DBNull.Value,
        studyDuration.MasterDegreeMIN ?? (object)DBNull.Value,
        studyDuration.MasterMIN ?? (object)DBNull.Value,
        studyDuration.PhD_DegreeMIN ?? (object)DBNull.Value,
        studyDuration.PhD_MIN ?? (object)DBNull.Value,
        studyDuration.SemestersPerYear.HasValue ? studyDuration.SemestersPerYear.Value : (object)DBNull.Value,
        studyDuration.MonthsPerSemester.HasValue ? studyDuration.MonthsPerSemester.Value : (object)DBNull.Value,
        studyDuration.InsID
    };

            // Do the update
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(updateSql, conn))
            {
                conn.Open();
                for (int i = 0; i < parameterNames.Length; i++)
                {
                    cmd.Parameters.AddWithValue(parameterNames[i], values[i]);
                }
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteStudyDuration(int insID)
        {
            string tableName = "StudyDuration";

            // Condition for which record to delete (based on InsID)
            string condition = "[InsID] = @InsID";

            // Call the DeleteRecord method of the DatabaseHandler
           // DeleteRecord(tableName, condition);
        }
        public List<StudyDuration> GetAllStudyDurations()
        {
            string query = @"
        SELECT
            [InsID],
            [DiplomaDegreeMIN], [DiplomaMIN],
            [BSC_DegreeMIN], [BSC_MIN],
            [HigherDiplomaDegreeMIN], [HigherDiplomaMIN],
            [MasterDegreeMIN], [MasterMIN],
            [PhD_DegreeMIN], [PhD_MIN],
            [SemestersPerYear], [MonthsPerSemester]
        FROM StudyDuration";

            DataTable resultTable = SelectRecords(query);
            List<StudyDuration> studyDurationList = new List<StudyDuration>();

            foreach (DataRow row in resultTable.Rows)
            {
                // We handle possible NULLs => (int?)null
                int insID = Convert.ToInt32(row["InsID"]);

                string dipDegMin = row["DiplomaDegreeMIN"]?.ToString() ?? "";
                string dipMin = row["DiplomaMIN"]?.ToString() ?? "";
                string bscDegMin = row["BSC_DegreeMIN"]?.ToString() ?? "";
                string bscMin = row["BSC_MIN"]?.ToString() ?? "";
                string hiDipDegMin = row["HigherDiplomaDegreeMIN"]?.ToString() ?? "";
                string hiDipMin = row["HigherDiplomaMIN"]?.ToString() ?? "";
                string masDegMin = row["MasterDegreeMIN"]?.ToString() ?? "";
                string masMin = row["MasterMIN"]?.ToString() ?? "";
                string phdDegMin = row["PhD_DegreeMIN"]?.ToString() ?? "";
                string phdMin = row["PhD_MIN"]?.ToString() ?? "";

                // new fields
                int? semesters = row["SemestersPerYear"] != DBNull.Value ? Convert.ToInt32(row["SemestersPerYear"]) : (int?)null;
                int? months = row["MonthsPerSemester"] != DBNull.Value ? Convert.ToInt32(row["MonthsPerSemester"]) : (int?)null;

                StudyDuration sd = new StudyDuration(
                    insID,
                    dipDegMin, dipMin,
                    bscDegMin, bscMin,
                    hiDipDegMin, hiDipMin,
                    masDegMin, masMin,
                    phdDegMin, phdMin,
                    semesters,
                    months
                );
                studyDurationList.Add(sd);
            }
            return studyDurationList;
        }


        public StudyDuration GetStudyDurationByInsID(int insid)
        {
            string query = @"
        SELECT
            [InsID],
            [DiplomaDegreeMIN], [DiplomaMIN],
            [BSC_DegreeMIN], [BSC_MIN],
            [HigherDiplomaDegreeMIN], [HigherDiplomaMIN],
            [MasterDegreeMIN], [MasterMIN],
            [PhD_DegreeMIN], [PhD_MIN],
            [SemestersPerYear], [MonthsPerSemester]
        FROM [StudyDuration]
        WHERE [InsID] = @InsID;";

            DataTable dt = SelectRecords(query, new[] { "@InsID" }, new object[] { insid });

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            int retrievedInsID = Convert.ToInt32(row["InsID"]);

            // old fields
            string diplomaDegreeMin = row["DiplomaDegreeMIN"]?.ToString() ?? "";
            string diplomaMin = row["DiplomaMIN"]?.ToString() ?? "";
            string bscDegreeMin = row["BSC_DegreeMIN"]?.ToString() ?? "";
            string bscMin = row["BSC_MIN"]?.ToString() ?? "";
            string hiDipDegreeMin = row["HigherDiplomaDegreeMIN"]?.ToString() ?? "";
            string hiDipMin = row["HigherDiplomaMIN"]?.ToString() ?? "";
            string masterDegMin = row["MasterDegreeMIN"]?.ToString() ?? "";
            string masterMin = row["MasterMIN"]?.ToString() ?? "";
            string phdDegMin = row["PhD_DegreeMIN"]?.ToString() ?? "";
            string phdMin = row["PhD_MIN"]?.ToString() ?? "";

            // new fields
            int? semesters = row["SemestersPerYear"] != DBNull.Value ? Convert.ToInt32(row["SemestersPerYear"]) : (int?)null;
            int? months = row["MonthsPerSemester"] != DBNull.Value ? Convert.ToInt32(row["MonthsPerSemester"]) : (int?)null;

            return new StudyDuration(
                retrievedInsID,
                diplomaDegreeMin, diplomaMin,
                bscDegreeMin, bscMin,
                hiDipDegreeMin, hiDipMin,
                masterDegMin, masterMin,
                phdDegMin, phdMin,
                semesters,
                months
            );
        }

        public void InsertUser(Users user)
        {
            string tableName = "Users";
            string columns = "[InsName], [InsCountry], [Email], [Password], [VerificationCode], [Verified], [Clearance], [Speciality]";

            string[] parameterNames = {
                "@InsName", "@InsCountry", "@Email", "@Password", "@VerificationCode", "@Verified", "@Clearance", "@Speciality"
            };

            object[] values = {
                user.InsName ?? (object)DBNull.Value,
                user.InsCountry ?? (object)DBNull.Value,
                user.Email ?? (object)DBNull.Value,
                user.Password ?? (object)DBNull.Value,
                user.VerificationCode ?? (object)DBNull.Value,
                user.Verified ?? (object)DBNull.Value,
                user.Clearance,
                user.Speciality ?? (object)DBNull.Value
            };

            // Call the InsertRecord method of the DatabaseHandler
            InsertRecord(tableName, columns, parameterNames, values);
        }
        public void UpdateUser(Users user)
        {
            string tableName = "Users";

            // Columns to update
            string setValues = "[InsName] = @InsName, [InsCountry] = @InsCountry, [Email] = @Email, [Password] = @Password, " +
                               "[VerificationCode] = @VerificationCode, [Verified] = @Verified, [Clearance] = @Clearance, [Speciality] = @Speciality";

            // Condition for which record to update (based on InsID)
            string condition = "[InsID] = @InsID";

            string[] parameterNames = {
        "@InsName", "@InsCountry", "@Email", "@Password", "@VerificationCode", "@Verified", "@Clearance", "@Speciality", "@InsID"
    };

            object[] values = {
        user.InsName ?? (object)DBNull.Value,
        user.InsCountry ?? (object)DBNull.Value,
        user.Email ?? (object)DBNull.Value,
        user.Password ?? (object)DBNull.Value,
        user.VerificationCode ?? (object)DBNull.Value,
        user.Verified ?? (object)DBNull.Value,
        user.Clearance,
        user.Speciality ?? (object)DBNull.Value,
        user.InsID
    };

            // Call the UpdateRecord method of the DatabaseHandler
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }

        public void DeleteUser(int insID)
        {
            string tableName = "Users";

            // Condition for which record to delete (based on InsID)
            string condition = "[InsID] = @InsID";

            // Call the DeleteRecord method of the DatabaseHandler
           // DeleteRecord(tableName, condition);
        }
        public List<Users> GetAllUsers()
        {
            string query = "SELECT [InsID], [InsName], [InsCountry], [Email], [Password], [VerificationCode], [Verified], [Clearance], [Speciality] FROM Users";
            DataTable resultTable = SelectRecords(query);

            List<Users> usersList = new();

            foreach (DataRow row in resultTable.Rows)
            {
                int insID = Convert.ToInt32(row["InsID"]);
                string insName = row["InsName"]?.ToString() ?? string.Empty;
                string insCountry = row["InsCountry"]?.ToString() ?? string.Empty;
                string email = row["Email"]?.ToString() ?? string.Empty;
                string password = row["Password"]?.ToString() ?? string.Empty;
                string verificationCode = row["VerificationCode"]?.ToString() ?? string.Empty;
                int? verified = row["Verified"] != DBNull.Value ? Convert.ToInt32(row["Verified"]) : (int?)null;
                int clearance = row["Clearance"] != DBNull.Value ? Convert.ToInt32(row["Clearance"]) : 0;
                string speciality = row["Speciality"]?.ToString() ?? string.Empty;

                usersList.Add(new Users(insID, insName, insCountry, email, password, verificationCode, verified, clearance, speciality));
            }

            return usersList;
        }
        //other ways of doing acceptancerecord.

        public void InsertAcceptanceRecord(AcceptanceRecord record)
        {
            string tableName = "AcceptanceRecord";
            string columns = "[IsAccepted], [Date], [Reason]";

            // Convert the list of reasons to a JSON string
            string reasonJson = JsonConvert.SerializeObject(record.Reason);

            string[] parameterNames = { "@IsAccepted", "@Date", "@Reason" };
            object[] values = { record.IsAccepted, record.Date.ToString("yyyy-MM-dd"), reasonJson };

            // Call the InsertRecord method of the DatabaseHandler
            InsertRecord(tableName, columns, parameterNames, values);
        }
        public void UpdateAcceptanceRecord(AcceptanceRecord record, int recordId)
        {
            string tableName = "AcceptanceRecord";

            // Columns to update
            string setValues = "[IsAccepted] = @IsAccepted, [Date] = @Date, [Reason] = @Reason";

            // Condition for which record to update (assuming the table has an ID column)
            string condition = "[RecordID] = @RecordID";

            // Convert the list of reasons to a JSON string
            string reasonJson = JsonConvert.SerializeObject(record.Reason);

            string[] parameterNames = { "@IsAccepted", "@Date", "@Reason", "@RecordID" };
            object[] values = { record.IsAccepted, record.Date.ToString("yyyy-MM-dd"), reasonJson, recordId };

            // Call the UpdateRecord method of the DatabaseHandler
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }
        public void DeleteAcceptanceRecord(int recordId)
        {
            string tableName = "AcceptanceRecord";
            string condition = "[RecordID] = @RecordID";

            // Call the DeleteRecord method of the DatabaseHandler
            // DeleteRecord(tableName, condition);
        }
        public List<AcceptanceRecord> GetAllAcceptanceRecords()
        {
            List<AcceptanceRecord> results = new List<AcceptanceRecord>();
            string selectSql = "SELECT RecordID, DateOfRecord, Result, Message FROM AcceptanceRecord ORDER BY RecordID ASC;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(selectSql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int recordId = reader.GetInt32(0);
                        DateTime dt = reader.GetDateTime(1);
                        string result = reader.GetString(2);
                        string message = reader.GetString(3);

                        bool isAccepted = result.Equals("Accepted", StringComparison.OrdinalIgnoreCase);
                        DateOnly dateOfRecord = DateOnly.FromDateTime(dt);
                        List<string> reasons = JsonConvert.DeserializeObject<List<string>>(message);

                        results.Add(new AcceptanceRecord(isAccepted, dateOfRecord, reasons, recordId));
                    }
                }
            }

            return results;
        }

        //---------------------------------------
        // 1) InsertAcceptanceRecord
        //---------------------------------------
        public void InsertAcceptanceRecord(int insId, AcceptanceRecord record)
        {
            Console.WriteLine($"[InsertAcceptanceRecord] Called with InsID={insId}, IsAccepted={record.IsAccepted}, Date={record.Date}, ReasonCount={record.Reason.Count}");

            // A) Determine the next RecordNo for this InsID
            int nextRecordNo = 1; // default to 1 if none found
            string countQuery = "SELECT COUNT(*) FROM AcceptanceRecord WHERE InsID=@InsID";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(countQuery, conn))
            {
                cmd.Parameters.AddWithValue("@InsID", insId);
                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                nextRecordNo = count + 1;
            }
            Console.WriteLine($"[InsertAcceptanceRecord] Next recordNo for InsID={insId} is {nextRecordNo}.");

            // B) Convert boolean -> "Accepted" / "Rejected"
            string resultString = record.IsAccepted ? "Accepted" : "Rejected";
            // C) Convert reasons to JSON
            string reasonJson = JsonConvert.SerializeObject(record.Reason);
            // D) Insert the new row
            string insertSql = @"
                INSERT INTO AcceptanceRecord
                    (InsID, RecordNo, DateOfRecord, Result, Message)
                VALUES
                    (@InsID, @RecordNo, @DateOfRecord, @Result, @Message);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(insertSql, conn))
            {
                cmd.Parameters.AddWithValue("@InsID", insId);
                cmd.Parameters.AddWithValue("@RecordNo", nextRecordNo);
                cmd.Parameters.AddWithValue("@DateOfRecord", record.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Result", resultString);
                cmd.Parameters.AddWithValue("@Message", reasonJson);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            // E) Also update AcademicInfo.Accepted
            //    1 means "true", 0 means "false"
            //UpdateAcademicAccepted(insId, record.IsAccepted);
            //Console.WriteLine($"[InsertAcceptanceRecord] Insert + academic updated done for InsID={insId}.");
        }

        //---------------------------------------
        // 2) GetAllAcceptanceRecords
        //---------------------------------------
        public List<AcceptanceRecord> GetAllAcceptanceRecordsByInsId(int insId)
        {
            Console.WriteLine($"[GetAllAcceptanceRecords] Called with InsID={insId}.");
            List<AcceptanceRecord> results = new List<AcceptanceRecord>();

            // Update the query to select RecordID
            string selectSql = @"
          SELECT RecordID, DateOfRecord, Result, Message
          FROM AcceptanceRecord
          WHERE InsID=@InsID
          ORDER BY RecordNo ASC;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(selectSql, conn))
            {
                cmd.Parameters.AddWithValue("@InsID", insId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int recordID = reader.GetInt32(0); // Get the RecordID from the first column
                        DateTime dt = reader.GetDateTime(1);
                        string result = reader.GetString(2);
                        string message = reader.GetString(3);

                        bool isAccepted = result.Equals("Accepted", StringComparison.OrdinalIgnoreCase);
                        DateOnly dateOfRecord = DateOnly.FromDateTime(dt);

                        // Convert the JSON message back to a list of strings.
                        List<string> reasons = JsonConvert.DeserializeObject<List<string>>(message);

                        // Create the AcceptanceRecord with RecordID set.
                        AcceptanceRecord rec = new AcceptanceRecord(isAccepted, dateOfRecord, reasons, recordID);
                        results.Add(rec);
                    }
                }
            }

            Console.WriteLine($"[GetAllAcceptanceRecords] Found {results.Count} record(s) for InsID={insId}.");
            return results;
        }

        //---------------------------------------
        // 3) UpdateAcceptanceRecord (Optional)
        //---------------------------------------
        public void UpdateAcceptanceRecord(int insId, int recordNo, AcceptanceRecord record)
        {
            Console.WriteLine($"[UpdateAcceptanceRecord] Called with InsID={insId}, recordNo={recordNo}.");

            string updateSql = @"
                UPDATE AcceptanceRecord
                SET DateOfRecord=@DateOfRecord, Result=@Result, Message=@Message
                WHERE InsID=@InsID AND RecordNo=@RecordNo;";

            string resultString = record.IsAccepted ? "Accepted" : "Rejected";
            string reasonJson = JsonConvert.SerializeObject(record.Reason);

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(updateSql, conn))
            {
                cmd.Parameters.AddWithValue("@InsID", insId);
                cmd.Parameters.AddWithValue("@RecordNo", recordNo);
                cmd.Parameters.AddWithValue("@DateOfRecord", record.Date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@Result", resultString);
                cmd.Parameters.AddWithValue("@Message", reasonJson);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                Console.WriteLine($"[UpdateAcceptanceRecord] Affected rows = {rows} for InsID={insId}, recordNo={recordNo}.");
            }

            // Also update AcademicInfo.Accepted
            //UpdateAcademicAccepted(insId, record.IsAccepted);
        }

        //---------------------------------------
        // 4) UpdateAcademicAccepted
        //---------------------------------------
        public void UpdateAcademicAccepted(int insId, bool isAccepted)
        {
            Console.WriteLine($"[UpdateAcademicAccepted] Setting AcademicInfo.Accepted = {isAccepted} for InsID={insId}.");

            string updateSql = @"
                UPDATE AcademicInfo
                SET Accepted=@AcceptedValue
                WHERE InsID=@InsID;";

            int acceptedVal = isAccepted ? 1 : 0;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(updateSql, conn))
            {
                cmd.Parameters.AddWithValue("@InsID", insId);
                cmd.Parameters.AddWithValue("@AcceptedValue", acceptedVal);
                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                Console.WriteLine($"[UpdateAcademicAccepted] Done. Rows affected={rows} for InsID={insId}.");
            }
        }


        // In DatabaseHandler.cs

        public void InsertSupervisor(Supervisor supervisor)
        {
            string tableName = "Supervisor";
            string columns = "[Name], [PhoneNumber], [Email], [Password], [Speciality]";

            // We do NOT insert SupervisorID because it's an IDENTITY
            string[] parameterNames = { "@Name", "@PhoneNumber", "@Email", "@Password", "@Speciality" };
            object[] values = {
        supervisor.Name,
        supervisor.PhoneNumber,
        supervisor.Email,
        supervisor.Password,
        supervisor.Speciality
    };

            InsertRecord(tableName, columns, parameterNames, values);
        }

        // Optionally, get a single supervisor by ID
        public Supervisor GetSupervisorById(int supervisorID)
        {
            string query = "SELECT * FROM Supervisor WHERE [SupervisorID] = @SupervisorID";
            string[] paramNames = { "@SupervisorID" };
            object[] paramValues = { supervisorID };

            DataTable dt = SelectRecords(query, paramNames, paramValues);
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new Supervisor(
                supervisorID: Convert.ToInt32(row["SupervisorID"]),
                name: row["Name"].ToString(),
                phoneNumber: row["PhoneNumber"].ToString(),
                email: row["Email"].ToString(),
                password: row["Password"].ToString(),
                speciality: row["Speciality"].ToString()
            );
        }

        // Get all supervisors
        public List<Supervisor> GetAllSupervisors()
        {
            string query = "SELECT [SupervisorID], [Name], [PhoneNumber], [Email], [Password], [Speciality] FROM Supervisor";
            DataTable dt = SelectRecords(query);

            List<Supervisor> supervisors = new List<Supervisor>();
            foreach (DataRow row in dt.Rows)
            {
                int supID = Convert.ToInt32(row["SupervisorID"]);
                string name = row["Name"].ToString();
                string phone = row["PhoneNumber"].ToString();
                string email = row["Email"].ToString();
                string pwd = row["Password"].ToString();
                string spec = row["Speciality"].ToString();

                Supervisor sup = new Supervisor(supID, name, phone, email, pwd, spec);
                supervisors.Add(sup);
            }
            return supervisors;
        }

        // Optionally update a supervisor
        public void UpdateSupervisor(Supervisor supervisor)
        {
            string tableName = "Supervisor";
            // We'll update all fields except the PK
            string setValues = "[Name] = @Name, [PhoneNumber] = @PhoneNumber, [Email] = @Email, [Password] = @Password, [Speciality] = @Speciality";
            string condition = "[SupervisorID] = @SupervisorID";

            string[] paramNames = { "@Name", "@PhoneNumber", "@Email", "@Password", "@Speciality", "@SupervisorID" };
            object[] values = {
        supervisor.Name,
        supervisor.PhoneNumber,
        supervisor.Email,
        supervisor.Password,
        supervisor.Speciality,
        supervisor.SupervisorID
    };

            UpdateRecord(tableName, setValues, condition, paramNames, values);
        }

        // Optionally delete a supervisor
        public void DeleteSupervisor(int supervisorID)
        {
            // Build a dictionary for the WHERE conditions
            var parameters = new Dictionary<string, object>
    {
        { "SupervisorID", supervisorID }
    };

            // Use your existing DeleteRecord helper
            int rowsDeleted = DeleteRecord("Supervisor", parameters);

            Console.WriteLine($"[DeleteSupervisor] Deleted {rowsDeleted} row(s) for SupervisorID={supervisorID}");
        }

        public void InsertSubmissionRecord(int insID, string applicantName, string workPlace, string email, int supervisorID)
        {
            // We'll store the current date/time
            DateTime now = DateTime.Now;

            string tableName = "SubmissionRequests";
            string columns = "[InsID], [ApplicantName], [WorkPlace], [Email], [SubmissionDate], [AssignedSupervisorID]";

            string[] parameterNames = {
        "@InsID", "@ApplicantName", "@WorkPlace", "@Email", "@SubmissionDate", "@AssignedSupervisorID"
    };

            object[] values = {
        insID,
        applicantName,
        workPlace,
        email,
        now,
        supervisorID
    };

            InsertRecord(tableName, columns, parameterNames, values);
        }
        public int CountInstitutionsAssignedToSupervisor(int supervisorID)
        {
            // We assume "PublicInfo" table has [SupervisorID].
            string query = @"SELECT COUNT(*) FROM PublicInfo WHERE SupervisorID = @SupID";
            string[] paramNames = { "@SupID" };
            object[] paramValues = { supervisorID };

            DataTable dt = SelectRecords(query, paramNames, paramValues);
            if (dt.Rows.Count == 0) return 0;

            // dt.Rows[0][0] => first cell = count
            return Convert.ToInt32(dt.Rows[0][0]);
        }
        public void InsertIntoQueue(int insID, string institutionName, string specialization)
        {
            string query = "INSERT INTO ApplicationQueue (InsID, InstitutionName, Specialization) VALUES (@InsID, @InstitutionName, @Specialization)";
            string[] paramNames = { "@InsID", "@InstitutionName", "@Specialization" };
            object[] paramValues = { insID, institutionName, specialization };

            InsertRecord("ApplicationQueue", "InsID, InstitutionName, Specialization", paramNames, paramValues);
        }

        public int CountPendingApplications()
        {
            string query = "SELECT COUNT(*) FROM ApplicationQueue WHERE Processed = 0";
            return Convert.ToInt32(SelectRecords(query).Rows[0][0]);
        }

        public List<ApplicationQueueItem> GetPendingApplications()
        {
            string query = "SELECT * FROM ApplicationQueue WHERE Processed = 0";
            DataTable table = SelectRecords(query);
            List<ApplicationQueueItem> queue = new();

            foreach (DataRow row in table.Rows)
            {
                queue.Add(new ApplicationQueueItem
                {
                    QueueID = Convert.ToInt32(row["QueueID"]),
                    InsID = Convert.ToInt32(row["InsID"]),
                    InstitutionName = row["InstitutionName"].ToString(),
                    Specialization = row["Specialization"].ToString(),
                    SubmissionDate = Convert.ToDateTime(row["SubmissionDate"])
                });
            }
            return queue;
        }

        public void CreateMeeting(string meetingNumber)
        {
            string query = "INSERT INTO Meetings (MeetingNumber) VALUES (@MeetingNumber)";
            string[] paramNames = { "@MeetingNumber" };
            object[] paramValues = { meetingNumber };
            InsertRecord("Meetings", "MeetingNumber", paramNames, paramValues);
        }

        public int GetNextMeetingNumber()
        {
            string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(MeetingNumber, 1, CHARINDEX('/', MeetingNumber) - 1) AS INT)), 0) FROM Meetings WHERE YEAR(CreationDate) = YEAR(GETDATE())";
            return Convert.ToInt32(SelectRecords(query).Rows[0][0]) + 1;
        }


        public void MarkAsProcessed(int queueID)
        {
            string query = "UPDATE ApplicationQueue SET Processed = 1 WHERE QueueID = @QueueID";
            string[] paramNames = { "@QueueID" };
            object[] paramValues = { queueID };
            UpdateRecord("ApplicationQueue", "Processed = 1", "QueueID = @QueueID", paramNames, paramValues);
        }
        public void MarkLatestAsUnprocessed(int insID)
        {
            // This query updates the ApplicationQueue row that is the latest for the given InsID.
            // It uses a subquery to select the TOP 1 QueueID ordered by SubmissionDate descending.
            string setClause = "Processed = 0";
            string whereClause = @"QueueID = (
                                SELECT TOP 1 QueueID 
                                FROM ApplicationQueue 
                                WHERE InsID = @InsID 
                                ORDER BY SubmissionDate DESC
                            )";

            // Parameter(s) for the query
            string[] paramNames = { "@InsID" };
            object[] paramValues = { insID };

            // Call your existing helper method to run the update.
            UpdateRecord("ApplicationQueue", setClause, whereClause, paramNames, paramValues);
        }

        public void AssignInstitutionToSupervisor(int insID, int supervisorID)
        {
            string query = @"
        UPDATE PublicInfo 
        SET SupervisorID = @SupervisorID, 
            SupervisorName = (SELECT Name FROM Supervisor WHERE SupervisorID = @SupervisorID)
        WHERE InsID = @InsID";

            string[] paramNames = { "@SupervisorID", "@InsID" };
            object[] paramValues = { supervisorID, insID };

            UpdateRecord("PublicInfo", "SupervisorID = @SupervisorID, Supervisor = (SELECT Name FROM Supervisor WHERE SupervisorID = @SupervisorID)", "InsID = @InsID", paramNames, paramValues);
        }

        public void CreateMeetingEntry(string meetingID, int insID, string institutionName, string specialization, DateTime submissionDate)
        {
            // ✅ Ensure we don't insert duplicate meetings for the same InsID (NO REVERSE THIS )
            string checkQuery = "SELECT COUNT(*) FROM Meetings WHERE MeetingNumber = @MeetingNumber AND InsID = @InsID";
            string[] checkParams = { "@MeetingNumber", "@InsID" };
            object[] checkValues = { meetingID, insID };

            int count = Convert.ToInt32(SelectRecords(checkQuery, checkParams, checkValues).Rows[0][0]);
            if (count > 0)
            {
                Console.WriteLine($"[CreateMeetingEntry] Skipping duplicate entry for MeetingID={meetingID}, InsID={insID}");
                return; // Avoid duplicate entry
            }

            string supervisorQuery = "SELECT SupervisorID FROM PublicInfo WHERE InsID = @InsID";
            string[] supervisorParam = { "@InsID" };
            object[] supervisorValue = { insID };

            DataTable result = SelectRecords(supervisorQuery, supervisorParam, supervisorValue);

            int? supervisorID = (result.Rows.Count > 0 && result.Rows[0]["SupervisorID"] != DBNull.Value)
                ? Convert.ToInt32(result.Rows[0]["SupervisorID"])
                : (int?)null;

            string query = @"
INSERT INTO Meetings (MeetingNumber, CreationDate, Status, InsID, InstitutionName, Specialization, SubmissionDate, SupervisorID)
VALUES (@MeetingNumber, @CreationDate, @Status, @InsID, @InstitutionName, @Specialization, @SubmissionDate, @SupervisorID)";

            string[] paramNames = { "@MeetingNumber", "@CreationDate", "@Status", "@InsID", "@InstitutionName", "@Specialization", "@SubmissionDate", "@SupervisorID" };
            object[] paramValues = {
    meetingID,
    DateTime.Now,
    "Scheduled",
    insID,
    institutionName,
    specialization,
    submissionDate,
    supervisorID.HasValue ? supervisorID.Value : (object)DBNull.Value  // Fixed line
};

            InsertRecord("Meetings", "MeetingNumber, CreationDate, Status, InsID, InstitutionName, Specialization, SubmissionDate, SupervisorID", paramNames, paramValues);
        }






        public List<MeetingApplication> GetMeetingApplications()
        {
            string query = @"
    SELECT 
        m.MeetingID, 
        m.MeetingNumber, 
        m.InsID, 
        u.InsName AS InstitutionName, 
        m.Specialization, 
        m.SubmissionDate, 
        m.SupervisorID,
        s.Name AS SupervisorName 
    FROM Meetings m
    LEFT JOIN USERS u ON m.InsID = u.InsID
    LEFT JOIN Supervisor s ON m.SupervisorID = s.SupervisorID
    ORDER BY m.MeetingID DESC, m.SubmissionDate ASC";

            DataTable table = SelectRecords(query);
            List<MeetingApplication> meetings = new();

            foreach (DataRow row in table.Rows)
            {
                meetings.Add(new MeetingApplication
                {
                    MeetingID = row["MeetingID"] != DBNull.Value ? Convert.ToInt32(row["MeetingID"]) : 0,
                    MeetingNumber = row["MeetingNumber"].ToString(),
                    InsID = Convert.ToInt32(row["InsID"]),
                    InstitutionName = row["InstitutionName"].ToString(),
                    Specialization = row["Specialization"].ToString(),
                    SubmissionDate = Convert.ToDateTime(row["SubmissionDate"]).ToString("yyyy-MM-dd"),
                    SupervisorID = row["SupervisorID"] != DBNull.Value ? Convert.ToInt32(row["SupervisorID"]) : 0,
                    SupervisorName = row["SupervisorName"]?.ToString() ?? "Not Assigned"
                });
            }

            return meetings;
        }

        public void UpdateMeetingSupervisor(int meetingId, int supervisorID)
        {
            string query = "UPDATE Meetings SET SupervisorID = @SupervisorID WHERE MeetingID = @MeetingID";
            string[] paramNames = { "@SupervisorID", "@MeetingID" };
            object[] paramValues = { supervisorID, meetingId };

            UpdateRecord("Meetings", "SupervisorID = @SupervisorID", "MeetingID = @MeetingID", paramNames, paramValues);
        }



        public Users GetUserByInsID(int insID)
        {
            string query = "SELECT * FROM Users WHERE InsID = @InsID";
            string[] paramNames = { "@InsID" };
            object[] paramValues = { insID };

            DataTable table = SelectRecords(query, paramNames, paramValues);

            if (table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];
            return new Users
            {
                InsID = Convert.ToInt32(row["InsID"]),
                InsName = row["InsName"].ToString(),
                Speciality = row["Speciality"].ToString()
            };
        }
        public void ClearProcessedQueue()
        {
            string query = "DELETE FROM ApplicationQueue WHERE Processed = 1";
            Query(query);
        }
        public void MarkQueueAsProcessed()
        {
            string query = "UPDATE ApplicationQueue SET Processed = 1 WHERE Processed = 0";
            Query(query);
        }


        public void UpdateSupervisorNameInPublicInfo(int insID, int supervisorID, string supervisorName)
        {
            string query = "UPDATE PublicInfo SET SupervisorID = @SupervisorID, Supervisor = @Supervisor WHERE InsID = @InsID";
            string[] paramNames = { "@SupervisorID", "@Supervisor", "@InsID" };
            object[] paramValues = { supervisorID, supervisorName, insID };

            UpdateRecord("PublicInfo", "SupervisorID = @SupervisorID, Supervisor = @Supervisor", "InsID = @InsID", paramNames, paramValues);
        }

        public List<MeetingApplication> GetMeetingByNumber(string meetingNumber)
        {
            string query = @"
    SELECT 
        m.MeetingID,
        m.CreationDate,
        m.MeetingNumber,
        m.InsID,
        u.InsName AS InstitutionName,
        m.Specialization,
        m.SubmissionDate,
        s.Name AS SupervisorName
    FROM Meetings m
    LEFT JOIN USERS u ON m.InsID = u.InsID
    LEFT JOIN Supervisor s ON m.SupervisorID = s.SupervisorID
    WHERE m.MeetingNumber = @MeetingNumber
    ORDER BY m.SubmissionDate ASC";

            string[] paramNames = { "@MeetingNumber" };
            object[] paramValues = { meetingNumber };

            DataTable table = SelectRecords(query, paramNames, paramValues);
            List<MeetingApplication> meetings = new();

            foreach (DataRow row in table.Rows)
            {
                meetings.Add(new MeetingApplication
                {
                    MeetingID = Convert.ToInt32(row["MeetingID"]),
                    CreationDate = Convert.ToDateTime(row["CreationDate"]).ToString("yyyy-MM-dd"),
                    MeetingNumber = row["MeetingNumber"].ToString(),
                    InsID = Convert.ToInt32(row["InsID"]),
                    InstitutionName = row["InstitutionName"].ToString(),
                    Specialization = row["Specialization"].ToString(),
                    SubmissionDate = Convert.ToDateTime(row["SubmissionDate"]).ToString("yyyy-MM-dd"),
                    SupervisorName = row["SupervisorName"]?.ToString() ?? "Not Assigned"
                });
            }

            return meetings;
        }

        public List<MeetingInfo> GetAllMeetingNumbers()
        {
            string query = @"
        SELECT MeetingNumber, CreationDate 
        FROM Meetings 
        WHERE CreationDate = (
            SELECT MAX(CreationDate) 
            FROM Meetings AS sub 
            WHERE sub.MeetingNumber = Meetings.MeetingNumber
        ) 
        ORDER BY MeetingNumber DESC";
            DataTable table = SelectRecords(query);
            List<MeetingInfo> meetingList = new();

            foreach (DataRow row in table.Rows)
            {
                meetingList.Add(new MeetingInfo
                {
                    MeetingNumber = row["MeetingNumber"].ToString(),
                    CreationDate = Convert.ToDateTime(row["CreationDate"])
                });
            }

            return meetingList;
        }

        public void InsertFaculty(Faculty faculty)
        {
            string tableName = "Faculty";
            string columns = "[InsID], [FacultyName], [CreateDate], [LastGraduteDate],[NumPrograms],[NumPgPrograms ]";

            string[] parameterNames = {
     "@InsID", "@FacultyName", "@CreateDate", "@LastGraduteDate","@NumPrograms","@NumPgPrograms"
 };

            object[] values = {
     faculty.InsID, faculty.FacultyName, faculty.CreateDate, faculty.LastGraduateDate ,faculty.NumPrograms ,faculty.NumPgPrograms
 };

            // Call the InsertRecord method of the DatabaseHandler
            InsertRecord(tableName, columns, parameterNames, values);
        }
        public List<Faculty> GetAllFacultiesByInsID(int insId)
        {
            List<Faculty> faculties = new List<Faculty>();

            string sql = "SELECT * FROM Faculty WHERE InsID = @InsID";
            string[] paramNames = { "@InsID" };
            object[] values = { insId };

            DataTable dt = SelectRecords(sql, paramNames, values);

            foreach (DataRow row in dt.Rows)
            {

                Faculty faculty = new Faculty(
                    insID: Convert.ToInt32(row["InsID"]),
                    facultyName: row["FacultyName"].ToString(),
                    createDate: DateOnly.FromDateTime(Convert.ToDateTime(row["CreateDate"])),
                    lastGraduateDate: DateOnly.FromDateTime(Convert.ToDateTime(row["LastGraduteDate"])),
                    numPrograms: row["NumPrograms"] != DBNull.Value ? Convert.ToInt32(row["NumPrograms"]) : 0,
                    numPgPrograms: row["NumPgPrograms"] != DBNull.Value ? Convert.ToInt32(row["NumPgPrograms"]) : 0
                );

                faculties.Add(faculty);
            }

            return faculties;
        }



        public List<Specialization> GetAllSpecializationsByInsID(int insId)
        {
            List<Specialization> specializations = new List<Specialization>();

            string sql = "SELECT * FROM Specializations WHERE InsID = @InsID";
            string[] paramNames = { "@InsID" };
            object[] values = { insId };

            DataTable dt = SelectRecords(sql, paramNames, values);

            foreach (DataRow row in dt.Rows)
            {
                // Match the constructor parameter order *exactly*:
                // Specialization(
                //    int insId,
                //    string type,
                //    int numStu,
                //    int numProf,
                //    int numAssociative,
                //    int numAssistant,
                //    int? numProfPractice,
                //    int numberLecturers,
                //    int numAssisLecturer,
                //    int numOtherTeachers,
                //    int color,
                //    double ratio,
                //    int numPhdHolders,
                //    int? practicalHours,
                //    int? theoreticalHours
                // )

                Specialization sp = new Specialization(
                    insId: Convert.ToInt32(row["InsID"]),
                    type: row["Type"].ToString(),
                    numStu: Convert.ToInt32(row["NumStu"]),
                    numProf: Convert.ToInt32(row["NumProf"]),
                    numAssociative: Convert.ToInt32(row["NumAssociative"]),
                    numAssistant: Convert.ToInt32(row["NumAssistant"]),
                    numProfPractice: row["NumProfPractice"] != DBNull.Value
                        ? Convert.ToInt32(row["NumProfPractice"])
                        : (int?)null, // Must be (int?)null, not 0
                    numberLecturers: Convert.ToInt32(row["NumberLecturers"]),
                    numAssisLecturer: Convert.ToInt32(row["NumAssisLecturer"]),
                    numOtherTeachers: Convert.ToInt32(row["NumOtherTeachers"]),
                    color: Convert.ToInt32(row["Color"]),
                    ratio: Convert.ToDouble(row["Ratio"]),
                    numPhdHolders: Convert.ToInt32(row["NumPhdHolders"]),
                    practicalHours: row["PracticalHours"] != DBNull.Value
                        ? Convert.ToInt32(row["PracticalHours"])
                        : (int?)null,
                    theoreticalHours: row["TheoreticalHours"] != DBNull.Value
                        ? Convert.ToInt32(row["TheoreticalHours"])
                        : (int?)null
                );

                specializations.Add(sp);
            }

            return specializations;
        }
        public List<Hospitals> GetAllHospitalsByInsID(int insId)
        {
            List<Hospitals> hospitals = new List<Hospitals>();

            string sql = "SELECT * FROM Hospitals WHERE InsID = @InsID";
            string[] paramNames = { "@InsID" };
            object[] values = { insId };

            DataTable dt = SelectRecords(sql, paramNames, values);

            foreach (DataRow row in dt.Rows)
            {
                Hospitals hospital = new Hospitals(
                    insID: Convert.ToInt32(row["InsID"]),
                    hospType: row["HospType"].ToString(),
                    hospName: row["HospName"].ToString(),
                    hospMajor: row["HospMajor"].ToString()
                );

                hospitals.Add(hospital);
            }

            return hospitals;
        }
        public List<PublicInfo> GetPublicInfoBySupervisorID(int supervisorID)
        {
            List<PublicInfo> results = new List<PublicInfo>();

            string query = @"
        SELECT [InsID],
               [InsName],
               [Provider],
               [StartDate],
               [SDateT],
               [SDateNT],
               [SupervisorID],
               [Supervisor],
               [PreName],
               [PreDegree],
               [PreMajor],
               [Postal],
               [Phone],
               [Fax],
               [Email],
               [Website],
               [Vision],
               [Mission],
               [Goals],
               [InsValues],
               [LastEditDate],
               [EntryDate],
               [Country],
               [City],
               [Address],
               [CreationDate],
               [StudentAcceptanceDate],
               [OtherInfo]
        FROM PublicInfo
        WHERE [SupervisorID] = @SID";

            string[] parameterNames = { "@SID" };
            object[] values = { supervisorID };

            DataTable dt = SelectRecords(query, parameterNames, values);

            foreach (DataRow row in dt.Rows)
            {
                // Parse DateOnly fields
                DateOnly entryDate = default;
                if (DateTime.TryParse(row["EntryDate"]?.ToString(), out DateTime dtEntry))
                {
                    entryDate = DateOnly.FromDateTime(dtEntry);
                }

                DateOnly creationDate = default;
                if (DateTime.TryParse(row["CreationDate"]?.ToString(), out DateTime dtCreation))
                {
                    creationDate = DateOnly.FromDateTime(dtCreation);
                }

                DateOnly studentAcceptanceDate = default;
                if (DateTime.TryParse(row["StudentAcceptanceDate"]?.ToString(), out DateTime dtStudent))
                {
                    studentAcceptanceDate = DateOnly.FromDateTime(dtStudent);
                }

                PublicInfo pi = new PublicInfo(
                    insID: Convert.ToInt32(row["InsID"]),
                    insName: row["InsName"].ToString() ?? "",
                    provider: row["Provider"].ToString() ?? "",
                    startDate: row["StartDate"].ToString() ?? "",
                    sDateT: row["SDateT"].ToString() ?? "",
                    sDateNT: row["SDateNT"].ToString() ?? "",
                    supervisorID: row["SupervisorID"] != DBNull.Value ? Convert.ToInt32(row["SupervisorID"]) : (int?)null,
                    supervisor: row["Supervisor"]?.ToString() ?? "",
                    preName: row["PreName"]?.ToString() ?? "",
                    preDegree: row["PreDegree"]?.ToString() ?? "",
                    preMajor: row["PreMajor"]?.ToString() ?? "",
                    postal: row["Postal"]?.ToString() ?? "",
                    phone: row["Phone"]?.ToString() ?? "",
                    fax: row["Fax"]?.ToString() ?? "",
                    email: row["Email"]?.ToString() ?? "",
                    website: row["Website"]?.ToString() ?? "",
                    vision: row["Vision"]?.ToString() ?? "",
                    mission: row["Mission"]?.ToString() ?? "",
                    goals: row["Goals"]?.ToString() ?? "",
                    insValues: row["InsValues"]?.ToString() ?? "",
                    lastEditDate: row["LastEditDate"]?.ToString() ?? "",
                    entryDate: entryDate,
                    country: row["Country"]?.ToString() ?? "",
                    city: row["City"]?.ToString() ?? "",
                    address: row["Address"]?.ToString() ?? "",
                    creationDate: creationDate,
                    studentAcceptanceDate: studentAcceptanceDate,
                    otherInfo: row["OtherInfo"]?.ToString() ?? ""
                );

                results.Add(pi);
            }

            return results;
        }
        public void UpdateUserSpeciality(int insID, string speciality)
        {
            // Validate the inputs
            if (insID <= 0)
            {
                throw new ArgumentException("InsID must be a positive integer.", nameof(insID));
            }

            if (string.IsNullOrWhiteSpace(speciality))
            {
                throw new ArgumentException("Speciality cannot be null or empty.", nameof(speciality));
            }

            // Define the table and columns to update
            string tableName = "Users";
            string setValues = "[Speciality] = @Speciality";
            string condition = "[InsID] = @InsID";

            // Define parameter names and corresponding values
            string[] parameterNames = { "@Speciality", "@InsID" };
            object[] values = {
        speciality,
        insID
    };

            // Call the existing UpdateRecord method to perform the update
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }

        public void UpdateMeetingSupervisor(int insID, int oldSupervisorID, int newSupervisorID)
        {
            if (insID <= 0)
            {
                throw new ArgumentException("InsID must be a positive integer.", nameof(insID));
            }

            string tableName = "Meetings";
            string setValues = "[SupervisorID] = @NewSupervisorID";
            string condition = "[InsID] = @InsID AND [SupervisorID] = @OldSupervisorID";

            string[] parameterNames = { "@NewSupervisorID", "@InsID", "@OldSupervisorID" };
            object[] values = {
        newSupervisorID,
        insID,
        oldSupervisorID
    };

            // Call the existing UpdateRecord method to perform the update
            UpdateRecord(tableName, setValues, condition, parameterNames, values);
        }
        public List<InstitutionStatus> GetInstitutionStatuses()
        {
            string query = @"
SELECT 
    pi.InsID, 
    pi.InsName, 
    pi.Country, 
    pi.Supervisor,
    COALESCE(ar.Result, 'Not Processed') AS Status,  -- ignored in our logic
    COALESCE(ar.Message, '') AS Message,
    COALESCE(ar.decisionOutcome, '') AS decisionOutcome,
    m.MeetingNumber
FROM PublicInfo pi
LEFT JOIN (
    SELECT 
        InsID, 
        STRING_AGG(Result, '; ') AS Result, 
        STRING_AGG(Message, '; ') AS Message,
        STRING_AGG(decisionOutcome, '; ') AS decisionOutcome
    FROM AcceptanceRecord
    GROUP BY InsID
) ar ON pi.InsID = ar.InsID
LEFT JOIN Meetings m ON pi.InsID = m.InsID
";

            DataTable resultTable = SelectRecords(query);
            List<InstitutionStatus> institutionStatuses = new();

            // Helper to clean tokens from fallback strings.
            string CleanToken(string token)
            {
                return (token ?? "").Trim('[', ']', '"');
            }

            foreach (DataRow row in resultTable.Rows)
            {
                string supervisor = row["Supervisor"] != DBNull.Value ? row["Supervisor"].ToString() ?? "Not Assigned" : "Not Assigned";
                // We ignore aggregatedStatus (from the Result field)
                string aggregatedMessage = row["Message"] != DBNull.Value ? row["Message"].ToString() ?? string.Empty : string.Empty;
                string aggregatedDecisionOutcome = row["decisionOutcome"] != DBNull.Value ? row["decisionOutcome"].ToString() ?? string.Empty : string.Empty;
                string meetingNumber = row["MeetingNumber"] != DBNull.Value ? row["MeetingNumber"].ToString() ?? string.Empty : string.Empty;

                // Deserialize message tokens (JSON array if possible; otherwise, split on "; ")
                string[] messageTokens;
                try
                {
                    messageTokens = JsonConvert.DeserializeObject<string[]>(aggregatedMessage);
                }
                catch
                {
                    messageTokens = aggregatedMessage
                        .Trim('[', ']')
                        .Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => m.Trim())
                        .ToArray();
                }
                if (messageTokens == null)
                {
                    messageTokens = new string[0];
                }
                // Split decisionOutcome tokens (assumed to be aligned with messageTokens)
                string[] decisionOutcomeTokens = string.IsNullOrEmpty(aggregatedDecisionOutcome)
                    ? new string[0]
                    : aggregatedDecisionOutcome.Split(new string[] { "; " }, StringSplitOptions.None);

                int n = Math.Min(messageTokens.Length, decisionOutcomeTokens.Length);

                // --- Determine conventional (i.e. non–nonconventional) recognition ---
                // We ignore the Result tokens completely.
                bool conventionalRecognized = false;
                bool hasPartialBsc = false;
                bool hasPartialDiploma = false;
                bool hasFullRecognition = false;
                for (int i = 0; i < n; i++)
                {
                    string token = (messageTokens[i] ?? "").Trim();
                    // If token is in JSON array form, try to extract its first element.
                    string candidate;
                    if (token.StartsWith("[") && token.EndsWith("]"))
                    {
                        try
                        {
                            var arr = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(token);
                            candidate = (arr != null && arr.Count > 0) ? arr[0].Trim() : CleanToken(token);
                        }
                        catch
                        {
                            candidate = CleanToken(token);
                        }
                    }
                    else
                    {
                        candidate = CleanToken(token);
                    }
                    // Skip nonconventional tokens.
                    if (candidate.StartsWith("Non-Conventional Status:", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string outcome = (decisionOutcomeTokens[i] ?? "").Trim();
                    if (outcome.Equals("Recognized", StringComparison.OrdinalIgnoreCase))
                    {
                        conventionalRecognized = true;
                        string lowerCandidate = candidate.ToLower();
                        if (lowerCandidate.Contains("partial: bachelor's recognized"))
                            hasPartialBsc = true;
                        if (lowerCandidate.Contains("partial: diplomas recognized"))
                            hasPartialDiploma = true;
                        if (lowerCandidate.Contains("recognition: full recognition") ||
                            lowerCandidate.Contains("higher graduate studies recognized"))
                            hasFullRecognition = true;
                    }
                }

                // Build the conventional recognition result.
                string conventionalResult;
                if (!conventionalRecognized)
                {
                    conventionalResult = "Not yet Finalized by Ministry in a Meeting";
                }
                else
                {
                    if (hasPartialBsc && hasPartialDiploma)
                        conventionalResult = $"Accepted (Partial: BSc and Diploma) - Meeting {meetingNumber}";
                    else if (hasPartialBsc)
                        conventionalResult = $"Accepted (Partial: BSc) - Meeting {meetingNumber}";
                    else if (hasPartialDiploma)
                        conventionalResult = $"Accepted (Partial: Diploma) - Meeting {meetingNumber}";
                    else if (hasFullRecognition)
                        conventionalResult = $"Fully Accepted - Meeting {meetingNumber}";
                    else
                        conventionalResult = "Unknown Status - Needs Review";
                }

                // --- Determine non-conventional recognition ---
                string nonConventionalStatus = "Not yet Finalized by Ministry in a Meeting";
                // Scan in reverse order so the most recent non-conventional token takes precedence.
                for (int i = n - 1; i >= 0; i--)
                {
                    string token = (messageTokens[i] ?? "").Trim();
                    string candidate;
                    if (token.StartsWith("[") && token.EndsWith("]"))
                    {
                        try
                        {
                            var arr = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(token);
                            candidate = (arr != null && arr.Count > 0) ? arr[0].Trim() : CleanToken(token);
                        }
                        catch
                        {
                            candidate = CleanToken(token);
                        }
                    }
                    else
                    {
                        candidate = CleanToken(token);
                    }

                    if (candidate.StartsWith("Non-Conventional Status:", StringComparison.OrdinalIgnoreCase))
                    {
                        string outcome = (decisionOutcomeTokens[i] ?? "").Trim();
                        nonConventionalStatus = outcome.Equals("Recognized", StringComparison.OrdinalIgnoreCase)
                                                ? "Nonconventional Recognized"
                                                : "Not yet Finalized by Ministry in a Meeting";
                        break;
                    }
                }

                // --- Determine final status ---
                string finalStatus = "";
                if (supervisor.Equals("Not Assigned", StringComparison.OrdinalIgnoreCase))
                {
                    finalStatus = "University has not completed submission process (No Supervisor Assigned)";
                }
                else if (string.IsNullOrEmpty(meetingNumber))
                {
                    finalStatus = "Uni assigned to a supervisor but meeting hasn't been made for it";
                }
                else
                {
                    // Use the conventional recognition result.
                    finalStatus = conventionalResult;
                }

                // Gather bullet reasons (non-control tokens).
                var reasonsList = new List<string>();
                foreach (var line in messageTokens)
                {
                    string lower = (line ?? "").ToLower().Trim();
                    if (lower.StartsWith("partial") ||
                        lower.StartsWith("rejection") ||
                        lower.StartsWith("recognition") ||
                        lower.StartsWith("non-conventional status") ||
                        lower.StartsWith("rejected"))
                    {
                        continue;
                    }
                    reasonsList.Add(line);
                }
                string bulletPointsHtml = "";
                foreach (var reason in reasonsList)
                {
                    bulletPointsHtml += $"<li>{reason}</li>";
                }

                // Build the output object (structure preserved).
                var resultObject = new
                {
                    recordid = row["InsID"].ToString(), // using InsID as record id
                    meetingnumber = meetingNumber,
                    MeetingCreationDate = "(from meeting info)",
                    insname = row["InsName"].ToString(),
                    inscountry = row["Country"].ToString(),
                    recognizedDegreePhrase = conventionalResult,
                    systemPhrase = nonConventionalStatus == "Nonconventional Recognized" ? "non-conventional (Online)" : "traditional",
                    showRecognizedDiv = conventionalRecognized,
                    showRejectedDiv = !conventionalRecognized,
                    bulletPoints = bulletPointsHtml
                };

                // Prevent duplicate institutions.
                if (!institutionStatuses.Any(inst => inst.InsID == Convert.ToInt32(row["InsID"])))
                {
                    institutionStatuses.Add(new InstitutionStatus
                    {
                        InsID = Convert.ToInt32(row["InsID"]),
                        InsName = row["InsName"].ToString(),
                        Country = row["Country"].ToString(),
                        Supervisor = supervisor,
                        Status = finalStatus,
                        Message = aggregatedMessage,
                        NonConventionalStatus = nonConventionalStatus
                    });
                }
            }

            return institutionStatuses;
        }






        public List<PublicInstitutionStatus> GetPublicInstitutionStatuses()
        {
            string query = @"
SELECT 
    pi.InsID, 
    pi.InsName, 
    pi.Country, 
    COALESCE(ar.Result, 'Not Processed') AS Status,  -- ignored in our logic
    COALESCE(ar.Message, '') AS Message,
    COALESCE(ar.decisionOutcome, '') AS decisionOutcome
FROM PublicInfo pi
LEFT JOIN (
    SELECT 
        InsID, 
        STRING_AGG(Result, ', ') AS Result, 
        STRING_AGG(Message, ', ') AS Message,
        STRING_AGG(decisionOutcome, ', ') AS decisionOutcome
    FROM AcceptanceRecord
    GROUP BY InsID
) ar ON pi.InsID = ar.InsID
";

            DataTable resultTable = SelectRecords(query);
            List<PublicInstitutionStatus> publicStatuses = new();

            foreach (DataRow row in resultTable.Rows)
            {
                string message = row["Message"].ToString();
                string aggregatedDecisionOutcome = row["decisionOutcome"].ToString();

                // Split tokens by ", " (public version uses comma separation)
                List<string> messagesList = string.IsNullOrEmpty(message)
                    ? new List<string>()
                    : message.Split(new string[] { ", " }, StringSplitOptions.None).Select(m => m.Trim()).ToList();
                string[] decisionOutcomeTokens = string.IsNullOrEmpty(aggregatedDecisionOutcome)
                    ? new string[0]
                    : aggregatedDecisionOutcome.Split(new string[] { ", " }, StringSplitOptions.None);

                int count = Math.Min(messagesList.Count, decisionOutcomeTokens.Length);

                // --- Determine conventional recognition ---
                bool conventionalRecognizedFound = false;
                bool hasPartialBsc = false;
                bool hasPartialDiploma = false;
                bool hasFullRecognition = false;
                for (int i = 0; i < count; i++)
                {
                    string token = (messagesList[i] ?? "").Trim('[', ']', '"').Trim();
                    if (token.StartsWith("Non-Conventional Status:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string outcome = (decisionOutcomeTokens[i] ?? "").Trim();
                    if (outcome.Equals("Recognized", StringComparison.OrdinalIgnoreCase))
                    {
                        conventionalRecognizedFound = true;
                        string lowerToken = token.ToLower();
                        if (lowerToken.Contains("partial: bachelor's recognized"))
                            hasPartialBsc = true;
                        if (lowerToken.Contains("partial: diplomas recognized"))
                            hasPartialDiploma = true;
                        if (lowerToken.Contains("recognition: full recognition") ||
                            lowerToken.Contains("higher graduate studies recognized"))
                            hasFullRecognition = true;
                    }
                }

                string acceptanceLevel = "Not yet Recognized by Ministry";
                if (conventionalRecognizedFound)
                {
                    if (hasPartialBsc && hasPartialDiploma)
                        acceptanceLevel = "Diploma & BSc";
                    else if (hasPartialBsc)
                        acceptanceLevel = "BSc";
                    else if (hasPartialDiploma)
                        acceptanceLevel = "Diploma";
                    else if (hasFullRecognition)
                        acceptanceLevel = "Fully Accepted";
                }

                // --- Determine non-conventional (online) recognition ---
                string onlineRecognition = "Not yet Recognized by Ministry";
                for (int i = count - 1; i >= 0; i--)
                {
                    string token = (messagesList[i] ?? "").Trim('[', ']', '"').Trim();
                    if (token.StartsWith("Non-Conventional Status:", StringComparison.OrdinalIgnoreCase))
                    {
                        string outcome = (decisionOutcomeTokens[i] ?? "").Trim();
                        onlineRecognition = outcome.Equals("Recognized", StringComparison.OrdinalIgnoreCase)
                                          ? "Online(Non-Conventional) recognized"
                                          : "Not yet Recognized by Ministry";
                        break;
                    }
                }

                publicStatuses.Add(new PublicInstitutionStatus
                {
                    InsID = row["InsID"].ToString(), // Add this line
                    InsName = row["InsName"].ToString(),
                    Country = row["Country"].ToString(),
                    AcceptanceLevel = acceptanceLevel,
                    OnlineRecognition = onlineRecognition
                });
            }

            return publicStatuses;
        }

        public void ClearSupervisorForInstitution(int insId)
        {
            string query = "UPDATE PublicInfo SET SupervisorID = NULL, Supervisor = NULL WHERE InsID = @InsID";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@InsID", insId);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[ClearSupervisorForInstitution] SQL EXCEPTION: {ex}");
                    throw new Exception($"Database update operation failed: {ex.Message}", ex);
                }
            }
        }

        public void MarkAcceptanceRecordFinalized(int recordId, string meetingFinalized, string decisionOutcome)
        {
            // We'll use a simple byte array as our "finalized" flag.
            byte[] finalizedValue = new byte[] { 1 };

            string query = @"
        UPDATE AcceptanceRecord 
        SET finalized = @finalized,
            meetingFinalized = @meetingFinalized,
            decisionOutcome = @decisionOutcome
        WHERE RecordID = @recordId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@finalized", finalizedValue);
                        command.Parameters.AddWithValue("@meetingFinalized",
                            string.IsNullOrEmpty(meetingFinalized) ? (object)DBNull.Value : meetingFinalized);
                        command.Parameters.AddWithValue("@decisionOutcome",
                            string.IsNullOrEmpty(decisionOutcome) ? (object)DBNull.Value : decisionOutcome);
                        command.Parameters.AddWithValue("@recordId", recordId);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"[MarkAcceptanceRecordFinalized] SQL EXCEPTION: {ex}");
                    throw new Exception($"Database update operation failed: {ex.Message}", ex);
                }
            }
        }
        public Infrastructure GetInfrastructureByInsID(int insId)
        {
            string sql = "SELECT * FROM Infrastructure WHERE InsID = @InsID";
            string[] paramNames = { "@InsID" };
            object[] values = { insId };

            DataTable dt = SelectRecords(sql, paramNames, values);
            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];

            return new Infrastructure(
                insID: Convert.ToInt32(row["InsID"]),
                area: row["Area"] != DBNull.Value ? Convert.ToInt32(row["Area"]) : (int?)null,
                sites: row["Sites"] != DBNull.Value ? Convert.ToInt32(row["Sites"]) : (int?)null,
                terr: row["Terr"] != DBNull.Value ? Convert.ToInt32(row["Terr"]) : (int?)null,
                halls: row["Halls"] != DBNull.Value ? Convert.ToInt32(row["Halls"]) : (int?)null,
                library: row["Library"] != DBNull.Value ? Convert.ToInt32(row["Library"]) : (int?)null
            );
        }



    }
}
