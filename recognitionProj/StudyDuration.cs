public class StudyDuration
{
    public int InsID { get; set; }

    public string? DiplomaDegreeMIN { get; set; }
    public string? DiplomaMIN { get; set; }
    public string? BSC_DegreeMIN { get; set; }
    public string? BSC_MIN { get; set; }
    public string? HigherDiplomaDegreeMIN { get; set; }
    public string? HigherDiplomaMIN { get; set; }
    public string? MasterDegreeMIN { get; set; }
    public string? MasterMIN { get; set; }
    public string? PhD_DegreeMIN { get; set; }
    public string? PhD_MIN { get; set; }

    // ========== NEW FIELDS ===============
    public int? SemestersPerYear { get; set; }
    public int? MonthsPerSemester { get; set; }

    // (Optionally keep existing constructors, or update them)
    // We'll add a constructor that includes the new fields:
    public StudyDuration(
        int insID,
        string diplomaDegreeMIN,
        string diplomaMIN,
        string bscDegreeMIN,
        string bscMin,
        string higherDiplomaDegreeMIN,
        string higherDiplomaMIN,
        string masterDegreeMIN,
        string masterMin,
        string phdDegreeMIN,
        string phdMin,
        int? semestersPerYear,
        int? monthsPerSemester
    )
    {
        InsID = insID;
        DiplomaDegreeMIN = diplomaDegreeMIN;
        DiplomaMIN = diplomaMIN;
        BSC_DegreeMIN = bscDegreeMIN;
        BSC_MIN = bscMin;
        HigherDiplomaDegreeMIN = higherDiplomaDegreeMIN;
        HigherDiplomaMIN = higherDiplomaMIN;
        MasterDegreeMIN = masterDegreeMIN;
        MasterMIN = masterMin;
        PhD_DegreeMIN = phdDegreeMIN;
        PhD_MIN = phdMin;
        SemestersPerYear = semestersPerYear;
        MonthsPerSemester = monthsPerSemester;
    }

    // If needed, also keep a parameterless constructor
    public StudyDuration() { }
}
