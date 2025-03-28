﻿using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Data;
using System.Runtime.InteropServices;

namespace recognitionProj
{
    public class Hospitals // todo controller and db handler
    {
        private int _insID;
        private string _hospType;
        private string _hospName;
        private string _hospMajor;

        // Constructor
        public Hospitals(int insID,  string hospType, string hospName, string hospMajor)
        {
            _insID = insID;
            
            _hospType = hospType;
            _hospName = hospName;
            _hospMajor = hospMajor;
        }

        // Properties
        public int InsID
        {
            get => _insID;
            set => _insID = value;
        }

        public string HospType
        {
            get => _hospType;
            set => _hospType = value;
        }

        public string HospName
        {
            get => _hospName;
            set => _hospName = value;
        }

        public string HospMajor
        {
            get => _hospMajor;
            set => _hospMajor = value;
        }
    }
}
