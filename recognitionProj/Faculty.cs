namespace recognitionProj
{
    public class Faculty
    {
        private int _insID;
        private string _facultyName;
        private DateOnly _createDate;
        private DateOnly _lastGraduateDate;
        private int _numPrograms;
        private int _numPgPrograms;
        public Faculty() { }

        public Faculty(int insID, string facultyName, DateOnly createDate, DateOnly lastGraduateDate, int numPrograms, int numPgPrograms)
        {
            _insID = insID;
            _facultyName = facultyName;
            _createDate = createDate;
            _lastGraduateDate = lastGraduateDate;
            _numPrograms = numPrograms;
            _numPgPrograms = numPgPrograms;
        }

        public int InsID
        {
            get => _insID;
            set => _insID = value;
        }

        public string FacultyName
        {
            get => _facultyName;
            set => _facultyName = value;
        }

        public DateOnly CreateDate
        {
            get => _createDate;
            set => _createDate = value;
        }

        public DateOnly LastGraduateDate
        {
            get => _lastGraduateDate;
            set => _lastGraduateDate = value;
        }
        public int NumPrograms
        {
            get => _numPrograms;
            set => _numPrograms = value;

        }
        public int NumPgPrograms
        {
            get => _numPgPrograms;
            set => _numPgPrograms = value;
        }
    }
}
