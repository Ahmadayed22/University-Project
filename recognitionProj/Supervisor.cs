using Microsoft.AspNetCore.Http.HttpResults;

namespace recognitionProj
{
    public class Supervisor // todo controller and db handler
    {
        private int _supervisorID;
        private string _name;
        private string _phoneNumber;
        private string _email;
        private string _password;
        private string _speciality;//to distribute the supervisors to the universities

        // Constructor
        public Supervisor(int supervisorID, string name,string phoneNumber, string email, string password, string speciality)
        {
            _supervisorID = supervisorID;
            _name = name;
            _phoneNumber = phoneNumber;
            _email = email;
            _password = password;

            _speciality = speciality;
        }
        

        // Properties
        public int SupervisorID
        {
            get => _supervisorID;
            set => _supervisorID = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => _phoneNumber = value;
        }

        public string Email
        {
            get => _email;
            set => _email = value;
        }

        public string Password
        {
            get => _password;
            set => _password = value;
        }

       
        public string Speciality
        {
            get => _speciality;
            set => _speciality = value;
        }
    }
}
