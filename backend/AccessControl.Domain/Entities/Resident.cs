using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessControl.Domain.Entities
{
    public class Resident : UserBase
    {
        public string ApartmentNumber { get; set; } = string.Empty;
        public string Tower { get; set; } = string.Empty;
    }
}
